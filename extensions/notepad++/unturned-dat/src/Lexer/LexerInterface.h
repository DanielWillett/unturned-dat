#pragma once

#ifndef UDAT_LEXINT_H
#define UDAT_LEXINT_H

#include <string.h>
#include <wtypes.h>

#include "../Host/ILexer.h"
#include "../Host/LexerModule.h"
#include "LexUnturnedDat.h"

using namespace Lexilla;

namespace UnturnedDatNpp {

	extern "C" {

		__declspec(dllexport) int SCI_METHOD GetLexerCount()
		{
			return 1;
		}

		__declspec(dllexport) void SCI_METHOD GetLexerName(unsigned int index, char* name, int buflength)
		{
			switch (index)
			{
			case 0:
				strncpy_s(name, buflength, LexerUnturnedDat::name(), _TRUNCATE);
				break;
			}
		}

		__declspec(dllexport) void SCI_METHOD GetLexerStatusText(unsigned int index, WCHAR* text, int buflength)
		{
			switch (index)
			{
			case 0:
				wcsncpy_s(text, buflength, LexerUnturnedDat::statusText(), _TRUNCATE);
				break;
			}
		}

		__declspec(dllexport) LexerFactoryFunction SCI_METHOD GetLexerFactory(unsigned int index)
		{
			switch (index)
			{
			case 0:
				return LexerUnturnedDat::LexerFactoryUnturnedDat;
			}

			return 0;
		}

		__declspec(dllexport) Scintilla::ILexer5* SCI_METHOD CreateLexer(const char* name)
		{
			if (strcmp(name, LexerUnturnedDat::name()) == 0)
			{
				return LexerUnturnedDat::LexerFactoryUnturnedDat();
			}

			return nullptr;
		}

		__declspec(dllexport) const char* SCI_METHOD GetNameSpace()
		{
			return "UnturnedDatNpp";
		}
	}
}

#endif