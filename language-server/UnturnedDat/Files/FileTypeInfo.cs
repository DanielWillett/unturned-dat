using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Files;

/// <summary>
/// Guesses information about an asset file from its parent folder and relative files.
/// </summary>
public readonly struct FileTypeInfo : IEquatable<FileTypeInfo>
{
    /// <summary>
    /// Whether or not this file is an asset file.
    /// </summary>
    public bool IsAsset { get; }

    /// <summary>
    /// Whether or not this file is an asset's localization file.
    /// </summary>
    [MemberNotNullWhen(true, nameof(AssetPath))]
    public bool IsLocalization { get; }

    /// <summary>
    /// Corresponding asset path if this is a localization file.
    /// </summary>
    public string? AssetPath { get; }

    public FileTypeInfo(ReadOnlySpan<char> fullName, AssetInformation information)
    {
        ReadOnlySpan<char> fileName = OSPathHelper.GetFileName(fullName);

        if (fileName.Equals("Asset.dat", OSPathHelper.PathComparison))
        {
            IsAsset = true;
            return;
        }

        int extStartIndex = fileName.LastIndexOf('.');
        ReadOnlySpan<char> fnWithoutExt = extStartIndex >= 0 ? fileName[..extStartIndex] : fileName;
        if (fnWithoutExt.IsEmpty)
        {
            // file name is '**/.dat' or '**/.asset'
            return;
        }

        ReadOnlySpan<char> fullDirectoryName = OSPathHelper.GetDirectoryName(fullName);
        ReadOnlySpan<char> directoryName = OSPathHelper.GetFileName(fullDirectoryName);
        ReadOnlySpan<char> ext = fileName[extStartIndex..];

        // **/*.asset
        if (ext.Equals(".asset", StringComparison.OrdinalIgnoreCase))
        {
            IsAsset = true;
            return;
        }

        if (!ext.Equals(".dat", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Folder/Folder.dat
        if (!directoryName.IsEmpty && directoryName.Equals(fnWithoutExt, OSPathHelper.PathComparison))
        {
            IsAsset = true;
            return;
        }

        if (!char.IsUpper(fnWithoutExt[0]))
        {
            // all languages start with a capital letter
            return;
        }

        string languageName = SteamLanguageUtility.GetInternedLanguageName(fnWithoutExt);
        if (information.KnownLanguages == null || !information.KnownLanguages.Contains(languageName))
            return;

        IsLocalization = true;

        // Folder/English.dat (with Folder.dat)
        string datAsset = OSPathHelper.CombineAndConcat(fullDirectoryName, directoryName, ".dat");
        if (File.Exists(datAsset))
        {
            AssetPath = datAsset;
            return;
        }

        // Folder/English.dat (with Folder.asset)
        string assetAsset = OSPathHelper.CombineAndConcat(fullDirectoryName, directoryName, ".asset");
        if (File.Exists(assetAsset))
        {
            AssetPath = assetAsset;
            return;
        }

        // Folder/English.dat (with Asset.dat)
        string assetDatPath = OSPathHelper.CombineAndConcat(fullDirectoryName, "Asset.dat", ReadOnlySpan<char>.Empty);
        if (File.Exists(assetDatPath))
        {
            AssetPath = assetDatPath;
        }

        // Folder/English.dat (with *.asset, only if there's a single one)
        bool hasOne = false;
        foreach (string file in Directory.EnumerateFiles(fullDirectoryName.ToString(), "*.asset", SearchOption.TopDirectoryOnly))
        {
            if (hasOne)
            {
                AssetPath = null;
                return;
            }

            AssetPath = file;
            hasOne = true;
        }
    }

    /// <inheritdoc />
    public bool Equals(FileTypeInfo other) => Equals(in other);

    /// <inheritdoc cref="IEquatable{T}.Equals" />
    public bool Equals(in FileTypeInfo other)
    {
        return IsAsset == other.IsAsset
               && IsLocalization == other.IsLocalization
               && (!IsLocalization || string.Equals(AssetPath, other.AssetPath, OSPathHelper.PathComparison));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FileTypeInfo other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            IsAsset,
            IsLocalization,
            !IsLocalization || AssetPath == null ? 0 : OSPathHelper.PathComparer.GetHashCode(AssetPath)
        );
    }
}
