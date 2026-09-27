#include <string.h>
#include <wtypes.h>
#include <string_view>

#include "PluginDefinition.h"
#include "Lexer/LexerInterface.h"
#include <filesystem>

extern FuncItem funcItem[nbFunc];
extern NppData nppData;

static HMODULE module;

static bool hasDarkTheme;

using namespace std::literals;
using namespace std::filesystem;

BOOL APIENTRY DllMain(HANDLE hModule, DWORD reasonForCall, LPVOID /*lpReserved*/)
{
	try {

		switch (reasonForCall)
		{
			case DLL_PROCESS_ATTACH:
				pluginInit(hModule);
				module = (HMODULE)hModule;
				break;

			case DLL_PROCESS_DETACH:
				pluginCleanUp();
				break;

			case DLL_THREAD_ATTACH:
				break;

			case DLL_THREAD_DETACH:
				break;
		}
	}
	catch (...) { return FALSE; }

    return TRUE;
}

static void darkModeInit();

extern "C"
{
	__declspec(dllexport) FuncItem* getFuncsArray(int* nbF)
	{
		*nbF = nbFunc;
		return funcItem;
	}

	__declspec(dllexport) const TCHAR* getName()
	{
		return pluginName;
	}

	__declspec(dllexport) void setInfo(NppData notpadPlusData)
	{
		nppData = notpadPlusData;

		commandMenuInit();

		darkModeInit();
	}

	__declspec(dllexport) void beNotified(SCNotification* notifyCode)
	{
		switch (notifyCode->nmhdr.code)
		{
		case NPPN_SHUTDOWN:
		{
			commandMenuCleanUp();
			break;
		}
		case NPPN_DARKMODECHANGED:
		{
			darkModeInit();
			break;
		}

		default:
			return;
		}
	}

	__declspec(dllexport) LRESULT messageProc(UINT Message, WPARAM wParam, LPARAM lParam)
	{
		UNREFERENCED_PARAMETER(Message);
		UNREFERENCED_PARAMETER(wParam);
		UNREFERENCED_PARAMETER(lParam);

		return TRUE;
	}

#ifdef UNICODE
	__declspec(dllexport) BOOL isUnicode()
	{
		return TRUE;
	}
#endif
}

static void darkModeInit() {
	bool darkMode = SendMessage(nppData._nppHandle, NPPM_ISDARKMODEENABLED, 0, 0);
	hasDarkTheme = darkMode;
	LexerUnturnedDat::HandleDarkModeUpdated(darkMode);
}