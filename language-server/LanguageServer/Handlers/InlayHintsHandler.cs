
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Runtime.CompilerServices;
using System.Text;
using UnturnedDat.Data;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Project;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;
using UnturnedDat.LanguageServer.Files;
using UnturnedDat.LanguageServer.Utility;

namespace UnturnedDat.LanguageServer.Handlers;

internal class InlayHintsHandler : IInlayHintsHandler
{
    private readonly IParsingServices _parsingServices;
    private readonly IFileRelationalModelProvider _modelProvider;
    private readonly OpenedFileTracker _fileTracker;
    private readonly StartupWaitUtility _startupWait;

    public InlayHintsHandler(
        IParsingServices parsingServices,
        IFileRelationalModelProvider modelProvider,
        OpenedFileTracker fileTracker,
        StartupWaitUtility startupWait)
    {
        _parsingServices = parsingServices;
        _modelProvider = modelProvider;
        _fileTracker = fileTracker;
        _startupWait = startupWait;
    }

    public async Task<InlayHintContainer?> Handle(InlayHintParams request, CancellationToken cancellationToken)
    {
        await _startupWait.WaitForStartupAsync();

        if (!_fileTracker.Files.TryGetValue(request.TextDocument.Uri, out OpenedFile? file))
        {
            return new InlayHintContainer();
        }

        List<InlayHint> hints = new List<InlayHint>(4);

        ISourceFile sourceFile = file.SourceFile;
        InlayHintVisitor visitor = new InlayHintVisitor(hints, _modelProvider, _parsingServices, request.Range?.ToFileRange());
        sourceFile.Visit(ref visitor);

        return new InlayHintContainer(hints);
    }


    InlayHintRegistrationOptions IRegistration<InlayHintRegistrationOptions, InlayHintClientCapabilities>.GetRegistrationOptions(
        InlayHintClientCapabilities capability, ClientCapabilities clientCapabilities)
    {
        return new InlayHintRegistrationOptions
        {
            DocumentSelector = UnturnedDatLanguageServer.AssetFileSelector,
            ResolveProvider = false,
            WorkDoneProgress = false
        };
    }
}

