using UnturnedDat.Data.Properties;

namespace UnturnedDat.Data.Values;

internal interface IObjectStackContext
{
    ObjectStackContextType Type { get; }
    PropertyResolutionContext Context { get; }
    IObjectStackContext? Parent { get; internal set; }
}

internal enum ObjectStackContextType
{
    Object,
    ListElement,
    DictionaryPair
}