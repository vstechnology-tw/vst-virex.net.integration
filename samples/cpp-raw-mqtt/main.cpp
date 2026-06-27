#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>

#include <chrono>
#include <cstdint>
#include <iostream>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>

namespace
{
    struct WsaSession
    {
        WsaSession()
        {
            WSADATA data{};
            if (WSAStartup(MAKEWORD(2, 2), &data) != 0)
            {
                throw std::runtime_error("WSAStartup failed.");
            }
        }

        ~WsaSession()
        {
            WSACleanup();
        }
    };

    struct SocketHandle
    {
        SOCKET value = INVALID_SOCKET;

        SocketHandle() = default;

        ~SocketHandle()
        {
            if (value != INVALID_SOCKET)
            {
                closesocket(value);
            }
        }

        SocketHandle(const SocketHandle&) = delete;
        SocketHandle& operator=(const SocketHandle&) = delete;

        SocketHandle(SocketHandle&& other) noexcept : value(other.value)
        {
            other.value = INVALID_SOCKET;
        }

        SocketHandle& operator=(SocketHandle&& other) noexcept
        {
            if (this != &other)
            {
                if (value != INVALID_SOCKET)
                {
                    closesocket(value);
                }

                value = other.value;
                other.value = INVALID_SOCKET;
            }

            return *this;
        }
    };

    void AppendUtf8Field(std::vector<uint8_t>& output, const std::string& value)
    {
        output.push_back(static_cast<uint8_t>((value.size() >> 8) & 0xFF));
        output.push_back(static_cast<uint8_t>(value.size() & 0xFF));
        output.insert(output.end(), value.begin(), value.end());
    }

    std::vector<uint8_t> EncodeRemainingLength(size_t value)
    {
        std::vector<uint8_t> encoded;
        do
        {
            uint8_t digit = static_cast<uint8_t>(value % 128);
            value /= 128;
            if (value > 0)
            {
                digit |= 0x80;
            }

            encoded.push_back(digit);
        } while (value > 0);

        return encoded;
    }

    std::vector<uint8_t> Packet(uint8_t packetType, const std::vector<uint8_t>& payload)
    {
        std::vector<uint8_t> output;
        output.push_back(packetType);
        const auto remaining = EncodeRemainingLength(payload.size());
        output.insert(output.end(), remaining.begin(), remaining.end());
        output.insert(output.end(), payload.begin(), payload.end());
        return output;
    }

    std::vector<uint8_t> ConnectPacket(const std::string& clientId)
    {
        std::vector<uint8_t> payload;
        AppendUtf8Field(payload, "MQTT");
        payload.push_back(4);
        payload.push_back(2);
        payload.push_back(0);
        payload.push_back(60);
        AppendUtf8Field(payload, clientId);
        return Packet(0x10, payload);
    }

    std::vector<uint8_t> SubscribePacket(uint16_t packetId, const std::string& topic)
    {
        std::vector<uint8_t> payload;
        payload.push_back(static_cast<uint8_t>((packetId >> 8) & 0xFF));
        payload.push_back(static_cast<uint8_t>(packetId & 0xFF));
        AppendUtf8Field(payload, topic);
        payload.push_back(0);
        return Packet(0x82, payload);
    }

    SocketHandle Connect(const char* host, const char* port)
    {
        addrinfo hints{};
        hints.ai_family = AF_UNSPEC;
        hints.ai_socktype = SOCK_STREAM;
        hints.ai_protocol = IPPROTO_TCP;

        addrinfo* resolved = nullptr;
        if (getaddrinfo(host, port, &hints, &resolved) != 0)
        {
            throw std::runtime_error("Failed to resolve MQTT endpoint.");
        }

        SocketHandle result;
        for (addrinfo* candidate = resolved; candidate != nullptr; candidate = candidate->ai_next)
        {
            result.value = socket(candidate->ai_family, candidate->ai_socktype, candidate->ai_protocol);
            if (result.value == INVALID_SOCKET)
            {
                continue;
            }

            if (connect(result.value, candidate->ai_addr, static_cast<int>(candidate->ai_addrlen)) == 0)
            {
                freeaddrinfo(resolved);
                return result;
            }

            closesocket(result.value);
            result.value = INVALID_SOCKET;
        }

        freeaddrinfo(resolved);
        throw std::runtime_error("Failed to connect to MQTT endpoint.");
    }

    void SendAll(SOCKET socket, const std::vector<uint8_t>& value)
    {
        size_t sent = 0;
        while (sent < value.size())
        {
            const int chunk = send(socket, reinterpret_cast<const char*>(value.data() + sent), static_cast<int>(value.size() - sent), 0);
            if (chunk <= 0)
            {
                throw std::runtime_error("Failed to send MQTT packet.");
            }

            sent += static_cast<size_t>(chunk);
        }
    }

    uint8_t RecvByte(SOCKET socket)
    {
        uint8_t value = 0;
        const int received = recv(socket, reinterpret_cast<char*>(&value), 1, 0);
        if (received <= 0)
        {
            throw std::runtime_error("MQTT connection closed.");
        }

        return value;
    }

