using UnturnedDat.Data.Properties;

namespace UnturnedDat.Data.Values;

internal sealed class ObjectStackListContext : IObjectStackContext
{
    public PropertyResolutionContext Context { get; }
    public int Index { get; internal set; }
    public int Count { get; }
    public IObjectStackContext? Parent { get; set; }

    /// <summary>
    /// Reading a single value instead of a list of values.
    /// </summary>
    public bool IsSingle { get; }

    public ObjectStackListContext(PropertyResolutionContext context, int index, int count, bool isSingle = false)
    {
        Context = context;
        Index = index;
        IsSingle = isSingle;
        Count = count;
    }

    ObjectStackContextType IObjectStackContext.Type => ObjectStackContextType.ListElement;
}