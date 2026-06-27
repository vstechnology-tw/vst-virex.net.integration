#include <windows.h>
#include <winhttp.h>

#include <chrono>
#include <future>
#include <iostream>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>

namespace
{
    std::wstring ToWide(const std::string& value)
    {
        if (value.empty())
        {
            return std::wstring();
        }

        const int length = MultiByteToWideChar(CP_UTF8, 0, value.c_str(), -1, nullptr, 0);
        if (length == 0)
        {
            throw std::runtime_error("Failed to convert UTF-8 string.");
        }

        std::wstring result(static_cast<size_t>(length - 1), L'\0');
        MultiByteToWideChar(CP_UTF8, 0, value.c_str(), -1, result.data(), length);
        return result;
    }

    std::string ToUtf8(const std::wstring& value)
    {
        if (value.empty())
        {
            return std::string();
        }

        const int length = WideCharToMultiByte(CP_UTF8, 0, value.c_str(), -1, nullptr, 0, nullptr, nullptr);
        if (length == 0)
        {
            throw std::runtime_error("Failed to convert UTF-16 string.");
        }

        std::string result(static_cast<size_t>(length - 1), '\0');
        WideCharToMultiByte(CP_UTF8, 0, value.c_str(), -1, result.data(), length, nullptr, nullptr);
        return result;
    }

    struct ParsedUrl
    {
        std::wstring host;
        INTERNET_PORT port = 0;
        bool secure = false;
    };

    ParsedUrl ParseBaseUrl(const std::string& baseUrl)
    {
        const std::wstring wideUrl = ToWide(baseUrl);
        URL_COMPONENTS parts{};
        parts.dwStructSize = sizeof(parts);
        parts.dwHostNameLength = static_cast<DWORD>(-1);

        if (!WinHttpCrackUrl(wideUrl.c_str(), 0, 0, &parts))
        {
            throw std::runtime_error("Base URL must look like http://127.0.0.1:5088.");
        }

        ParsedUrl result;
        result.host.assign(parts.lpszHostName, parts.dwHostNameLength);
        result.port = parts.nPort;
        result.secure = parts.nScheme == INTERNET_SCHEME_HTTPS;
        return result;
    }

    struct WinHttpHandle
    {
        HINTERNET value = nullptr;

        explicit WinHttpHandle(HINTERNET handle = nullptr) : value(handle)
        {
        }

        ~WinHttpHandle()
        {
            if (value != nullptr)
            {
                WinHttpCloseHandle(value);
            }
        }

        WinHttpHandle(const WinHttpHandle&) = delete;
        WinHttpHandle& operator=(const WinHttpHandle&) = delete;
    };

    struct HttpResponse
    {
        DWORD statusCode = 0;
        std::string body;
    };

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

    HttpResponse SendRequest(const ParsedUrl& url, const wchar_t* method, const wchar_t* path, const std::string& body = "")
    {
        WinHttpHandle session(WinHttpOpen(L"Virex.NET C++ REST Sample/1.0", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0));
        if (session.value == nullptr)
        {
            throw std::runtime_error("WinHttpOpen failed.");
        }

        WinHttpHandle connection(WinHttpConnect(session.value, url.host.c_str(), url.port, 0));
        if (connection.value == nullptr)
        {
            throw std::runtime_error("WinHttpConnect failed.");
        }

        const DWORD flags = url.secure ? WINHTTP_FLAG_SECURE : 0;
        WinHttpHandle request(WinHttpOpenRequest(connection.value, method, path, nullptr, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, flags));
        if (request.value == nullptr)
        {
            throw std::runtime_error("WinHttpOpenRequest failed.");
        }

        const wchar_t* headers = body.empty() ? WINHTTP_NO_ADDITIONAL_HEADERS : L"Content-Type: application/json\r\n";
        const DWORD headerLength = body.empty() ? 0 : static_cast<DWORD>(-1L);
        void* requestBody = body.empty() ? WINHTTP_NO_REQUEST_DATA : const_cast<char*>(body.data());
        const DWORD bodyLength = static_cast<DWORD>(body.size());

        if (!WinHttpSendRequest(request.value, headers, headerLength, requestBody, bodyLength, bodyLength, 0) ||
            !WinHttpReceiveResponse(request.value, nullptr))
        {
            throw std::runtime_error("REST request failed.");
        }

        HttpResponse response;
        DWORD statusCode = 0;
        DWORD statusCodeSize = sizeof(statusCode);
        if (WinHttpQueryHeaders(request.value, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, WINHTTP_HEADER_NAME_BY_INDEX, &statusCode, &statusCodeSize, WINHTTP_NO_HEADER_INDEX))
        {
            response.statusCode = statusCode;
        }

        DWORD available = 0;
        while (WinHttpQueryDataAvailable(request.value, &available) && available > 0)
        {
            std::vector<char> buffer(static_cast<size_t>(available));
            DWORD read = 0;
            if (!WinHttpReadData(request.value, buffer.data(), available, &read))
            {
                throw std::runtime_error("Failed to read REST response.");
            }

            response.body.append(buffer.data(), read);
        }

        return response;
    }
}

