using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Files;

/// <summary>
/// Loops through all localization files for an asset file.
/// </summary>
public struct LocalizationFileEnumerator : IEnumerator<string>
{
    private readonly string _assetFilePath;
    private readonly string[] _files;

    private int _fileIndex;
    private string? _currentFile;

#nullable disable
    public string Current => _currentFile;
#nullable restore

    public int FileCount { get; }

    public LocalizationFileEnumerator(IAssetSpecDatabase database, string assetFilePath, QualifiedType actualType)
    {
        _assetFilePath = assetFilePath;

        if (database.FileTypes.TryGetValue(actualType, out DatFileType? specType) && !specType.HasLocalizationProperties)
        {
            _files = Array.Empty<string>();
            return;
        }

        string? dirName = Path.GetDirectoryName(assetFilePath);
        if (!string.IsNullOrEmpty(dirName))
        {
            try
            {
                string[] files = Directory.GetFiles(dirName, "*.dat", SearchOption.TopDirectoryOnly);
                FileCount = files.Length - (assetFilePath.EndsWith(".dat", OSPathHelper.PathComparison) ? 1 : 0);
                _files = files;
                return;
            }
            catch (SystemException) { }
        }

        _files = Array.Empty<string>();
    }

    public bool MoveNext()
    {
        while (true)
        {
            if (_files.Length <= _fileIndex)
                return false;

            string localFile = _files[_fileIndex];
            ++_fileIndex;

            if (localFile.Equals(_assetFilePath, OSPathHelper.PathComparison))
                continue;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            ReadOnlySpan<char> langName = Path.GetFileNameWithoutExtension(localFile.AsSpan());
            if (langName.IsWhiteSpace() || !char.IsUpper(langName[0]))
                continue;
#else
            string langName = Path.GetFileNameWithoutExtension(localFile);
            if (string.IsNullOrWhiteSpace(langName) || !char.IsUpper(langName[0]))
                continue;
#endif

            _currentFile = localFile;
            return true;
        }
    }

    public void Dispose() { }
    
    object? IEnumerator.Current => Current;
    void IEnumerator.Reset() => throw new NotSupportedException();
}