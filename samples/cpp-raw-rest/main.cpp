#include <windows.h>
#include <winhttp.h>

#include <iostream>
#include <stdexcept>
#include <string>
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

    std::string SendRequest(const ParsedUrl& url, const wchar_t* method, const wchar_t* path, const std::string& body = "")
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

        std::string response;
        DWORD available = 0;
        while (WinHttpQueryDataAvailable(request.value, &available) && available > 0)
        {
            std::vector<char> buffer(static_cast<size_t>(available));
            DWORD read = 0;
            if (!WinHttpReadData(request.value, buffer.data(), available, &read))
            {
                throw std::runtime_error("Failed to read REST response.");
            }

            response.append(buffer.data(), read);
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

        const std::string status = SendRequest(url, L"GET", L"/api/status");
        std::cout << "Status: " << status << std::endl;

        const std::string waferInfo =
            R"({"lotId":"LOT-CPP-REST-001","waferId":"W01","recipeId":"RCP-A","slot":"1","foupId":"FOUP-A","chamberId":"CH-1"})";
        SendRequest(url, L"POST", L"/api/wafer-info", waferInfo);
        std::cout << "WaferInfo updated through raw REST." << std::endl;
    }
    catch (const std::exception& ex)
    {
        std::cerr << "Sample failed: " << ex.what() << std::endl;
        return 1;
    }

    return 0;
}

