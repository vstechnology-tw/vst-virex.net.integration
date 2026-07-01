#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>

#include <iostream>
#include <stdexcept>
#include <string>

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

    std::string ReadLine(SOCKET socket)
    {
        std::string line;
        char ch = '\0';
        while (true)
        {
            const int received = recv(socket, &ch, 1, 0);
            if (received <= 0)
            {
                throw std::runtime_error("Connection closed before a full TCP frame was received.");
            }

            if (ch == '\n')
            {
                return line;
            }

            if (ch != '\r')
            {
                line.push_back(ch);
            }
        }
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
            throw std::runtime_error("Failed to resolve TCP endpoint.");
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
        throw std::runtime_error("Failed to connect to TCP endpoint.");
    }

    void SendAll(SOCKET socket, const std::string& value)
    {
        size_t sent = 0;
        while (sent < value.size())
        {
            const int chunk = send(socket, value.data() + sent, static_cast<int>(value.size() - sent), 0);
            if (chunk <= 0)
            {
                throw std::runtime_error("Failed to send TCP frame.");
            }

            sent += static_cast<size_t>(chunk);
        }
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
        const char* port = argc > 2 ? argv[2] : "5089";

        PrintStep("Virex.NET C++ Raw TCP 13-Step Demo");
        std::cout << "TCP endpoint: " << host << ":" << port << std::endl;
        Prompt("Press Start Servers, then press Enter here.");

        WsaSession wsa;
        SocketHandle client = Connect(host, port);

        std::cout << "Initial simulator frames:" << std::endl;
        std::cout << ReadLine(client.value) << std::endl;
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 1 - Query status");
        SendAll(client.value, std::string(R"({"type":"status"})") + "\n");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 2 - Query error");
        SendAll(client.value, std::string(R"({"type":"error"})") + "\n");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 3 - Query ProductInfo");
        SendAll(client.value, std::string(R"({"type":"getProductInfo"})") + "\n");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 4 - Initialize");
        SendAll(client.value, std::string(R"({"type":"initialize"})") + "\n");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 5 - Confirm Ready");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 6 - Set ProductInfo");
        const std::string frame =
            R"({"type":"productInfo","waferID":"WCPP-TCP-210-001","lotID":"LOT-CPP-TCP-210","recipe":"RCP-DEMO","slot":"1","foupID":"FOUP-DEMO","chamberID":"CH-1"})"
            "\n";
        SendAll(client.value, frame);
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 7 - Confirm ProductInfo");
        SendAll(client.value, std::string(R"({"type":"getProductInfo"})") + "\n");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 8 - Start run");
        const std::string startFrame = R"({"type":"start","condition":"golden-sample","runMode":"continue"})"
            "\n";
        SendAll(client.value, startFrame);
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 9 - Observe run events");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 10 - Stop run");
        const std::string stopFrame = R"({"type":"stop","reason":"operator-request"})"
            "\n";
        SendAll(client.value, stopFrame);
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 11 - Query results");
        SendAll(client.value, std::string(R"({"type":"results","lotID":"LOT-CPP-TCP-210","waferID":"WCPP-TCP-210-001"})") + "\n");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 12 - Deinitialize");
        SendAll(client.value, std::string(R"({"type":"deinitialize"})") + "\n");
        std::cout << ReadLine(client.value) << std::endl;

        PrintStep("Step 13 - Confirm Uninitialized");
        std::cout << ReadLine(client.value) << std::endl;
    }
    catch (const std::exception& ex)
    {
        std::cerr << "Sample failed: " << ex.what() << std::endl;
        return 1;
    }

    return 0;
}
