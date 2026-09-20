#include <windows.h>
#include <shellapi.h>
#include <string>
#include <vector>

namespace
{
constexpr wchar_t AppFileName[] = L"Mini-Web-Counter.App.exe";
constexpr wchar_t DownloadUrl[] = L"https://dotnet.microsoft.com/download/dotnet/10.0";
constexpr wchar_t WindowTitle[] = L"Mini Web Counter";

std::wstring GetEnvironmentValue(const wchar_t* name)
{
    const DWORD length = GetEnvironmentVariableW(name, nullptr, 0);
    if (length == 0)
    {
        return {};
    }

    std::wstring value(length, L'\0');
    const DWORD written = GetEnvironmentVariableW(name, value.data(), length);
    if (written == 0 || written >= length)
    {
        return {};
    }

    value.resize(written);
    return value;
}

std::wstring GetRegisteredDotnetRoot()
{
    DWORD size = 0;
    const LSTATUS sizeResult = RegGetValueW(
        HKEY_LOCAL_MACHINE,
        L"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\x64",
        L"InstallLocation",
        RRF_RT_REG_SZ | RRF_SUBKEY_WOW6464KEY,
        nullptr,
        nullptr,
        &size);

    if (sizeResult != ERROR_SUCCESS || size == 0)
    {
        return {};
    }

    std::wstring value(size / sizeof(wchar_t), L'\0');
    const LSTATUS readResult = RegGetValueW(
        HKEY_LOCAL_MACHINE,
        L"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\x64",
        L"InstallLocation",
        RRF_RT_REG_SZ | RRF_SUBKEY_WOW6464KEY,
        nullptr,
        value.data(),
        &size);

    if (readResult != ERROR_SUCCESS)
    {
        return {};
    }

    while (!value.empty() && value.back() == L'\0')
    {
        value.pop_back();
    }

    return value;
}

bool HasDesktopRuntime10(const std::wstring& dotnetRoot)
{
    if (dotnetRoot.empty())
    {
        return false;
    }

    std::wstring pattern = dotnetRoot;
    if (pattern.back() != L'\\')
    {
        pattern += L'\\';
    }
    pattern += L"shared\\Microsoft.WindowsDesktop.App\\10.*";

    WIN32_FIND_DATAW findData{};
    HANDLE findHandle = FindFirstFileW(pattern.c_str(), &findData);
    if (findHandle == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    bool found = false;
    do
    {
        if ((findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0 &&
            wcsncmp(findData.cFileName, L"10.", 3) == 0)
        {
            found = true;
            break;
        }
    } while (FindNextFileW(findHandle, &findData));

    FindClose(findHandle);
    return found;
}

bool IsDesktopRuntimeInstalled()
{
    std::vector<std::wstring> roots;

    const std::wstring dotnetRootX64 = GetEnvironmentValue(L"DOTNET_ROOT_X64");
    if (!dotnetRootX64.empty())
    {
        roots.push_back(dotnetRootX64);
    }

    const std::wstring dotnetRoot = GetEnvironmentValue(L"DOTNET_ROOT");
    if (!dotnetRoot.empty())
    {
        roots.push_back(dotnetRoot);
    }

    const std::wstring registeredRoot = GetRegisteredDotnetRoot();
    if (!registeredRoot.empty())
    {
        roots.push_back(registeredRoot);
    }

    const std::wstring programFiles = GetEnvironmentValue(L"ProgramW6432");
    if (!programFiles.empty())
    {
        roots.push_back(programFiles + L"\\dotnet");
    }

    for (const auto& root : roots)
    {
        if (HasDesktopRuntime10(root))
        {
            return true;
        }
    }

    return false;
}

std::wstring GetExecutableDirectory()
{
    std::vector<wchar_t> buffer(32768);
    const DWORD length = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (length == 0 || length >= buffer.size())
    {
        return {};
    }

    std::wstring path(buffer.data(), length);
    const size_t slash = path.find_last_of(L"\\/");
    if (slash == std::wstring::npos)
    {
        return {};
    }

    return path.substr(0, slash);
}

bool FileExists(const std::wstring& path)
{
    const DWORD attributes = GetFileAttributesW(path.c_str());
    return attributes != INVALID_FILE_ATTRIBUTES &&
           (attributes & FILE_ATTRIBUTE_DIRECTORY) == 0;
}
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int)
{
    const std::wstring directory = GetExecutableDirectory();
    if (directory.empty())
    {
        MessageBoxW(
            nullptr,
            L"프로그램 실행 경로를 확인할 수 없습니다.",
            WindowTitle,
            MB_OK | MB_ICONERROR);
        return 1;
    }

    const std::wstring appPath = directory + L"\\" + AppFileName;
    if (!FileExists(appPath))
    {
        MessageBoxW(
            nullptr,
            L"Mini-Web-Counter.App.exe 파일을 찾을 수 없습니다.\n배포 ZIP의 파일을 같은 폴더에 모두 압축 해제한 뒤 다시 실행해 주세요.",
            WindowTitle,
            MB_OK | MB_ICONERROR);
        return 2;
    }

    if (!IsDesktopRuntimeInstalled())
    {
        const int result = MessageBoxW(
            nullptr,
            L"이 프로그램을 실행하려면 Microsoft .NET 10 Desktop Runtime (x64)이 필요합니다.\n\n공식 다운로드 페이지를 여시겠습니까?",
            WindowTitle,
            MB_YESNO | MB_ICONINFORMATION | MB_DEFBUTTON1);

        if (result == IDYES)
        {
            ShellExecuteW(nullptr, L"open", DownloadUrl, nullptr, nullptr, SW_SHOWNORMAL);
        }

        return 3;
    }

    SetCurrentDirectoryW(directory.c_str());

    const HINSTANCE launchResult = ShellExecuteW(
        nullptr,
        L"open",
        appPath.c_str(),
        nullptr,
        directory.c_str(),
        SW_SHOWNORMAL);

    if (reinterpret_cast<INT_PTR>(launchResult) <= 32)
    {
        MessageBoxW(
            nullptr,
            L"Mini Web Counter를 실행하지 못했습니다.",
            WindowTitle,
            MB_OK | MB_ICONERROR);
        return 4;
    }

    return 0;
}
