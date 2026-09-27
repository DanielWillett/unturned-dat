#include "LexUnturnedDat.h"

bool lexerDarkMode;

Sci_Position LexerUnturnedDat::PropertySet(const char* key, const char* val)
{
    return optionSet.PropertySet(&options, key, val) ? 0 : -1;
}

Sci_Position LexerUnturnedDat::WordListSet(int n, const char* wl)
{
    WordList* list = nullptr;

    switch (n)
    {
    case 0:
        list = &keywords;
        break;
    case 1:
        list = &properties;
        break;
    }

    return list && list->Set(wl) ? 0 : -1;
}

/* container stack impl */

void LexerUnturnedDat::pushStack(DatContainer cont)
{
    containerStack.push_back(cont);
}

DatContainer LexerUnturnedDat::peekStack()
{
    return containerStack.empty()
        ? DatContainer::None
        : containerStack[containerStack.size() - 1];
}

bool LexerUnturnedDat::popStack(DatContainer cont)
{
    if (containerStack.empty())
    {
        return false;
    }
    
    for (std::vector<DatContainer>::iterator it = containerStack.end() - 1; it != containerStack.begin(); --it)
    {
        if (*it == cont)
        {
            it = containerStack.erase(it);
            return true;
        }
    }

    return false;
}

/* container stack impl (end) */

static void skipWhiteSpace(StyleContext& ctx, bool newLinesToo = true)
{
    while (ctx.More())
    {
        if (!iswspace(ctx.ch))
            break;

        if (!newLinesToo && UDAT_NEWLINE(ctx.ch))
            break;

        ctx.Forward();
        ctx.ChangeState(LC_DARK(UDAT_LC_DEFAULT));
    }

    ctx.Complete();
}

static void skipSpacesAndTabs(StyleContext& ctx)
{
    while (ctx.More())
    {
        if (ctx.ch != ' ' && ctx.ch != '\t')
            break;

        ctx.Forward();
        ctx.ChangeState(LC_DARK(UDAT_LC_DEFAULT));
    }

    ctx.Complete();
}

static void readComments(StyleContext& ctx)
{
    do
    {
        for (; ctx.More(); ctx.Forward())
        {
            if (UDAT_NEWLINE(ctx.ch))
            {
                ctx.Complete();
                skipWhiteSpace(ctx);
                break;
            }

            ctx.ChangeState(LC_DARK(UDAT_LC_COMMENT));
        }
    } while (ctx.ch == '/');
    ctx.Complete();
}

static void readQuotedString(StyleContext& ctx, bool isKey)
{
    ctx.SetState(LC_DARK(UDAT_LC_PUNCTUATION));
    if (!ctx.More()) return;

    const int state = LC_DARK(isKey ? UDAT_LC_PROPERTY : UDAT_LC_LIT_STRING);

    ctx.Forward();
    ctx.Complete();
    ctx.ChangeState(state);
    while (ctx.More())
    {
        if (ctx.ch == '\\')
        {
            switch (ctx.chNext)
            {
            case 'n':
            case 't':
            case '\\':
            case '"':
                ctx.Complete();
                ctx.ChangeState(LC_DARK(UDAT_LC_ESCAPE_SEQUENCE));
                ctx.Forward();
                ctx.Forward();
                ctx.Complete();
                ctx.ChangeState(state);
                continue;
            }
        }
        else if (ctx.ch == '"')
        {
            ctx.Complete();
            ctx.ChangeState(LC_DARK(UDAT_LC_PUNCTUATION));
            ctx.Forward();
            ctx.Complete();
            break;
        }

        ctx.Forward();
    }

    ctx.Complete();

    if (ctx.ch == ',')
    {
        ctx.ForwardSetState(LC_DARK(UDAT_LC_PUNCTUATION));
    }

    if (isKey)
    {
        skipSpacesAndTabs(ctx);
    }
    else
    {
        skipWhiteSpace(ctx);
        if (ctx.ch == '/')
            readComments(ctx);
    }
}

