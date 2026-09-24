using UnturnedDat.Data.Properties;

namespace UnturnedDat.Data.Values;

internal sealed class ObjectStackDictionaryContext : IObjectStackContext
{
    public int Index { get; internal set; }
    public int Count { get; }
    public string Key { get; internal set; }
    public IObjectStackContext? Parent { get; set; }

    public ObjectStackDictionaryContext(int index, string key, int count)
    {
        Index = index;
        Key = key;
        Count = count;
    }

    ObjectStackContextType IObjectStackContext.Type => ObjectStackContextType.DictionaryPair;
    PropertyResolutionContext IObjectStackContext.Context => PropertyResolutionContext.Modern;
}