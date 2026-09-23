using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Parsing;

internal sealed class VersionTypeConverter : ITypeConverter<UnturnedVersion>
{
    public IType<UnturnedVersion> DefaultType => VersionType.Instance;

    public bool TryParse(ReadOnlySpan<char> text, ref TypeConverterParseArgs<UnturnedVersion> args, out UnturnedVersion parsedValue)
    {
        return UnturnedVersion.TryParse(args.StringOrSpan(text), out parsedValue);
    }

    public string Format(UnturnedVersion value, ref TypeConverterFormatArgs args)
    {
        return value.ToString(true);
    }

    public bool TryFormat(Span<char> output, UnturnedVersion value, out int size, ref TypeConverterFormatArgs args)
    {
        if (value.TryFormat(output, out size, true))
        {
            return true;
        }

        size = UnturnedVersion.MaximumStringLength;
        return false;
    }

    public override bool Equals(object? obj) => obj is VersionTypeConverter;
    public override int GetHashCode() => 417383781;

    public bool TryConvertTo<TTo>(Optional<UnturnedVersion> obj, out Optional<TTo> result) where TTo : IEquatable<TTo>
    {
        if (!obj.HasValue)
        {
            result = Optional<TTo>.Null;
            return true;
        }

        if (typeof(TTo) == typeof(UnturnedVersion))
        {
            result = Unsafe.As<Optional<UnturnedVersion>, Optional<TTo>>(ref obj);
            return true;
        }

        if (typeof(TTo) == typeof(string))
        {
            result = new Optional<TTo>(MathMatrix.As<string, TTo>(obj.Value.ToString(true)));
            return true;
        }

        if (typeof(TTo) == typeof(Vector4))
        {
            Vector4 v4 = new Vector4(obj.Value.Edition, obj.Value.Major, obj.Value.Minor, obj.Value.Patch);
            result = new Optional<TTo>(Unsafe.As<Vector4, TTo>(ref v4));
        }

        if (typeof(TTo) == typeof(Vector3))
        {
            Vector3 v3 = new Vector3(obj.Value.Edition, obj.Value.Major, obj.Value.Minor);
            result = new Optional<TTo>(Unsafe.As<Vector3, TTo>(ref v3));
        }

        if (typeof(TTo) == typeof(Vector2))
        {
            Vector2 v2 = new Vector2(obj.Value.Edition, obj.Value.Major);
            result = new Optional<TTo>(Unsafe.As<Vector2, TTo>(ref v2));
        }

        result = Optional<TTo>.Null;
        return false;
    }

    public void WriteJson(Utf8JsonWriter writer, UnturnedVersion value, ref TypeConverterFormatArgs args, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public bool TryReadJson(in JsonElement json, out Optional<UnturnedVersion> value, ref TypeConverterParseArgs<UnturnedVersion> args)
    {
        switch (json.ValueKind)
        {
            case JsonValueKind.Null:
                value = Optional<UnturnedVersion>.Null;
                return true;

            case JsonValueKind.String:
                if (!UnturnedVersion.TryParse(json.GetString()!, out UnturnedVersion v))
                {
                    value = Optional<UnturnedVersion>.Null;
                    return false;
                }

                value = v;
                return true;

            case JsonValueKind.Array:
                byte b = 0, c = 0, d = 0;
                switch (json.GetArrayLength())
                {
                    case 4:
                        d = json[3].GetByte();
                        goto case 3;
                    case 3:
                        c = json[2].GetByte();
                        goto case 2;
                    case 2:
                        b = json[1].GetByte();
                        goto case 1;
                    case 1:
                        byte a = json[0].GetByte();
                        value = new UnturnedVersion(a, b, c, d);
                        return true;
                }

                value = Optional<UnturnedVersion>.Null;
                return false;

            default:
                value = Optional<UnturnedVersion>.Null;
                return false;
        }
    }
}