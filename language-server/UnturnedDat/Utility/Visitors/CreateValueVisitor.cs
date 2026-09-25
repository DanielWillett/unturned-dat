using System;
using System.Diagnostics.CodeAnalysis;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Values;

namespace UnturnedDat.Data.Utility;

/// <summary>
/// Creates a <see cref="IValue"/> as the result of a <see cref="IValueVisitor"/>.
/// </summary>
internal struct CreateValueVisitor : IValueVisitor
{
    public static IValue? CreateFromValue(IValue value, ref FileEvaluationContext ctx)
    {
        CreateValueVisitor v;
        v.NewValue = null;
        value.VisitValue(ref v, ref ctx);
        return v.NewValue;
    }

    public static bool TryCreateFromValue(IValue value, ref FileEvaluationContext ctx, [NotNullWhen(true)] out IValue? newValue)
    {
        CreateValueVisitor v;
        v.NewValue = null;
        value.VisitValue(ref v, ref ctx);
        if (v.NewValue != null)
        {
            newValue = v.NewValue;
            return true;
        }

        newValue = null;
        return false;
    }

    public IValue? NewValue;

    public void Accept<TValue>(IType<TValue> type, Optional<TValue> value) where TValue : IEquatable<TValue>
    {
        NewValue = value.HasValue ? Value.Create(value.Value, type) : Value.Null(type);
    }
}
