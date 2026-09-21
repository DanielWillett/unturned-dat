using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using UnturnedDat.Data.Project;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Files;

internal class RootAssetNodeSkippedLocalization : RootDictionaryNode, IAssetSourceFile
{
    private bool _hasMetadata;
    private Guid? _guid;
    private ushort? _id;
    private AssetCategoryValue _category;
    private QualifiedOrAliasedType _assetType;
    private bool _isErrored;
    private QualifiedType _actualType;

    public virtual ImmutableArray<ILocalizationSourceFile> Localization => throw new NotSupportedException();

    public Guid? Guid
    {
        get
        {
            if (_hasMetadata)
                return _guid;

            LoadMetadata();
            return _guid;
        }
    }

    public ushort? Id
    {
        get
        {
            if (_hasMetadata)
                return _id;

            LoadMetadata();
            return _id;
        }
    }

    public AssetCategoryValue Category
    {
        get
        {
            if (_hasMetadata)
                return _category;

            LoadMetadata();
            return _category;
        }
    }

    public QualifiedOrAliasedType AssetType
    {
        get
        {
            if (_hasMetadata)
                return _assetType;

            LoadMetadata();
            return _assetType;
        }
    }

    public string AssetName { get; protected set; }

    public bool IsErrored
    {
        get
        {
            if (_hasMetadata)
                return _isErrored;

            LoadMetadata();
            return _isErrored;
        }
    }

    public new static RootAssetNodeSkippedLocalization Create(
        IWorkspaceFile file,
        IAssetSpecDatabase database,
        int count,
        ISourceNode[] nodes,
        in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
    {
        return new RootAssetNodeSkippedLocalization(file, database, count, nodes, in properties, additionalProperties);
    }

    public RootAssetNode CreateWithLocalization(ImmutableArray<ILocalizationSourceFile> localization)
    {
        return RootAssetNode.Create(this, localization);
    }

    protected RootAssetNodeSkippedLocalization(IWorkspaceFile file, IAssetSpecDatabase database, int count, ISourceNode[] nodes, in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
        : base(file, database, count, nodes, in properties, additionalProperties)
    {
        string fileName = file.File;

        if (fileName.EndsWith("Asset.dat", StringComparison.OrdinalIgnoreCase))
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            ReadOnlySpan<char> dirNameSpan = Path.GetDirectoryName(fileName.AsSpan());
            AssetName = Path.GetFileName(dirNameSpan).ToString();
#else
            string? dirName = Path.GetDirectoryName(fileName);
            AssetName = string.IsNullOrEmpty(dirName) ? string.Empty : Path.GetFileName(dirName);
#endif
        }
        else
        {
            AssetName = Path.GetFileNameWithoutExtension(fileName);
        }
    }

    protected override QualifiedType CalculateActualType()
    {
        if (!_hasMetadata)
            LoadMetadata();

        return _actualType;
    }

    internal void LoadMetadata()
    {
        lock (TreeSync)
        {
            if (_hasMetadata)
                return;

            bool isErrored = false;
            Guid? guid = null;
            ushort? id = null;
            AssetCategoryValue? category = null;
            QualifiedOrAliasedType? type = null;
            QualifiedType? actualType = null;

            IValueSourceNode? guidProp, typeProp;
            Guid parsedGuid;
            IDictionarySourceNode? metadata = this.GetMetadataDictionary();
            if (metadata != null)
            {
                if (metadata.TryGetPropertyValue("GUID", out guidProp))
                {
                    if (!KnownTypeValueHelper.TryParseGuid(guidProp.Value, out parsedGuid) || parsedGuid == System.Guid.Empty)
                    {
                        isErrored = true;
                    }
                    else
                    {
                        guid = parsedGuid;
                    }
                }
                else
                {
                    isErrored = true;
                }

                if (metadata.TryGetPropertyValue("Type", out typeProp))
                {
                    if (!KnownTypeValueHelper.TryParseType(typeProp.Value, out QualifiedType parsedType))
                    {
                        isErrored = true;
                    }
                    else
                    {
                        type = QualifiedOrAliasedType.FromType(parsedType);
                        actualType = parsedType;
                    }
                }
            }
            else if (!this.TryGetPropertyValue("GUID", out guidProp)
                     || !KnownTypeValueHelper.TryParseGuid(guidProp.Value, out parsedGuid)
                     || parsedGuid == System.Guid.Empty)
            {
                isErrored = true;
            }
            else
            {
                guid = parsedGuid;
            }

            IDictionarySourceNode assetData = this.AssetData;
            if (this.TryGetAdditionalProperty(Comment.TypeAdditionalProperty, out string? str) && str != null)
            {
                QualifiedType parsedType = new QualifiedType(str, true);
                actualType = parsedType;
                type = QualifiedOrAliasedType.FromType(parsedType);
            }

            if (!type.HasValue)
            {
                if (!assetData.TryGetPropertyValue("Type", out typeProp) || string.IsNullOrWhiteSpace(typeProp.Value))
                {
                    isErrored = true;
                }
                else if (Database!.Information.AssetAliases.TryGetValue(typeProp.Value, out QualifiedType parsedActualType))
                {
                    actualType = parsedActualType;
                    type = QualifiedOrAliasedType.FromAlias(typeProp.Value);
                }
                else
                {
                    parsedActualType = new QualifiedType(typeProp.Value, isCaseInsensitive: true);
                    actualType = parsedActualType;
                    type = QualifiedOrAliasedType.FromType(parsedActualType);
                }
            }

            if (assetData.TryGetPropertyValue("ID", out IValueSourceNode? idProp))
            {
                if (KnownTypeValueHelper.TryParseUInt16(idProp.Value, out ushort parsedId))
                {
                    id = parsedId;
                }
            }

            if (actualType.HasValue)
            {
                category = AssetCategoryValue.None;
                if (actualType.Equals("SDG.Unturned.RedirectorAsset, Assembly-CSharp"))
                {
                    if (assetData.TryGetPropertyValue("AssetCategory", out IValueSourceNode? assetCategoryProp)
                        && AssetCategory.TryParse(assetCategoryProp.Value, out int index))
                    {
                        category = new AssetCategoryValue(index);
                    }
                }
                else if (Database!.Information.AssetCategories.TryGetValue(actualType.Value, out string? catStr))
                {
                    category = new AssetCategoryValue(catStr);
                }
                else
                {
                    InverseTypeHierarchy parentTypes = Database.Information.GetParentTypes(actualType.Value);
                    for (int i = parentTypes.ParentTypes.Length - 1; i >= 0; --i)
                    {
                        if (!Database.Information.AssetCategories.TryGetValue(parentTypes.ParentTypes[i], out catStr))
                            continue;

                        category = new AssetCategoryValue(catStr);
                        break;
                    }
                }
            }

            _guid = guid;
            _id = id;
            _category = category.GetValueOrDefault(AssetCategoryValue.None);
            _assetType = type.GetValueOrDefault();
            _actualType = actualType.GetValueOrDefault(QualifiedType.None);
            _isErrored = isErrored;
            _hasMetadata = true;
        }
    }
}

internal sealed class RootAssetNode : RootAssetNodeSkippedLocalization
{
    private ImmutableArray<ILocalizationSourceFile> _localization;

