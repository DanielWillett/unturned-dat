using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Values;

/// <summary>
/// A value parsed from a <see cref="DatCustomType"/>.
/// </summary>
public sealed class DatObjectValue :
    IValue<DatObjectValue>,
    IEquatable<DatObjectValue?>
{
    private int? _hash;

    private readonly bool _isConcrete;

    /// <inheritdoc cref="IValue{TValue}.Type"/>
    public DatCustomType Type { get; }

    /// <summary>
    /// Defined properties for this object.
    /// </summary>
    public ImmutableArray<DatObjectPropertyValue> Properties { get; }

    public DatObjectValue(DatCustomType type, ImmutableArray<DatObjectPropertyValue> properties)
    {
        Type = type;
        Properties = properties;
        _isConcrete = true;
        foreach (DatObjectPropertyValue property in properties)
        {
            DetectorVisitor v;
            v.WasVisited = false;
            property.Value.VisitConcreteValue(ref v);
            if (v.WasVisited)
                continue;

            _isConcrete = false;
            break;
        }
    }

    internal DatObjectValue(DatCustomType type, ImmutableArray<DatObjectPropertyValue> properties, bool isConcrete)
    {
        Type = type;
        Properties = properties;
        _isConcrete = isConcrete;
    }

    /// <summary>
    /// Attempt to get a property by it's primary key.
    /// </summary>
    public bool TryGetProperty(ReadOnlySpan<char> key, out DatObjectPropertyValue value)
    {
        foreach (DatObjectPropertyValue v in Properties)
        {
            if (!key.Equals(v.Property.Key, StringComparison.OrdinalIgnoreCase))
                continue;

            value = v;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        Type.WriteValueToJson(writer, this, Type, options);
    }

    /// <inheritdoc />
    public bool Equals(DatObjectValue? other)
    {
        if (other == null)
            return false;

        if (this == other)
            return true;

        if (!Type.Equals(other.Type))
            return false;

        if (_isConcrete != other._isConcrete)
            return false;

        ImmutableArray<DatObjectPropertyValue> thisProps = Properties;
        ImmutableArray<DatObjectPropertyValue> otherProps = other.Properties;
        
        if (thisProps.Length != otherProps.Length)
            return false;

        for (int i = 0; i < thisProps.Length; ++i)
        {
            if (!thisProps[i].Equals(otherProps[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool Equals(IValue? other)
    {
        return other is DatObjectValue v && Equals(v);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is DatObjectValue v && Equals(v);
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        if (_hash.HasValue)
            return _hash.Value;

        HashCode hc = new HashCode();
        hc.Add(1596513245);
        hc.Add(Type);
        hc.Add(_isConcrete);
        ImmutableArray<DatObjectPropertyValue> properties = Properties;
        hc.Add(properties.Length);
        foreach (DatObjectPropertyValue v in properties)
        {
            hc.Add(v.GetHashCode());
        }

        _hash = hc.ToHashCode();
        return _hash.Value;
    }

    /// <inheritdoc cref="IValue.TryCreateConcreteValue"/>
    /// <summary>
    /// If this object has any non-concrete property values, such as property-refs, this function will instantly resolve those and replace them with concrete values.
    /// </summary>
    public bool TryCreateConcreteValue(ref FileEvaluationContext ctx, [NotNullWhen(true)] out DatObjectValue? value)
    {
        if (_isConcrete)
        {
            value = this;
            return true;
        }

        ImmutableArray<DatObjectPropertyValue> properties = Properties;
        ImmutableArray<DatObjectPropertyValue>.Builder bldr = ImmutableArray.CreateBuilder<DatObjectPropertyValue>(properties.Length);
        for (int i = 0; i < properties.Length; ++i)
        {
            DatObjectPropertyValue prop = properties[i];
            if (!prop.Value.TryCreateConcreteValue(ref ctx, out IValue? val))
            {
                value = null;
                return false;
            }

            bldr.Add(new DatObjectPropertyValue(val, prop.Property, prop.Node));
        }

        value = new DatObjectValue(Type, bldr.MoveToImmutableOrCopy(), isConcrete: true);
        return true;
    }

    bool IValue<DatObjectValue>.TryGetConcreteValue(out Optional<DatObjectValue> value)
    {
        value = this;
        return true;
    }

    bool IValue<DatObjectValue>.TryEvaluateValue(out Optional<DatObjectValue> value, ref FileEvaluationContext ctx)
    {
        value = this;
        return true;
    }

    bool IValue.VisitConcreteValue<TVisitor>(ref TVisitor visitor)
    {
        if (!_isConcrete)
        {
            return false;
        }

        visitor.Accept(Type, new Optional<DatObjectValue>(this));
        return true;
    }

    bool IValue.VisitValue<TVisitor>(ref TVisitor visitor, ref FileEvaluationContext ctx)
    {
        visitor.Accept(Type, new Optional<DatObjectValue>(this));
        return true;
    }

    bool IValue.TryCreateConcreteValue(ref FileEvaluationContext ctx, [NotNullWhen(true)] out IValue? value)
    {
        if (!TryCreateConcreteValue(ref ctx, out DatObjectValue? newValue))
        {
            value = null;
            return false;
        }

        value = newValue;
        return true;
    }

    IType<DatObjectValue> IValue<DatObjectValue>.Type => Type;
    bool IValue.IsNull => false;
}

/// <summary>
/// A key-value pair for a property and it's value.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct DatObjectPropertyValue(IValue value, DatProperty property, ISourceNode? node = null) : IEquatable<DatObjectPropertyValue>
{
    /// <summary>
    /// The value of the property.
    /// </summary>
    public IValue Value { get; } = value;

    /// <summary>
    /// The property that was evaluated.
    /// </summary>
    public DatProperty Property { get; } = property;

    /// <summary>
    /// The node given for this property.
    /// </summary>
    /// <remarks>Usually a <see cref="IPropertySourceNode"/> unless this object was parsed from a string, in which case it'll be a <see cref="IValueSourceNode"/>.</remarks>
    public ISourceNode? Node { get; } = node;

    /// <inheritdoc />
    public bool Equals(DatObjectPropertyValue other)
    {
        return Property == other.Property && (Value?.Equals(other.Value) ?? other.Value == null);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DatObjectPropertyValue v && Equals(v);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Property.Key), Value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Value.TryGetConcreteValueAs(out Optional<string> v))
        {
            return v.HasValue ? $"\"{Property.Key}\" \"{v.Value}\"" : $"\"{Property.Key}\"";
        }

        return $"\"{Property.Key}\" {{{v}}}";
    }
}