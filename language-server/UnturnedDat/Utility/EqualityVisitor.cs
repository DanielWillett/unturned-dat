using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Values;

namespace UnturnedDat.Data.Utility;

/// <summary>
/// Compares one value with another of a different type.
/// </summary>
public struct EqualityVisitor<TValue> : IValueVisitor, IGenericVisitor
    where TValue : IEquatable<TValue>
{
    public bool IsEqual;
    public bool IsNull;
    public bool Success;
    public TValue? Value;
    public bool CaseInsensitive;

    /// <summary>
    /// Attempts to compare two values to see if they're equal.
    /// </summary>
    /// <typeparam name="TOtherValue">The type of the other value to compare with.</typeparam>
    /// <param name="value">The first value in the comparison.</param>
    /// <param name="otherValue">The other value in the comparison.</param>
    /// <param name="caseInsensitive">Whether or not to ignore case when comparing the values, if they're a <see cref="string"/> type.</param>
    /// <param name="isEqual">Whether or not the two values should be considered equal, given the parameters.</param>
    /// <returns>Whether or not a comparison could be made successfully between the two values.</returns>
    public static bool TryCompare<TOtherValue>(TValue value, TOtherValue otherValue, bool caseInsensitive, out bool isEqual)
        where TOtherValue : IEquatable<TOtherValue>
    {
        EqualityVisitor<TValue> v;
        v.IsEqual = false;
        v.IsNull = value == null;
        v.Value = value;
        v.CaseInsensitive = caseInsensitive;
        v.Success = false;

        v.Accept(otherValue);

        if (!v.Success)
        {
            isEqual = false;
            return false;
        }

        isEqual = v.IsEqual;
        return true;
    }

    /// <inheritdoc cref="TryCompare{TOtherValue}(TValue,TOtherValue,bool,out bool)"/>
    public static bool TryCompare<TOtherValue>(TValue value, TOtherValue otherValue, out bool isEqual)
        where TOtherValue : IEquatable<TOtherValue>
    {
        return TryCompare(value, otherValue, false, out isEqual);
    }

    /// <inheritdoc cref="TryCompare{TOtherValue}(TValue,TOtherValue,bool,out bool)"/>
    public static bool TryCompare<TOtherValue>(Optional<TValue> value, Optional<TOtherValue> otherValue, bool caseInsensitive, out bool isEqual)
        where TOtherValue : IEquatable<TOtherValue>
    {
        EqualityVisitor<TValue> v;
        v.IsEqual = false;
        v.IsNull = !value.HasValue;
        v.Value = value.Value;
        v.CaseInsensitive = caseInsensitive;
        v.Success = false;

        v.Accept(otherValue);

        if (!v.Success)
        {
            isEqual = false;
            return false;
        }

        isEqual = v.IsEqual;
        return true;
    }

    /// <inheritdoc cref="TryCompare{TOtherValue}(TValue,TOtherValue,bool,out bool)"/>
    public static bool TryCompare<TOtherValue>(Optional<TValue> value, Optional<TOtherValue> otherValue, out bool isEqual)
        where TOtherValue : IEquatable<TOtherValue>
    {
        return TryCompare(value, otherValue, false, out isEqual);
    }

    public void Accept<TOtherValue>(IType<TOtherValue> type, Optional<TOtherValue> optVal)
        where TOtherValue : IEquatable<TOtherValue>
    {
        Accept(optVal);
    }

    public void Accept<TOtherValue>(Optional<TOtherValue> optVal)
        where TOtherValue : IEquatable<TOtherValue>
    {
        Success = true;
        if (!optVal.HasValue)
        {
            IsEqual = IsNull;
            return;
        }

        if (IsNull)
        {
            IsEqual = false;
            return;
        }

        // already same type
        if (typeof(TValue) == typeof(TOtherValue))
        {
            if (CaseInsensitive && typeof(TValue) == typeof(string))
            {
                IsEqual = string.Equals(
                    MathMatrix.As<TValue?, string?>(Value),
                    MathMatrix.As<TOtherValue?, string?>(optVal.Value),
                    StringComparison.OrdinalIgnoreCase
                );
            }
            else
            {
                IsEqual = EqualityComparer<TValue>.Default.Equals(Value!, MathMatrix.As<TOtherValue, TValue>(optVal.Value));
            }
            return;
        }

        // compare char with string (need this to take priority over MathMatrix)
        if (typeof(TValue) == typeof(char))
        {
            if (typeof(TOtherValue) == typeof(string))
            {
                string s = MathMatrix.As<TOtherValue, string>(optVal.Value);
                if (s is { Length: 1 })
                {
                    char c = MathMatrix.As<TValue?, char>(Value);
                    IsEqual = CaseInsensitive ? char.ToLowerInvariant(c) == char.ToLowerInvariant(s[0]) : c == s[0];
                }
                else
                {
                    IsEqual = false;
                }
                return;
            }
        }
        else if (typeof(TOtherValue) == typeof(char))
        {
            if (typeof(TValue) == typeof(string))
            {
                string? s = MathMatrix.As<TValue?, string?>(Value);
                if (s is { Length: 1 })
                {
                    char c = MathMatrix.As<TOtherValue?, char>(optVal.Value);
                    IsEqual = CaseInsensitive ? char.ToLowerInvariant(s[0]) == char.ToLowerInvariant(c) : s[0] == c;
                }
                else
                {
                    IsEqual = false;
                }
                return;
            }
        }

        if (MathMatrix.IsValidMathExpressionInputType<TValue>()
            || MathMatrix.IsValidMathExpressionInputType<TOtherValue>())
        {
            ReduceLeft<TOtherValue> v;
            v.Success = false;
            v.RightValue = optVal.Value!;
            v.IsEqual = false;
            if (MathMatrix.TryReduce(Value!, ref v) && v.Success)
            {
                IsEqual = v.IsEqual;
                return;
            }
        }

        // compare Guid with GuidOrId (MathMatrix will take care of IDs)
        if (typeof(TOtherValue) == typeof(Guid))
        {
            if (typeof(TValue) == typeof(GuidOrId))
            {
                IsEqual = Unsafe.As<TValue, GuidOrId>(ref Value!).Equals(MathMatrix.As<TOtherValue, Guid>(optVal.Value));
                return;
            }
        }
        else if (typeof(TOtherValue) == typeof(GuidOrId))
        {
            if (typeof(TValue) == typeof(Guid))
            {
                IsEqual = MathMatrix.As<TOtherValue, GuidOrId>(optVal.Value).Equals(Unsafe.As<TValue, Guid>(ref Value!));
                return;
            }
        }

        // compare value with string
        if (typeof(TValue) == typeof(string))
        {
            if (TypeConverters.TryGet<TOtherValue>() is { } rightTypeConverter)
            {
                TypeConverterParseArgs<TOtherValue> ov = default;
                ov.Type = rightTypeConverter.DefaultType;
                ov.TextAsString = MathMatrix.As<TValue?, string?>(Value);
                if (rightTypeConverter.TryParse(ov.TextAsString, ref ov, out TOtherValue? parsedValue))
                {
                    IsEqual = EqualityComparer<TOtherValue>.Default.Equals(parsedValue, optVal.Value);
                    return;
                }
            }
        }
        else if (typeof(TOtherValue) == typeof(string))
        {
            if (TypeConverters.TryGet<TValue>() is { } rightTypeConverter)
            {
                TypeConverterParseArgs<TValue> ov = default;
                ov.Type = rightTypeConverter.DefaultType;
                ov.TextAsString = MathMatrix.As<TOtherValue?, string?>(optVal.Value);
                if (rightTypeConverter.TryParse(ov.TextAsString, ref ov, out TValue? parsedValue))
                {
                    IsEqual = EqualityComparer<TValue>.Default.Equals(Value!, parsedValue);
                    return;
                }
            }
        }

        if (Value is DatEnumValue enumValue)
        {
            EquateEnumAndValue(enumValue, optVal.Value);
            return;
        }

        if (optVal.Value is DatEnumValue enumValue2)
        {
            EquateEnumAndValue(enumValue2, Value!);
            return;
        }

        Success = false;
    }

    private void EquateEnumAndValue<TOtherValue>(DatEnumValue enumValue, TOtherValue value) where TOtherValue : IEquatable<TOtherValue>
    {
        if (typeof(TOtherValue) == typeof(string))
        {
            string? str = MathMatrix.As<TOtherValue?, string?>(value);
            if (!enumValue.Owner.TryParse(str.AsSpan(), out DatEnumValue? otherEnumValue, caseInsensitive: false))
            {
                Success = false;
                IsEqual = false;
            }
            else
            {
                IsEqual = otherEnumValue.Equals(enumValue);
                Success = true;
            }

            return;
        }
        
        if (!enumValue.NumericValue.HasValue)
        {
            Success = false;
            IsEqual = false;
            return;
        }

        ReduceRight<long> reduceRight;
        reduceRight.Value = enumValue.NumericValue.Value;
        reduceRight.IsEqual = false;
        reduceRight.Success = false;

        Success = MathMatrix.TryReduce(value, ref reduceRight) & reduceRight.Success;
        IsEqual = reduceRight.IsEqual;
    }

    public void Accept<T>(T? value) where T : IEquatable<T>
    {
        Accept(value == null ? Optional<T>.Null : new Optional<T>(value));
    }
}