    public override ImmutableArray<ILocalizationSourceFile> Localization
    {
        get
        {
            if (_localization.IsDefault)
                DiscoverLocalization();
            return _localization;
        }
    }

    public new static RootAssetNode Create(
        IWorkspaceFile file,
        IAssetSpecDatabase database,
        int count,
        ISourceNode[] nodes,
        in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
    {
        return new RootAssetNode(file, database, count, nodes, in properties, additionalProperties);
    }

    public static RootAssetNode Create(RootAssetNodeSkippedLocalization withoutLocalization, ImmutableArray<ILocalizationSourceFile> localization)
    {
        // ReSharper disable once CoVariantArrayConversion
        return new RootAssetNode(
            localization,
            withoutLocalization.WorkspaceFile,
            withoutLocalization.Database!,
            withoutLocalization.Count,
            withoutLocalization.Properties.UnsafeThaw(),
            withoutLocalization.GetAnyNodeProperties(),
            withoutLocalization.AdditionalProperties);
    }

    private RootAssetNode(ImmutableArray<ILocalizationSourceFile> localization, IWorkspaceFile file, IAssetSpecDatabase database, int count, ISourceNode[] nodes, in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
        : base(file, database, count, nodes, in properties, additionalProperties)
    {
        _localization = localization;
    }

    /// <inheritdoc />
    private RootAssetNode(IWorkspaceFile file, IAssetSpecDatabase database, int count, ISourceNode[] nodes, in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
        : base(file, database, count, nodes, in properties, additionalProperties)
    { }

    private void DiscoverLocalization()
    {
        if (Database == null)
        {
            _localization = ImmutableArray<ILocalizationSourceFile>.Empty;
            return;
        }

        string fileName = File.WorkspaceFile.File;
        using LocalizationFileEnumerator enumerator = new LocalizationFileEnumerator(Database!, fileName, ActualType);

        int englishIndex = -1;
        ImmutableArray<ILocalizationSourceFile>.Builder builder = ImmutableArray.CreateBuilder<ILocalizationSourceFile>(enumerator.FileCount);

        while (enumerator.MoveNext())
        {
            string localFile = enumerator.Current!;
            string? text;
            try
            {
                text = System.IO.File.ReadAllText(localFile);
            }
            catch (SystemException)
            {
                text = null;
            }

            ReferencedWorkspaceFile workspaceFile = new ReferencedWorkspaceFile(localFile, Database!, this, text!, static (file, state, text) =>
            {
                if (text == null)
                    return null!;

                using SourceNodeTokenizer tokenizer = new SourceNodeTokenizer(
                    text,
                    SourceNodeTokenizerOptions.None
                );
                return tokenizer.ReadRootDictionary(SourceNodeTokenizer.RootInfo.Localization(file, file.Database, (IAssetSourceFile)state!));
            }, File.WorkspaceFile.Bundle);

            if (workspaceFile.SourceFile is not ILocalizationSourceFile local)
            {
                workspaceFile.Dispose();
                continue;
            }

            if (local.LanguageName.Equals("English", StringComparison.Ordinal))
            {
                englishIndex = builder.Count;
            }

            builder.Add(local);
        }

        if (englishIndex > 0)
            (builder[0], builder[englishIndex]) = (builder[englishIndex], builder[0]);

        _localization = builder.MoveToImmutableOrCopy();
    }
}