int main(int argc, char* argv[])
{
    try
    {
        const std::string baseUrl = argc > 1 ? argv[1] : "http://127.0.0.1:5088";
        const ParsedUrl url = ParseBaseUrl(baseUrl);

        PrintStep("Virex.NET C++ Raw REST Guided Demo");
        std::cout << "This sample uses direct REST calls and shows how simulator UI state affects API returns." << std::endl;
        std::cout << "REST base URL: " << baseUrl << std::endl;
        Prompt("Press Start Servers. Leave Initialize unpressed for the first check, then press Enter here.");

        PrintStep("Step 1 - Read /api/status");
        const HttpResponse status = SendRequest(url, L"GET", L"/api/status");
        std::cout << "Status returned HTTP " << status.statusCode << ": " << status.body << std::endl;

        if (status.body.find("\"state\":\"Uninitialized\"") != std::string::npos)
        {
            PrintStep("Step 2 - Expected negative check");
            std::cout << "Calling POST /api/system/start before Initialize should return HTTP 409 invalid_state." << std::endl;
            const HttpResponse negative = SendRequest(url, L"POST", L"/api/system/start");
            std::cout << "Start returned HTTP " << negative.statusCode << ": " << negative.body << std::endl;
            if (negative.statusCode != 409 || negative.body.find("invalid_state") == std::string::npos)
            {
                throw std::runtime_error("Expected HTTP 409 invalid_state. Confirm the simulator was not initialized.");
            }

            SendRequest(url, L"POST", L"/api/system/initialize");
        }

        PrintStep("Step 3 - POST /api/product-info");
        const std::string productInfo =
            R"({"waferID":"WCPP-REST-001","lotID":"LOT-CPP-REST-001","recipe":"RCP-A","slot":"1","foupID":"FOUP-A","chamberID":"CH-1"})";
        const HttpResponse waferResponse = SendRequest(url, L"POST", L"/api/product-info", productInfo);
        if (waferResponse.statusCode >= 400)
        {
            throw std::runtime_error("ProductInfo update failed: " + waferResponse.body);
        }

        std::cout << "Expected Simulator Event Log:" << std::endl;
        std::cout << "ProductInfo updated from REST." << std::endl;

        PrintStep("Step 4 - POST /api/system/start with condition and runMode payload");
        std::cout << "Expected Simulator Status: capturing -> inspecting -> saving -> ready." << std::endl;
        const HttpResponse start = SendRequest(url, L"POST", L"/api/system/start", R"({"condition":"golden-sample","runMode":"continue"})");
        std::cout << "Start returned HTTP " << start.statusCode << ": " << start.body << std::endl;
        if (start.statusCode >= 400)
        {
            throw std::runtime_error("Start failed. Confirm Initialize was pressed and state is Ready.");
        }
        std::cout << "Expected Simulator Event Log:" << std::endl;
        std::cout << "Start condition: golden-sample" << std::endl;
        std::cout << "Start run mode: continue" << std::endl;

        PrintStep("Step 5 - POST /api/system/stop with reason payload");
        auto stopDemoStart = std::async(std::launch::async, [&url]
        {
            return SendRequest(url, L"POST", L"/api/system/start", R"({"condition":"stop-demo","runMode":"continue"})");
        });
        std::this_thread::sleep_for(std::chrono::milliseconds(300));
        const HttpResponse stop = SendRequest(url, L"POST", L"/api/system/stop", R"({"reason":"operator-request"})");
        stopDemoStart.get();
        std::cout << "Stop returned HTTP " << stop.statusCode << ": " << stop.body << std::endl;
        if (stop.statusCode >= 400)
        {
            throw std::runtime_error("Stop failed. Confirm a cycle was running.");
        }
        std::cout << "Expected Simulator Event Log:" << std::endl;
        std::cout << "Stopped. reason=operator-request" << std::endl;

        PrintStep("Step 6 - GET /api/results by lotID");
        const HttpResponse results = SendRequest(url, L"GET", L"/api/results?lotID=LOT-CPP-REST-001");
        std::cout << "Results returned HTTP " << results.statusCode << ": " << results.body << std::endl;
    }
    catch (const std::exception& ex)
    {
        std::cerr << "Sample failed: " << ex.what() << std::endl;
        return 1;
    }

    return 0;
}

