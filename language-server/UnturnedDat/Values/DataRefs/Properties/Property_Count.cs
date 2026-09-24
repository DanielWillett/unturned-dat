using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Values;

#pragma warning disable CA2231

/// <summary>
/// Returns the number of elements in a list or dictionary.
/// <para>
/// Supported targets:
/// <list type="bullet">
///     <item><c>#Self</c></item>
///     <item>Any property reference.</item>
/// </list>
/// </para>
/// <para>
/// Syntax:<br/>
/// <c>#Target.Count</c>
/// </para>
/// </summary>
public readonly struct CountProperty : IDataRefProperty, IEquatable<CountProperty>
{
    /// <inheritdoc />
    public string PropertyName => "Count";

    /// <inheritdoc />
    public bool Equals(CountProperty other) => true;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is CountProperty;

    /// <inheritdoc />
    public override int GetHashCode() => 413680222;

    /// <inheritdoc />
#if NET7_0_OR_GREATER
    public static IDataRef CreateDataRef(
#else
    public IDataRef CreateDataRef(
#endif
        IDataRefTarget target,
        OneOrMore<int> indices,
        OneOrMore<KeyValuePair<string, object?>> properties)
    {
        return new DataRefProperty<CountProperty>(target, default);
    }

    /// <inheritdoc />
#if NET7_0_OR_GREATER
    public static IDataRef<TValue> CreateDataRef<TValue>(
#else
    public IDataRef<TValue> CreateDataRef<TValue>(
#endif
        IType<TValue> type,
        IDataRefTarget target,
        OneOrMore<int> indices,
        OneOrMore<KeyValuePair<string, object?>> properties
    ) where TValue : IEquatable<TValue>
    {
        return new DataRefProperty<CountProperty, TValue>(type, target, default);
    }

    internal static bool TryGetCurrentCount(DatProperty currentProperty, in CountProperty property, out int count, ref FileEvaluationContext ctx)
    {
        if (!PropertyReference.TryFindPropertyOwnerFromContext(currentProperty, out ObjectStackObjectContext? objectContext))
        {
            count = 0;
            return false;
        }

        CountValueVisitor countVisitor;
        countVisitor.Count = 0;
        countVisitor.Success = false;

        if (objectContext != null)
        {
            if (objectContext.TryGetPropertyValue(currentProperty, out IValue? value))
            {
                value.VisitValue(ref countVisitor, ref ctx);

                if (countVisitor.Success)
                {
                    count = countVisitor.Count;
                    return true;
                }
            }
        }
        else
        {
            currentProperty.VisitValue(ref countVisitor, ref ctx, missingValueBahvior: TypeParserMissingValueBehavior.FallbackToDefaultValue);
            if (countVisitor.Success)
            {
                count = countVisitor.Count;
                return true;
            }
        }

        count = 0;
        return false;
    }

    private struct CountValueVisitor : IValueVisitor
    {
        public int Count;
        public bool Success;

        public void Accept<TValue>(IType<TValue> type, Optional<TValue> value)
            where TValue : IEquatable<TValue>
        {
            switch (value.Value)
            {
                case IEquatableArray<TValue> equatableArray:
                    Array array = equatableArray.Array;
                    Count = array?.Length ?? 0;
                    Success = true;
                    return;

                default:
                    Count = 1;
                    Success = true;
                    return;
            }
        }
    }
}