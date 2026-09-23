using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace UnturnedDat.Data;

/// <summary>
/// A 4-component build version of Unturned.
/// </summary>
/// <remarks>
/// Before <c>3.19.14.0</c>, versions followed the format <c>3.major.minor.patch</c>.
/// Starting with <c>3.19.14.0</c>, versions now follow <c>3.year.update.patch</c>.
/// This means that versions within the range <c>[3.19.14.0,3.30.6.0]</c> are ambiguous.
/// </remarks>
[DebuggerDisplay("{ToString(true),nq}")]
public readonly struct UnturnedVersion : IComparable<UnturnedVersion>, IEquatable<UnturnedVersion>
#if NET6_0_OR_GREATER
    , ISpanFormattable
#else
    , IFormattable
#endif
#if NET7_0_OR_GREATER
    , ISpanParsable<UnturnedVersion>
#endif
{
    private const uint LastLegacyVersion = 52299264; // 3.30.6.0
    private const uint FirstYearVersion = 51580416;  // 3.19.14.0

    // maps legacy major version to the year it's 3.i.0.0 patch was released
    private static readonly int[] LegacyVersionYears =
    [
        2014, // 0
        2014, // 1
        2014, // 2
        2014, // 3
        2014, // 4
        2015, // 5
        2015, // 6
        2015, // 7
        2015, // 8
        2015, // 9
        2015, // 10
        2015, // 11
        2015, // 12
        2015, // 13
        2016, // 14
        2016, // 15
        2016, // 16
        2016, // 17
        2017, // 18
        2017, // 19
        2017, // 20
        2017, // 21
        2017, // 22
        2017, // 23
        2018, // 24
        2018, // 25
        2018, // 26
        2018, // 27
        2019, // 28
        2019, // 29
        2019  // 30
    ];


    // max length 'o:255.255.255.255' is 17 chars

    /// <summary>
    /// Maximum number of characters a version can take up.
    /// </summary>
    public const int MaximumStringLength = 17;

    /// <summary>
    /// A version equal to <c>0.0.0.0</c>.
    /// </summary>
    public static readonly UnturnedVersion Zero = new UnturnedVersion(0, false);

    /// <summary>
    /// Version number packed into a <see cref="UInt32"/>.
    /// </summary>
    public uint PackedVersion { get; }

    /// <summary>
    /// Whether or not this version uses the year version numbers that were started with update <c>3.19.14.0</c>.
    /// </summary>
    public bool IsYearFormat { get; }

    /// <summary>
    /// Edition of the game, usually <c>3</c>.
    /// </summary>
    public byte Edition => unchecked ( (byte)(PackedVersion >> 24) );

    /// <summary>
    /// Major release, or the year after <c>3.19.14.0</c>.
    /// </summary>
    public byte Major => unchecked ( (byte)(PackedVersion >> 16) );

    /// <summary>
    /// Minor release.
    /// </summary>
    public byte Minor => unchecked ( (byte)(PackedVersion >> 8) );

    /// <summary>
    /// Patch release.
    /// </summary>
    public byte Patch => unchecked ( (byte)PackedVersion );

    private bool IsLegacyVersionPrefixNeeded => !IsYearFormat && PackedVersion is >= FirstYearVersion and <= LastLegacyVersion;

    /// <summary>
    /// Create a new <see cref="UnturnedVersion"/> value with the 4 version components.
    /// </summary>
    public UnturnedVersion(byte edition, byte major, byte minor, byte patch)
    {
        uint packed = ((uint)edition << 24) | ((uint)major << 16) | ((uint)minor << 8) | patch;
        PackedVersion = packed;
        IsYearFormat = edition == 3 && packed >= FirstYearVersion;
    }

    /// <summary>
    /// Create a new <see cref="UnturnedVersion"/> value with the 4 version components.
    /// </summary>
    public UnturnedVersion(byte edition, byte major, byte minor, byte patch, bool isYearFormat)
    {
        PackedVersion = ((uint)edition << 24) | ((uint)major << 16) | ((uint)minor << 8) | patch;
        IsYearFormat = isYearFormat;
    }

    /// <summary>
    /// Create a new <see cref="UnturnedVersion"/> value with a packed <see cref="uint"/>.
    /// </summary>
    public UnturnedVersion(uint packedVersion, bool isYearFormat)
    {
        PackedVersion = packedVersion;
        IsYearFormat = isYearFormat;
    }

    /// <returns>The parsed version.</returns>
    /// <exception cref="FormatException">Invalid format in <paramref name="text"/>.</exception>
    /// <inheritdoc cref="TryParse"/>
    public static UnturnedVersion Parse(ReadOnlySpan<char> text)
    {
        if (!TryParse(text, out UnturnedVersion v))
            throw new FormatException(Properties.Resources.FormatException_UnturnedVersion_FailedToParse);

        return v;
    }

    /// <summary>
    /// Converts a 4-component version to a <see cref="uint"/> value. Each component must fall within <c>[0,256)</c>.
    /// <para>The <paramref name="text"/> can contain an optional <c>"o:"</c> prefix to indicate the version number is referencing a legacy version before <c>3.19.14.0</c>, which has overlap with the modern version numbers.</para>
    /// </summary>
    /// <remarks>Whitespace between components is ignored.</remarks>
    /// <param name="text">The text to parse.</param>
    /// <param name="version">The parsed <see cref="UnturnedVersion"/> value, or <see cref="Zero"/> if it couldn't be parsed.</param>
    /// <returns>Whether or not the version could be successfully parsed.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out UnturnedVersion version)
    {
        text = text.Trim();
        // o:1.1.1.1
        bool hasPrefix = false;
        if (text.Length >= 9)
        {
            hasPrefix = text[0] is 'o' or 'O' && text[1] == ':';
            if (hasPrefix)
                text = text[2..].TrimStart();
        }

        // 1.1.1.1
        if (text.Length < 7)
        {
            version = Zero;
            return false;
        }

        if (!TryPack(text, out uint packed))
        {
            version = Zero;
            return false;
        }

        bool isYearVersion = !hasPrefix && unchecked( (byte)(packed >> 24) ) == 3 && packed >= FirstYearVersion;

        version = new UnturnedVersion(packed, isYearVersion);
        return true;
    }

    /// <summary>
    /// Converts a 4-component version to a <see cref="uint"/> value. Each component must fall within <c>[0,256)</c>.
    /// </summary>
    /// <remarks>Whitespace between components is ignored.</remarks>
    /// <param name="text">The text to parse.</param>
    /// <param name="packed">The version packed into a <see cref="uint"/>.</param>
    /// <returns>Whether or not the version could be successfully parsed.</returns>
    public static bool TryPack(ReadOnlySpan<char> text, out uint packed)
    {
        text = text.Trim();

        packed = 0u;

        int dotIndex = text.IndexOf('.');
        if (dotIndex <= 0)
            return false;

        if (!TryParseByte(text.Slice(0, dotIndex), out byte edition))
            return false;

        text = text.Slice(dotIndex + 1);
        dotIndex = text.IndexOf('.');
        if (dotIndex < 0)
            return false;

        if (!TryParseByte(text.Slice(0, dotIndex), out byte major))
            return false;

        text = text.Slice(dotIndex + 1);
        dotIndex = text.IndexOf('.');
        if (dotIndex < 0)
            return false;

        if (!TryParseByte(text.Slice(0, dotIndex), out byte minor))
            return false;

        if (!TryParseByte(text.Slice(dotIndex + 1), out byte patch))
            return false;

        packed = ((uint)edition << 24) | ((uint)major << 16) | ((uint)minor << 8) | patch;
        return true;
    }

    /// <summary>
    /// Gets the approximate year this version was released.
    /// </summary>
    /// <param name="year">The 4-digit year, or <c>0</c> if the year couldn't be determined.</param>
    /// <returns>Whether or not the year could be determined.</returns>
    /// <remarks>For legacy releases this is determined by the major version, so it may be delayed for patches that were released at the beginning of the year.</remarks>
    public bool TryGetYear(out int year)
    {
        if (Edition != 3)
        {
            year = 0;
            return false;
        }

        if (IsYearFormat)
        {
            year = 2000 + Major;
            return true;
        }

        int major = Major;
        if (major >= LegacyVersionYears.Length)
        {
            year = 0;
            return false;
        }

        year = LegacyVersionYears[major];
        return true;
    }

    /// <summary>
    /// Compares two versions, taking into ambiguous version numbers.
    /// </summary>
    public int CompareTo(UnturnedVersion other)
    {
        if (IsYearFormat == other.IsYearFormat)
            return PackedVersion.CompareTo(other.PackedVersion);

        return IsYearFormat ? 1 : -1;
    }

    /// <summary>
    /// Checks if two versions are the exact same, taking into ambiguous version numbers.
    /// </summary>
    public bool Equals(UnturnedVersion other)
    {
        return PackedVersion == other.PackedVersion && IsYearFormat == other.IsYearFormat;
    }

    /// <inheritdoc cref="Equals(UnturnedVersion)"/>
    public override bool Equals(object? obj)
    {
        return obj is UnturnedVersion v && Equals(v);
    }

    public static bool operator ==(UnturnedVersion left, UnturnedVersion right) => left.PackedVersion == right.PackedVersion && left.IsYearFormat == right.IsYearFormat;
    public static bool operator !=(UnturnedVersion left, UnturnedVersion right) => left.PackedVersion != right.PackedVersion || left.IsYearFormat != right.IsYearFormat;
    public static bool operator <(UnturnedVersion left, UnturnedVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(UnturnedVersion left, UnturnedVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(UnturnedVersion left, UnturnedVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(UnturnedVersion left, UnturnedVersion right) => left.CompareTo(right) >= 0;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return unchecked( (int)(IsYearFormat ? ~PackedVersion : PackedVersion) );
    }

    /// <summary>
    /// Converts this version to a 4-component version string, without any prefixes.
    /// </summary>
    public override string ToString() => ToString(false);

    /// <summary>
    /// Converts this version to a 4-component version string.
    /// <para>
    /// If <paramref name="withPrefixIfNeeded"/> is <see langword="true"/>, a <c>"o:"</c> prefix will be prepended if needed to address ambiguous versions.
    /// </para>
    /// </summary>
    public string ToString(bool withPrefixIfNeeded)
    {
        Span<char> buffer = stackalloc char[MaximumStringLength];
        TryFormat(buffer, out int charsWritten, withPrefixIfNeeded);

        buffer = buffer.Slice(0, charsWritten);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return new string(buffer);
#else
        return buffer.ToString();
#endif
    }

    /// <summary>
    /// Converts this version to a 4-component version string.
    /// <para>
    /// If <paramref name="withPrefixIfNeeded"/> is <see langword="true"/>, a <c>"o:"</c> prefix will be prepended if needed to address ambiguous versions.
    /// </para>
    /// </summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, bool withPrefixIfNeeded)
    {
        withPrefixIfNeeded &= IsLegacyVersionPrefixNeeded;

        int index;
        if (withPrefixIfNeeded)
        {
            if (destination.Length < 9)
            {
                charsWritten = 0;
                return false;
            }
            destination[0] = 'o';
            destination[1] = ':';
            index = 2;
        }
        else
        {
            if (destination.Length < 7)
            {
                charsWritten = 0;
                return false;
            }
            index = 0;
        }

        if (!TryFormatByte(Edition, destination, ref index) || destination.Length <= index)
        {
            charsWritten = index;
            return false;
        }

        destination[index] = '.';
        ++index;
        if (!TryFormatByte(Major, destination, ref index) || destination.Length <= index)
        {
            charsWritten = index;
            return false;
        }
        destination[index] = '.';
        ++index;
        if (!TryFormatByte(Minor, destination, ref index) || destination.Length <= index)
        {
            charsWritten = index;
            return false;
        }
        destination[index] = '.';
        ++index;
        if (!TryFormatByte(Patch, destination, ref index))
        {
            charsWritten = index;
            return false;
        }

        charsWritten = index;
        return true;
    }

    private static bool TryFormatByte(byte b, Span<char> buffer, ref int index)
    {
        int digitCount = b < 100 ? b < 10 ? 1 : 2 : 3;
        if (buffer.Length < digitCount)
            return false;

        switch (digitCount)
        {
            case 3:
                buffer[index] = (char)('0' + b / 100 % 10);
                ++index;
                goto case 2;
            case 2:
                buffer[index] = (char)('0' + b / 10 % 10);
                ++index;
                goto case 1;
            case 1:
                buffer[index] = (char)('0' + b % 10);
                ++index;
                break;
        }

        return true;
    }

    private static bool TryParseByte(ReadOnlySpan<char> str, out byte b)
    {
        str = str.Trim();
        char c1, c2;
        switch (str.Length)
        {
            case 1:
                c1 = str[0];
                if (c1 is not (>= '0' and <= '9'))
                {
                    b = 0;
                    return false;
                }
                b = (byte)(c1 - '0');
                return true;

            case 2:
                c2 = str[0];
                c1 = str[1];
                if (c2 is not (>= '0' and <= '9') || c1 is not (>= '0' and <= '9'))
                {
                    b = 0;
                    return false;
                }
                b = (byte)((c2 - '0') * 10 + (c1 - '0'));
                return true;

            case 3:
                char c3 = str[0];
                c2 = str[1];
                c1 = str[2];
                if (c3 is not (>= '0' and <= '9') || c2 is not (>= '0' and <= '9') || c1 is not (>= '0' and <= '9'))
                {
                    b = 0;
                    return false;
                }
                int v = (c3 - '0') * 100 + (c2 - '0') * 10 + (c1 - '0');
                if (v > byte.MaxValue)
                {
                    b = 0;
                    return false;
                }
                b = (byte)v;
                return true;

            default:
                b = 0;
                return false;
        }
    }

#if NET6_0_OR_GREATER
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (format.Equals("P", StringComparison.OrdinalIgnoreCase))
            return TryFormat(destination, out charsWritten, true);
        if (format.IsEmpty)
            return TryFormat(destination, out charsWritten, false);
        throw new ArgumentException(Properties.Resources.ArgumentException_UnturnedVersion_UnknownFormatArgument, nameof(format));
    }
#endif
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.Equals(format, "P", StringComparison.OrdinalIgnoreCase))
            return ToString(true);
        if (string.IsNullOrEmpty(format))
            return ToString(false);
        throw new ArgumentException(Properties.Resources.ArgumentException_UnturnedVersion_UnknownFormatArgument, nameof(format));
    }
#if NET7_0_OR_GREATER
    static UnturnedVersion IParsable<UnturnedVersion>.Parse(string s, IFormatProvider? provider) => Parse(s);
    static UnturnedVersion ISpanParsable<UnturnedVersion>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);
    static bool IParsable<UnturnedVersion>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UnturnedVersion result) => TryParse(s, out result);
    static bool ISpanParsable<UnturnedVersion>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UnturnedVersion result) => TryParse(s, out result);
#endif
}