static void readKey(StyleContext& ctx)
{
    if (ctx.ch == '"')
    {
        readQuotedString(ctx, true);
        return;
    }

    ctx.ChangeState(LC_DARK(UDAT_LC_PROPERTY));
    for (; ctx.More(); ctx.Forward())
    {
        if (iswspace(ctx.ch))
            break;
    }

    ctx.Complete();
    skipSpacesAndTabs(ctx);
}

void LexerUnturnedDat::readContainer(StyleContext& ctx)
{
    switch (ctx.ch)
    {
    case '{':
        pushStack(DatContainer::Dictionary);
        break;

    case '[':
        pushStack(DatContainer::List);
        break;

    case '}':
        popStack(DatContainer::Dictionary);
        break;

    case ']':
        popStack(DatContainer::List);
        break;

    default:
        return;
    }

    ctx.ChangeState(LC_DARK(UDAT_LC_PUNCTUATION));
    ctx.Forward();
    if (ctx.ch == ',')
    {
        ctx.Forward();
    }
    ctx.Complete();

    skipWhiteSpace(ctx, false);
    if (ctx.ch == '/')
        readComments(ctx);

    skipWhiteSpace(ctx);
}

void LexerUnturnedDat::readValue(StyleContext& ctx)
{
    if (ctx.ch == '"')
    {
        readQuotedString(ctx, false);
        return;
    }

    while (ctx.More())
    {
        if (ctx.ch == '\\')
        {
            switch (ctx.chNext)
            {
            case 'n':
            case 't':
            case '\\':
                ctx.Complete();
                ctx.ChangeState(LC_DARK(UDAT_LC_ESCAPE_SEQUENCE));
                ctx.Forward();
                ctx.Forward();
                ctx.Complete();
                ctx.ChangeState(LC_DARK(UDAT_LC_LIT_STRING));
                continue;
            }
        }
        else if (UDAT_NEWLINE(ctx.ch))
        {
            break;
        }

        ctx.SetState(LC_DARK(UDAT_LC_LIT_STRING));
        ctx.Forward();
    }

    ctx.Complete();
    skipWhiteSpace(ctx);
}

void LexerUnturnedDat::readAnyValue(StyleContext& ctx)
{
    switch (ctx.ch)
    {
    case '{':
    case '[':
        readContainer(ctx);
        break;

    default:
        readValue(ctx);
        break;
    }
}

void LexerUnturnedDat::Lex(Sci_PositionU startPos, Sci_Position length, int initStyle, IDocument* pAccess)
{
    DBG_UNREFERENCED_PARAMETER(length);
    DBG_UNREFERENCED_PARAMETER(initStyle);

    LexAccessor styler(pAccess);

    // too lazy to make this work properly so just re-lex the entire file
    Sci_Position lineStart = 0;

    StyleContext ctx(lineStart, length + (startPos - lineStart), initStyle, styler);

    while (ctx.More())
    {
        skipWhiteSpace(ctx);
        if (ctx.ch == '/') {
            readComments(ctx);
        }

        // at first character on new non-comment line

        if (peekStack() == DatContainer::List)
        {
            if (ctx.ch == '}' || ctx.ch == ']')
                readContainer(ctx);
            else
                readAnyValue(ctx);
        }
        else
        {
            readKey(ctx);
            if (UDAT_NEWLINE(ctx.ch)) {
                skipWhiteSpace(ctx);
            }
            else {
                readAnyValue(ctx);
            }
            readContainer(ctx);
        }
    }

    ctx.Complete();
    containerStack.clear();
}

void LexerUnturnedDat::Fold(Sci_PositionU startPos, Sci_Position length, int initStyle, IDocument* pAccess)
{
    DBG_UNREFERENCED_PARAMETER(startPos);
    DBG_UNREFERENCED_PARAMETER(length);
    DBG_UNREFERENCED_PARAMETER(initStyle);
    DBG_UNREFERENCED_PARAMETER(pAccess);
}