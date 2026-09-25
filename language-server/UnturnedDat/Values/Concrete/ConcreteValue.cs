using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;
using UnturnedDat.Data.Values.Expressions;

namespace UnturnedDat.Data.Values;

#pragma warning disable CS8500

/// <summary>
/// A concrete/constant value that isn't dynamic in any way.
/// </summary>
/// <remarks>Create using <see cref="Value.Create"/>.</remarks>
/// <typeparam name="TValue">The type of value.</typeparam>
public sealed class ConcreteValue<TValue>
    : IValue<TValue>,
      IEquatable<ConcreteValue<TValue>?>,
      IValueExpressionNode
    where TValue : IEquatable<TValue>
{
    private readonly TValue? _value;

    /// <summary>
    /// The value stored in this object.
    /// </summary>
    /// <remarks>Check <see cref="IsNull"/> before using this property.</remarks>
    public TValue? Value => _value;

    /// <summary>
    /// Whether or not the value stored in this object is a null value.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsNull { get; }

    /// <inheritdoc />
    public IType<TValue> Type { get; }

    internal ConcreteValue(IType<TValue> type)
    {
        IsNull = true;
        Type = type;
    }

    internal ConcreteValue(TValue value, IType<TValue> type)
    {
        _value = value;
        IsNull = _value == null;
        Type = type;
    }

    /// <inheritdoc />
    public bool TryGetConcreteValue(out Optional<TValue> value)
    {
        value = IsNull ? Optional<TValue>.Null : new Optional<TValue>(_value);
        return true;
    }

    /// <inheritdoc />
    public bool TryEvaluateValue(out Optional<TValue> value, ref FileEvaluationContext ctx)
    {
        return TryGetConcreteValue(out value);
    }

    /// <inheritdoc />
    public bool VisitConcreteValue<TVisitor>(ref TVisitor visitor)
        where TVisitor : IValueVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Type, IsNull ? Optional<TValue>.Null : new Optional<TValue>(_value));
        return true;
    }

    /// <inheritdoc />
    public bool VisitValue<TVisitor>(ref TVisitor visitor, ref FileEvaluationContext ctx)
        where TVisitor : IValueVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Type, IsNull ? Optional<TValue>.Null : new Optional<TValue>(_value));
        return true;
    }

    /// <inheritdoc cref="IEquatable{T}"/>
    public bool Equals(TValue? value)
    {
        return IsNull ? value == null : value != null && _value.Equals(value);
    }

    /// <inheritdoc />
    public bool Equals(ConcreteValue<TValue>? value)
    {
        return IsNull ? value == null || value.IsNull : value is { IsNull: false, _value: not null } && _value.Equals(value._value);
    }

    bool IEquatable<IExpressionNode?>.Equals(IExpressionNode? other)
    {
        switch (other)
        {
            case null:
                return IsNull;

            case IValue<TValue> strongValue:
                if (strongValue.IsNull)
                    return IsNull;
                if (!strongValue.TryGetConcreteValue(out Optional<TValue> optVal))
                    return false;
                return optVal.HasValue ? !IsNull && optVal.Value.Equals(Value) : IsNull;

            case IValue value:
                
                EqualityVisitor<TValue> visitor;
                visitor.IsNull = IsNull;
                visitor.Value = Value;
                visitor.IsEqual = false;
                visitor.CaseInsensitive = false;
                visitor.Success = false;
                value.VisitConcreteValue(ref visitor);
                return visitor.IsEqual;

            default:
                return false;
        }
    }

    /// <inheritdoc />
    public bool Equals(IValue? other) => Equals(other as ConcreteValue<TValue>);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj switch
        {
            ConcreteValue<TValue> v => Equals(v),
            TValue v => Equals(v),
            null => IsNull,
            _ => false
        };
    }

    /// <inheritdoc />
    public void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        if (IsNull)
        {
            writer.WriteNullValue();
        }
        else
        {
            Type.Parser.WriteValueToJson(writer, _value, Type, options);
        }
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return IsNull ? 1302072072 : HashCode.Combine(_value, 1302072072);
    }

    bool IValue.TryCreateConcreteValue(ref FileEvaluationContext ctx, [NotNullWhen(true)] out IValue? value)
    {
        if (CommonTypes.IsPrimitiveType<TValue>() || _value == null)
        {
            value = this;
            return true;
        }

        if (!TryCreateConcreteValue(_value, out TValue? newValue, ref ctx))
        {
            value = null;
            return false;
        }

        if (!typeof(TValue).IsValueType && (object)_value == (object?)newValue)
        {
            value = this;
            return true;
        }

        value = newValue == null ? Values.Value.Null(Type) : Values.Value.Create(newValue, Type);
        return true;
    }

    internal static bool TryCreateConcreteValue(TValue? value, out TValue? newValue, ref FileEvaluationContext ctx)
    {
        switch (value)
        {
            case null:
                newValue = default;
                return true;

            case IValue v:
                if (v.TryCreateConcreteValue(ref ctx, out IValue? v2))
                {
                    switch (v2)
                    {
                        case TValue newV:
                            newValue = newV;
                            return true;

                        case ConcreteValue<TValue> conc:
                            newValue = conc._value;
                            return true;

                        case { IsNull: true }:
                            newValue = default;
                            return !typeof(TValue).IsValueType;
                    }
                }

                break;

            case IEquatableArray<TValue> array:
#if NET9_0_OR_GREATER
                scoped
#endif
                    EquatableArrayReduceVisitor<TValue> visitor = default;
#if NET9_0_OR_GREATER
                visitor.EvalContext = ref ctx;
                array.Visit(ref visitor);
#else
                unsafe
                {
                    fixed (FileEvaluationContext* ctxPtr = &ctx)
                    {
                        visitor.EvalContext = ctxPtr;
                        array.Visit(ref visitor);
                    }
                }
#endif
                if (visitor.Success)
                {
                    newValue = visitor.ReducedValue;
                    return true;
                }
                break;
        }

        newValue = value;
        return true;
    }
}

#if NET9_0_OR_GREATER
file ref struct EquatableArrayReduceVisitor<TArrayType> : IEquatableArrayVisitor
#else
file unsafe struct EquatableArrayReduceVisitor<TArrayType> : IEquatableArrayVisitor
#endif
{
    public TArrayType ReducedValue;
    public bool Success;
#if NET9_0_OR_GREATER
    public ref FileEvaluationContext EvalContext;
#else
    public FileEvaluationContext* EvalContext;
#endif

    public void Accept<T>(EquatableArray<T> array) where T : IEquatable<T>
    {
#if NET9_0_OR_GREATER
        ref FileEvaluationContext ctx = ref EvalContext;
#else
        ref FileEvaluationContext ctx = ref Unsafe.AsRef<FileEvaluationContext>(EvalContext);
#endif
        T[] newArray = new T[array.Array.Length];
        for (int i = 0; i < array.Array.Length; ++i)
        {
            if (!ConcreteValue<T>.TryCreateConcreteValue(array.Array[i], out newArray[i], ref ctx))
                return;
        }

        EquatableArray<T> newEqArray = new EquatableArray<T>(newArray);
        Success = true;
        ReducedValue = Unsafe.As<EquatableArray<T>, TArrayType>(ref newEqArray);
    }
}

#pragma warning restore CS8500