file class InlayHintVisitor : ResolvedPropertyNodeVisitor, ITypeVisitor, IEquatableArrayVisitor
{
    private readonly IParsingServices _parsingServices;
    private readonly List<InlayHint> _hints;

    private OneOrMore<DiscoveredDatFile> _files = OneOrMore<DiscoveredDatFile>.Null;
    private IPropertySourceNode? _property;
    private StringBuilder? _tooltipBuilder;
    private IAssetReferenceType? _type;

    private bool _isList;

    public InlayHintVisitor(
        List<InlayHint> hints,
        IFileRelationalModelProvider modelProvider,
        IParsingServices parsingServices,
        FileRange? range = null) : base(modelProvider, parsingServices, range)
    {
        _parsingServices = parsingServices;
        _hints = hints;
    }

    protected override void AcceptResolvedProperty(
        DatProperty property,
        IType propertyType,
        ref FileEvaluationContext ctx,
        IPropertySourceNode node)
    {
        IAssetReferenceType? assetRefType = propertyType as IAssetReferenceType;
        _isList = false;
        if (assetRefType == null && propertyType is IListType { ElementType: IAssetReferenceType assetRefElement })
        {
            if (node.ValueKind != SourceValueType.List)
                return;

            assetRefType = assetRefElement;
            _isList = true;
        }

        if (assetRefType == null)
            return;

        _type = assetRefType;

        InlayHintVisitor @this = this;
        @this._files = OneOrMore<DiscoveredDatFile>.Null;
        @this._property = node;
        propertyType.Visit(ref @this);

        if (_isList)
            return;

        if (@this != this)
        {
            _files = @this._files;
        }

        FilePosition endPos = node.ValueKind == SourceValueType.Value
                                  ? node.GetValueRange().End
                                  : node.Range.End;

        Position pos = new Position(endPos.Line - 1, endPos.Character);

        CreateHints(pos);
    }

    public void Accept<TValue>(IType<TValue> type) where TValue : IEquatable<TValue>
    {
        TypeParserArgs<TValue> args = new TypeParserArgs<TValue>
        {
            Type = type,
            ParentNode = _property!,
            ValueNode = _property!.Value,
            MissingValueBehavior = TypeParserMissingValueBehavior.ErrorIfValueOrPropertyNotProvided
        };

        FileEvaluationContext ctx = new FileEvaluationContext(
            _parsingServices,
            _property.File,
            _property.GetRootPosition()
         );

        if (!type.Parser.TryParse(ref args, ref ctx, out Optional<TValue> value)
            || !value.HasValue)
        {
            return;
        }

        if (_isList && value.Value is IEquatableArray<TValue> equatableArray)
        {
            InlayHintVisitor @this = this;
            equatableArray.Visit(ref @this);
            return;
        }

        ResolveFiles(value.Value);
    }

    public void Accept<T>(EquatableArray<T> array) where T : IEquatable<T>
    {
        if (_property?.Value is not IListSourceNode listNode)
            return;

        int ct = Math.Min(array.Array.Length, listNode.Count);
        for (int i = 0; i < ct; ++i)
        {
            if (!listNode.TryGetElement(i, out IAnyValueSourceNode? valueNode))
                continue;

            _files = OneOrMore<DiscoveredDatFile>.Null;
            ResolveFiles(array.Array[i]);

            FilePosition endPos = valueNode.Range.End;
            CreateHints(new Position(endPos.Line - 1, endPos.Character));
        }
    }

    private void ResolveFiles<TValue>(TValue value)
    {
        if (typeof(TValue) == typeof(Guid))
        {
            Guid guid = Unsafe.As<TValue, Guid>(ref Unsafe.AsRef(in value));
            _files = _parsingServices.Installation.FindFile(guid);
        }
        else if (typeof(TValue) == typeof(ushort))
        {
            ushort id = Unsafe.As<TValue, ushort>(ref Unsafe.AsRef(in value));
            int c = _parsingServices.Database.Information.GetAssetCategory(_type!.BaseTypes);
            if (c == 0)
                return;

            _files = _parsingServices.Installation.FindFile(id, new AssetCategoryValue(c));
        }
        else if (typeof(TValue) == typeof(GuidOrId))
        {
            GuidOrId guidOrId = Unsafe.As<TValue, GuidOrId>(ref Unsafe.AsRef(in value));
            _files = _parsingServices.Installation.FindFile(guidOrId);
        }
    }

    private void CreateHints(Position pos)
    {
        switch (_files.Length)
        {
            case 0:
                _hints.Add(new InlayHint
                {
                    Label = new StringOrInlayHintLabelParts("???"),
                    Position = pos,
                    PaddingLeft = true,
                    Tooltip = new StringOrMarkupContent("There is no known asset with this ID.")
                });
                break;

            case 1:
                DiscoveredDatFile f = _files.Value;
                _hints.Add(new InlayHint
                {
                    Label = new StringOrInlayHintLabelParts(f.FriendlyName ?? f.AssetName),
                    Position = pos,
                    PaddingLeft = true,
                    Tooltip = new StringOrMarkupContent(f.Type.GetTypeName()),
                    Data = f.Guid.ToString("D")
                });
                break;

            default:
                _tooltipBuilder ??= new StringBuilder(32);
                _tooltipBuilder.Clear();
                foreach (DiscoveredDatFile file in _files)
                {
                    if (_tooltipBuilder.Length != 0)
                        _tooltipBuilder.Append(", ");

                    _tooltipBuilder.Append(file.FriendlyName ?? file.AssetName);
                }

                _hints.Add(new InlayHint
                {
                    Label = new StringOrInlayHintLabelParts(_files.Length + " assets"),
                    Position = pos,
                    PaddingLeft = true,
                    Tooltip = new StringOrMarkupContent(_tooltipBuilder.ToString())
                });
                break;
        }
    }
}