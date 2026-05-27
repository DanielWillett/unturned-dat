// UnturnedDatIconHandler.cpp : Implementation of CUnturnedDatIconHandler

#include "pch.h"
#include "UnturnedDatIconHandler.h"


// CUnturnedDatIconHandler

HRESULT __stdcall CUnturnedDatIconHandler::GetClassID(CLSID* pClassID)
{
    return E_NOTIMPL;
}

HRESULT __stdcall CUnturnedDatIconHandler::IsDirty(void)
{
    return E_NOTIMPL;
}

HRESULT __stdcall CUnturnedDatIconHandler::Load(LPCOLESTR pszFileName, DWORD dwMode)
{
    return S_OK;
}

HRESULT __stdcall CUnturnedDatIconHandler::Save(LPCOLESTR pszFileName, BOOL fRemember)
{
    return E_NOTIMPL;
}

HRESULT __stdcall CUnturnedDatIconHandler::SaveCompleted(LPCOLESTR pszFileName)
{
    return E_NOTIMPL;
}

HRESULT __stdcall CUnturnedDatIconHandler::GetCurFile(LPOLESTR* ppszFileName)
{
    return E_NOTIMPL;
}

HRESULT __stdcall CUnturnedDatIconHandler::GetIconLocation(UINT uFlags, PWSTR pszIconFile, UINT cchMax, int* piIndex, UINT* pwFlags)
{
    if (s_ModulePath[0] == 0) {
        ::GetModuleFileName(_pModule->GetModuleInstance(), s_ModulePath, _countof(s_ModulePath));
    }
    if (s_ModulePath[0] == 0)
        return S_FALSE;

    wcscpy_s(pszIconFile, min((UINT)wcslen(s_ModulePath) + 1, cchMax), s_ModulePath);
    //ATLTRACE(L"CUnturnedDatIconHandler::GetIconLocation: %s\n", pszIconFile);
    *piIndex = 0;

    // todo: if each file has a different icon this needs changed
    *pwFlags = GIL_PERCLASS;

    return S_OK;
}

HRESULT __stdcall CUnturnedDatIconHandler::Extract(PCWSTR pszFile, UINT nIconIndex, HICON* phiconLarge, HICON* phiconSmall, UINT nIconSize)
{
    return S_FALSE;
}
