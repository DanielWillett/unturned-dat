using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Values;

#pragma warning disable CA2231

/// <summary>
/// Checks whether or not the given property has a value.
/// <para>
/// Supports one index parameter.
/// </para>
/// <para>
/// Supported targets:
/// <list type="bullet">
///     <item><c>#Self</c></item>
///     <item><c>#This</c></item>
///     <item>Any property reference.</item>
/// </list>
/// </para>
/// <para>
/// Syntax:<br/>
/// <c>#Target.Indices</c><br/>
/// <c>#Target.Indices[0]</c><br/>
/// <c>#Target.Indices[0]{"PreventSelfReference":true}</c><br/>
/// <c>#Target.Indices[-1]</c> (starts from end of array)
/// </para>
/// </summary>
public readonly struct IndicesProperty : IIndexableDataRefProperty, IEquatable<IndicesProperty>
{
    /// <summary>
    /// The index being referred to, or <see langword="null"/> to reference the entire array.
    /// </summary>
    public int? Index { get; }

    /// <summary>
    /// When used with <c>ValueTemplateGroupReference</c>, makes it an error to reference the current object.
    /// </summary>
    public bool PreventSelfReference { get; }

    /// <inheritdoc />
    public OneOrMore<int> Indices => Index.HasValue ? new OneOrMore<int>(Index.Value) : OneOrMore<int>.Null;

    /// <inheritdoc />
    public string PropertyName => "Indices";

    /// <summary>
    /// The type returned by this property for a list of indices.
    /// </summary>
    [field: MaybeNull]
    internal static ListType<int, int> ListType => field ??= Types.ListType.Create(Int32Type.Instance);

    public IndicesProperty(int? index, bool preventSelfReference)
    {
        Index = index;
        PreventSelfReference = preventSelfReference;
    }

    /// <summary>
    /// Gets the target index for an array of the given length, or -1 to reference the whole array.
    /// </summary>
    /// <remarks>The returned index may be out of range, in which case a <see langword="null"/> value should be returned.</remarks>
    public int GetIndex(int length)
    {
        if (!Index.HasValue)
            return -1;

        int index = Index.Value;
        if (index < 0)
        {
            index = length + index;
            if (index < 0)
                return 0;
        }

        return index;
    }

    /// <inheritdoc />
    public bool Equals(IndicesProperty other)
    {
        return other.Index == Index;
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is IndicesProperty prop && Equals(prop);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(767241444, Index);
    }

    internal static IndicesProperty Create(OneOrMore<int> indices, OneOrMore<KeyValuePair<string, object?>> properties)
    {
        return new IndicesProperty(
            index: indices.Length > 0 ? indices[0] : null,
            preventSelfReference: (bool)properties.GetValueOrDefault(nameof(PreventSelfReference), BoxedPrimitives.False, StringComparison.OrdinalIgnoreCase)
        );
    }

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
        return new DataRefProperty<IndicesProperty>(target, Create(indices, properties));
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
        return new DataRefProperty<IndicesProperty, TValue>(type, target, Create(indices, properties));
    }

    internal static bool TryGetCurrentIndices<TVisitor>(DatProperty? currentProperty, in IndicesProperty property, ref TVisitor visitor)
        where TVisitor : IValueVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (property.Index is >= 0)
        {
            int index = property.Index.Value;
            foreach (IObjectStackContext context in DatObjectStack.AsEnumerable())
            {
                if (context is not ObjectStackListContext listContext)
                    continue;

                if (--index >= 0)
                    continue;

                visitor.Accept(Int32Type.Instance, listContext.Index);
                return true;
            }

            return false;
        }

        using DatObjectStack.DatObjectStackEnumerator enumerator = DatObjectStack.AsEnumerable().GetEnumerator();
        using PooledList<int> indices = new PooledList<int>(enumerator.Count);
        while (enumerator.MoveNext())
        {
            if (enumerator.Current is not ObjectStackListContext listContext)
                continue;

            indices.Add(listContext.Index);
        }

        if (indices.Count == 0 && property.Index.HasValue)
        {
            return false;
        }

        int targetIndex = property.GetIndex(indices.Count);
        if (targetIndex < 0)
        {
            ListType<int, int> type = IndicesProperty.ListType;
            visitor.Accept(type, new EquatableArray<int>(indices.ToArray()));
        }
        else
        {
            visitor.Accept(Int32Type.Instance, indices[targetIndex]);
        }

        return true;
    }
}