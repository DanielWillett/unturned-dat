using System;
using UnturnedDat.Data.Properties;

namespace UnturnedDat.Data.Values;

/// <remarks>
/// Note: <see cref="IDisposable.Dispose"/> will be called on pop if implemented.
/// </remarks>
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