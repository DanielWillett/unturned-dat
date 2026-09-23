using System;
using System.Collections.Generic;
using UnturnedDat.Data.Project;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Files;

internal class RootLocalizationNode : RootDictionaryNode, ILocalizationSourceFile
{
    /// <inheritdoc />
    public string LanguageName { get; }

    /// <inheritdoc />
    public IAssetSourceFile Asset { get; }

    public static RootLocalizationNode Create(
        IWorkspaceFile file,
        IAssetSourceFile asset,
        IAssetSpecDatabase database,
        int count,
        ISourceNode[] nodes,
        in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
    {
        return new RootLocalizationNode(file, asset, database, count, nodes, in properties, additionalProperties);
    }

    /// <inheritdoc />
    private protected RootLocalizationNode(IWorkspaceFile file, IAssetSourceFile asset, IAssetSpecDatabase database, int count, ISourceNode[] nodes, in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
        : base(file, database, count, nodes, in properties, additionalProperties)
    {
        Asset = asset;

        string fullName = file.File;

        ReadOnlySpan<char> fn = OSPathHelper.GetFileNameWithoutExtension(fullName.AsSpan());
        LanguageName = SteamLanguageUtility.GetInternedLanguageName(fn);
    }

    protected override QualifiedType CalculateActualType()
    {
        return Asset.ActualType;
    }
}