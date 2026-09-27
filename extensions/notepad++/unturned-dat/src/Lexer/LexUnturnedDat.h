#pragma once

#include <assert.h>
#include <windows.h>
#include <string>
#include <map>
#include <string_view>

#include "../Host/Scintilla.h"
#include "../Host/ILexer.h"
#include "../Host/LexAccessor.h"
#include "../Host/LexerModule.h"
#include "../Host/StyleContext.h"
#include "../Host/CharacterSet.h"
#include "../Host/DefaultLexer.h"
#include "../Host/OptionSet.h"
#include "../Host/WordList.h"

using namespace Scintilla;
using namespace Lexilla;

// white space
#define UDAT_LC_DEFAULT 0

// /comment, "Key" /comment, "Key" "Value" /comment
#define UDAT_LC_COMMENT 1

// "1", 1, "2.5", 2.5
#define UDAT_LC_LIT_NUMBER 2

// "0123456789012345601234567890123456", 0123456789012345601234567890123456
#define UDAT_LC_LIT_GUID 3

// "Value", Value
#define UDAT_LC_LIT_STRING 4

// property name
#define UDAT_LC_PROPERTY 5

// this is the 'x' in the following type of value:
//   0123456789012345601234567890123456 x 2
//   this x 4
#define UDAT_LC_PUNCTUATION 6

// true, false, this
#define UDAT_LC_KEYWORD 7

// \\, \r, \n, \t
#define UDAT_LC_ESCAPE_SEQUENCE 7

// offset for dark properties from the previous defines
#define UDAT_DARK 9

// defined in .cpp
static Lexilla::LexicalClass lexicalClasses[UDAT_DARK * 2] =
{
    0, "UDAT_LC_DEFAULT", "default", "White space (Light Theme)",
    1, "UDAT_LC_COMMENT", "comment line", "Comment (Light Theme)",
    2, "UDAT_LC_LIT_NUMBER", "literal numeric", "Number (Light Theme)",
    3, "UDAT_LC_LIT_GUID", "literal numeric", "GUID (Light Theme)",
    4, "UDAT_LC_LIT_STRING", "literal string", "String (Light Theme)",
    5, "UDAT_LC_PROPERTY", "identifier", "Property Name (Light Theme)",
    6, "UDAT_LC_PUNCTUATION", "operator", "Punctuation (Light Theme)",
    7, "UDAT_LC_KEYWORD", "keyword", "Keywords (\"true\", \"false\", \"this\") (Light Theme)",
    8, "UDAT_LC_ESCAPE_SEQUENCE", "literal string escapesequence", "String Escape Sequence (Light Theme)",

    UDAT_DARK + 0, "UDAT_LC_DEFAULT_D", "default", "White space (Dark Theme)",
    UDAT_DARK + 1, "UDAT_LC_COMMENT_D", "comment line", "Comment (Dark Theme)",
    UDAT_DARK + 2, "UDAT_LC_LIT_NUMBER_D", "literal numeric", "Number (Dark Theme)",
    UDAT_DARK + 3, "UDAT_LC_LIT_GUID_D", "literal numeric", "GUID (Dark Theme)",
    UDAT_DARK + 4, "UDAT_LC_LIT_STRING_D", "literal string", "String (Dark Theme)",
    UDAT_DARK + 5, "UDAT_LC_PROPERTY_D", "identifier", "Property Name (Dark Theme)",
    UDAT_DARK + 6, "UDAT_LC_PUNCTUATION_D", "operator", "Punctuation (Dark Theme)",
    UDAT_DARK + 7, "UDAT_LC_KEYWORD_D", "keyword", "Keywords (\"true\", \"false\", \"this\") (Dark Theme)",
    UDAT_DARK + 8, "UDAT_LC_ESCAPE_SEQUENCE_D", "literal string escapesequence", "String Escape Sequence (Dark Theme)",
};

#define LC_DARK(id) ((lexerDarkMode ? UDAT_DARK : 0) + (id))
#define UDAT_NEWLINE(c) ((c) == '\r' || (c) == '\n')

const char* const udatWordLists[] =
{
    "Keywords",
    "Properties",
    nullptr
};

struct OptionsUnturnedDat
{

};

struct OptionSetUnturnedDat : public OptionSet<OptionsUnturnedDat>
{
    OptionSetUnturnedDat()
    {
        DefineWordListSets(udatWordLists);
    }
};

extern bool lexerDarkMode;

enum DatContainer {
    None,
    Dictionary,
    List
};

class LexerUnturnedDat : public DefaultLexer
{

private:
    WordList keywords;
    WordList properties;
    OptionsUnturnedDat options;
    OptionSetUnturnedDat optionSet;

    std::vector<DatContainer> containerStack{};

    void pushStack(DatContainer cont);
    DatContainer peekStack();
    bool popStack(DatContainer cont);

    void readContainer(StyleContext& ctx);
    void readValue(StyleContext& ctx);
    void readAnyValue(StyleContext& ctx);

public:

    static void HandleDarkModeUpdated(bool newDarkMode)
    {
        lexerDarkMode = newDarkMode;
    }

    static constexpr const char* name() { return "Unturned Data File"; }
    static constexpr const TCHAR* statusText() { return L"Unturned Data File"; }

    LexerUnturnedDat() : DefaultLexer(name(), 0, lexicalClasses, std::size(lexicalClasses))
    {
        containerStack = std::vector<DatContainer>(16);
    }

    virtual ~LexerUnturnedDat() { }

    Sci_Position SCI_METHOD PropertySet(const char* key, const char* val) override;
    Sci_Position SCI_METHOD WordListSet(int n, const char* wl) override;
    void SCI_METHOD Lex(Sci_PositionU startPos, Sci_Position length, int initStyle, IDocument* pAccess) override;
    void SCI_METHOD Fold(Sci_PositionU startPos, Sci_Position length, int initStyle, IDocument* pAccess) override;

    static ILexer5* LexerFactoryUnturnedDat()
    {
        try
        {
            return new LexerUnturnedDat();
        }
        catch (...)
        {
            return nullptr;
        }
    }
};