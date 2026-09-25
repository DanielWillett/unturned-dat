using System;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Values;

namespace UnturnedDat.Data.Utility;

/// <summary>
/// Visitor that has a boolean value that determines whether or not it was invoked.
/// </summary>
internal struct DetectorVisitor : IGenericVisitor, IValueVisitor, ITypeVisitor
{
    public bool WasVisited;

    public void Accept<T>(T? value) where T : IEquatable<T>
    {
        WasVisited = true;
    }
    public void Accept<TValue>(IType<TValue> type, Optional<TValue> value) where TValue : IEquatable<TValue>
    {
        WasVisited = true;
    }
    public void Accept<TValue>(IType<TValue> type) where TValue : IEquatable<TValue>
    {
        WasVisited = true;
    }
}
