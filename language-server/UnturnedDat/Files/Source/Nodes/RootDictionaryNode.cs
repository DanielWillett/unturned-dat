using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using UnturnedDat.Data.Project;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Files;

internal class RootDictionaryNode : DictionaryNode, ISourceFile
{
    private bool _hasActualType;

    internal IAssetSpecDatabase? Database { get; }
    public OneOrMore<KeyValuePair<string, object?>> AdditionalProperties { get; }

    public IWorkspaceFile WorkspaceFile { get; internal set; }
    public TfmLock TreeSync { get; }
    public int FileVersion { get; private set; }
    public ImmutableArray<IPropertySourceNode> Properties { get; internal set; }

    public QualifiedType ActualType
    {
        get
        {
            if (_hasActualType)
                return field;

            field = CalculateActualType();
            _hasActualType = true;
            return field;
        }
    }

    protected void ResetActualTypeCache()
    {
        _hasActualType = false;
    }

    protected virtual QualifiedType CalculateActualType()
    {
        if (Database is not { IsInitialized: true } || this is RootLocalizationNode)
            return QualifiedType.None;

        if (this.TryGetAdditionalProperty(Comment.TypeAdditionalProperty, out string? str) && str != null)
        {
            return new QualifiedType(str, true);
        }
        if (Database.Information.KnownFileNames.TryGetValue(Path.GetFileName(File.WorkspaceFile.File), out QualifiedType t) && !t.IsNull)
        {
            return t.CaseInsensitive;
        }
        if (OSPathHelper.IsExtension(File.WorkspaceFile.File, ".udatproj"))
        {
            return ProjectFileType.TypeId;
        }

        return QualifiedType.None;
    }

    public static RootDictionaryNode Create(
        IWorkspaceFile file,
        IAssetSpecDatabase? database,
        int count,
        ISourceNode[] nodes,
        in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties)
    {
        return new RootDictionaryNode(file, database, count, nodes, in properties, additionalProperties);
    }

    private protected RootDictionaryNode(
        IWorkspaceFile file,
        IAssetSpecDatabase? database,
        int count,
        ISourceNode[] nodes,
        in AnySourceNodeProperties properties,
        OneOrMore<KeyValuePair<string, object?>> additionalProperties
    )
       : base(count, nodes, in properties)
    {
        FileVersion = 1;
        WorkspaceFile = file;
        Database = database;
        AdditionalProperties = additionalProperties;

        TreeSync = new TfmLock();

        ImmutableArray<IPropertySourceNode>.Builder builder = ImmutableArray.CreateBuilder<IPropertySourceNode>(count);
        for (int i = 0; i < nodes.Length; ++i)
        {
            if (nodes[i] is not IPropertySourceNode pn)
                continue;

            builder.Add(pn);
        }

        Properties = builder.MoveToImmutableOrCopy();
        SetParentInfo(this, this);
    }

    internal sealed override void SetParentInfo(ISourceFile? file, IParentSourceNode parent)
    {
        base.SetParentInfo(file, parent);
    }

    int ISourceFile.FileVersion { get => FileVersion; set => FileVersion = value; }
}