file struct ReduceLeft<TRight> : IGenericVisitor
    where TRight : IEquatable<TRight>
{
    public bool IsEqual;
    public bool Success;
    public TRight RightValue;
    public void Accept<T>(T? value) where T : IEquatable<T>
    {
        ReduceRight<T> reduceRight;
        reduceRight.IsEqual = false;
        reduceRight.Value = value;
        reduceRight.Success = false;

        Success = MathMatrix.TryReduce(RightValue, ref reduceRight) & reduceRight.Success;
        IsEqual = reduceRight.IsEqual;
    }
}

file struct ReduceRight<TLeft> : IGenericVisitor
    where TLeft : IEquatable<TLeft>
{
    public bool IsEqual;
    public bool Success;
    public TLeft? Value;
    public void Accept<T>(T? value) where T : IEquatable<T>
    {
        BooleanVisitor v;
        v.Value = false;
        v.Success = false;
        Success = MathMatrix.Equals(Value, value, ref v) & v.Success;
        IsEqual = v.Value;
    }
}

file struct BooleanVisitor : IGenericVisitor
{
    public bool Value;
    public bool Success;

    /// <inheritdoc />
    public void Accept<T>(T? value) where T : IEquatable<T>
    {
        if (typeof(T) != typeof(bool))
            return;

        Success = true;
        Value = MathMatrix.As<T, bool>(value!);
    }
}