    std::vector<uint8_t> RecvExact(SOCKET socket, size_t length)
    {
        std::vector<uint8_t> output(length);
        size_t read = 0;
        while (read < length)
        {
            const int received = recv(socket, reinterpret_cast<char*>(output.data() + read), static_cast<int>(length - read), 0);
            if (received <= 0)
            {
                throw std::runtime_error("MQTT connection closed.");
            }

            read += static_cast<size_t>(received);
        }

        return output;
    }

    bool TryRecvPacket(SOCKET socket, uint8_t& first, std::vector<uint8_t>& payload)
    {
        first = 0;
        char firstByte = 0;
        const int received = recv(socket, &firstByte, 1, 0);
        if (received == SOCKET_ERROR)
        {
            const int error = WSAGetLastError();
            if (error == WSAETIMEDOUT)
            {
                return false;
            }

            throw std::runtime_error("Failed to receive MQTT packet.");
        }

        if (received == 0)
        {
            throw std::runtime_error("MQTT connection closed.");
        }

        first = static_cast<uint8_t>(firstByte);
        size_t multiplier = 1;
        size_t remaining = 0;
        while (true)
        {
            const auto digit = RecvByte(socket);
            remaining += static_cast<size_t>(digit & 0x7F) * multiplier;
            if ((digit & 0x80) == 0)
            {
                break;
            }

            multiplier *= 128;
        }

        payload = RecvExact(socket, remaining);
        return true;
    }

    void PrintPublish(uint8_t first, const std::vector<uint8_t>& payload)
    {
        if (payload.size() < 2)
        {
            return;
        }

        const size_t topicLength = (static_cast<size_t>(payload[0]) << 8) | payload[1];
        if (payload.size() < 2 + topicLength)
        {
            return;
        }

        const int qos = (first >> 1) & 0x03;
        size_t offset = 2 + topicLength;
        if (qos > 0)
        {
            offset += 2;
        }

        const std::string topic(payload.begin() + 2, payload.begin() + 2 + static_cast<std::ptrdiff_t>(topicLength));
        const std::string message(payload.begin() + static_cast<std::ptrdiff_t>(offset), payload.end());
        std::cout << topic << ": " << message << std::endl;
    }

    void PrintStep(const std::string& title)
    {
        std::cout << std::endl;
        std::cout << "== " << title << " ==" << std::endl;
    }

    void Prompt(const std::string& message)
    {
        std::cout << std::endl;
        std::cout << "Action required in Simulator:" << std::endl;
        std::cout << message << " ";
        std::string ignored;
        std::getline(std::cin, ignored);
    }
}

int main(int argc, char* argv[])
{
    try
    {
        const char* host = argc > 1 ? argv[1] : "127.0.0.1";
        const char* port = argc > 2 ? argv[2] : "1883";
        const std::string baseTopic = argc > 3 ? argv[3] : "virex";
        const int durationSeconds = argc > 4 ? std::stoi(argv[4]) : 30;
        const std::string topicFilter = baseTopic + "/#";

        PrintStep("Virex.NET C++ Raw MQTT Guided Demo");
        std::cout << "This sample subscribes to simulator MQTT events and lets you trigger each event from the UI." << std::endl;
        std::cout << "MQTT endpoint: " << host << ":" << port << std::endl;
        std::cout << "Topic filter: " << topicFilter << std::endl;
        Prompt("Press Start Servers, then press Enter here.");

        WsaSession wsa;
        SocketHandle client = Connect(host, port);

        const DWORD timeout = 1000;
        setsockopt(client.value, SOL_SOCKET, SO_RCVTIMEO, reinterpret_cast<const char*>(&timeout), sizeof(timeout));

        SendAll(client.value, ConnectPacket("virex-cpp-raw-mqtt"));
        uint8_t first = 0;
        std::vector<uint8_t> payload;
        if (!TryRecvPacket(client.value, first, payload) || first != 0x20 || payload.size() < 2 || payload[1] != 0)
        {
            throw std::runtime_error("MQTT CONNACK did not indicate success.");
        }

        SendAll(client.value, SubscribePacket(1, topicFilter));
        if (!TryRecvPacket(client.value, first, payload) || first != 0x90)
        {
            throw std::runtime_error("MQTT SUBACK was not received.");
        }

        PrintStep("Step 1 - Trigger events from Simulator");
        std::cout << "Subscribed to " << topicFilter << " for " << durationSeconds << " seconds." << std::endl;
        std::cout << "Expected UI actions and topics:" << std::endl;
        std::cout << "- Press Apply ProductInfo: expect virex/productInfoChanged." << std::endl;
        std::cout << "- Press Initialize: expect virex/statusChanged with state=Ready." << std::endl;
        std::cout << "- Press Start Cycle: expect virex/statusChanged and virex/resultCreated." << std::endl;

        const auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(durationSeconds);
        while (std::chrono::steady_clock::now() < deadline)
        {
            if (TryRecvPacket(client.value, first, payload) && (first >> 4) == 3)
            {
                PrintPublish(first, payload);
            }
        }
    }
    catch (const std::exception& ex)
    {
        std::cerr << "Sample failed: " << ex.what() << std::endl;
        return 1;
    }

    return 0;
}
