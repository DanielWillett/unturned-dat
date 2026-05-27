
using DanielWillett.UnturnedDataFileLspServer.Data;
using DanielWillett.UnturnedDataFileLspServer.Data.Files;
using DanielWillett.UnturnedDataFileLspServer.Data.Parsing;
using DanielWillett.UnturnedDataFileLspServer.Data.Project;
using DanielWillett.UnturnedDataFileLspServer.Data.Properties;
using DanielWillett.UnturnedDataFileLspServer.Data.Spec;
using DanielWillett.UnturnedDataFileLspServer.Data.Types;
using DanielWillett.UnturnedDataFileLspServer.Data.Utility;
using DanielWillett.UnturnedDataFileLspServer.Files;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Runtime.CompilerServices;
using System.Text;

namespace DanielWillett.UnturnedDataFileLspServer.Handlers;

internal class InlayHintsHandler : IInlayHintsHandler
{
    private readonly IParsingServices _parsingServices;
    private readonly IFileRelationalModelProvider _modelProvider;
    private readonly OpenedFileTracker _fileTracker;

    public InlayHintsHandler(
        IParsingServices parsingServices,
        IFileRelationalModelProvider modelProvider,
        OpenedFileTracker fileTracker)
    {
        _parsingServices = parsingServices;
        _modelProvider = modelProvider;
        _fileTracker = fileTracker;
    }

    public Task<InlayHintContainer?> Handle(InlayHintParams request, CancellationToken cancellationToken)
    {
        if (!_fileTracker.Files.TryGetValue(request.TextDocument.Uri, out OpenedFile? file))
        {
            return Task.FromResult<InlayHintContainer?>(new InlayHintContainer());
        }

        List<InlayHint> hints = new List<InlayHint>(4);

        ISourceFile sourceFile = file.SourceFile;
        InlayHintVisitor visitor = new InlayHintVisitor(hints, _modelProvider, _parsingServices, request.Range?.ToFileRange());
        sourceFile.Visit(ref visitor);

        return Task.FromResult<InlayHintContainer?>(new InlayHintContainer(hints));
    }


    InlayHintRegistrationOptions IRegistration<InlayHintRegistrationOptions, InlayHintClientCapabilities>.GetRegistrationOptions(
        InlayHintClientCapabilities capability, ClientCapabilities clientCapabilities)
    {
        return new InlayHintRegistrationOptions
        {
            DocumentSelector = UnturnedAssetFileLspServer.AssetFileSelector,
            ResolveProvider = false,
            WorkDoneProgress = false
        };
    }
}

file class InlayHintVisitor : ResolvedPropertyNodeVisitor, ITypeVisitor
{
    private readonly IParsingServices _parsingServices;
    private readonly List<InlayHint> _hints;

    private OneOrMore<DiscoveredDatFile> _files = OneOrMore<DiscoveredDatFile>.Null;
    private IPropertySourceNode? _property;
    private StringBuilder? _tooltipBuilder;

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
        if (propertyType is not IAssetReferenceType assetRefType)
        {
            return;
        }

        InlayHintVisitor @this = this;
        @this._files = OneOrMore<DiscoveredDatFile>.Null;
        @this._property = node;
        assetRefType.Visit(ref @this);

        OneOrMore<DiscoveredDatFile> files = @this._files;

        FilePosition endPos = node.ValueKind == SourceValueType.Value
                                  ? node.GetValueRange().End
                                  : node.Range.End;

        Position pos = new Position(endPos.Line - 1, endPos.Character);

        switch (files.Length)
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
                foreach (DiscoveredDatFile file in files)
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

        if (typeof(TValue) == typeof(Guid))
        {
            Guid guid = Unsafe.As<TValue, Guid>(ref Unsafe.AsRef(in value.Value));
            _files = _parsingServices.Installation.FindFile(guid);
        }
        else if (typeof(TValue) == typeof(ushort))
        {
            ushort id = Unsafe.As<TValue, ushort>(ref Unsafe.AsRef(in value.Value));
            int c = _parsingServices.Database.Information.GetAssetCategory(((IAssetReferenceType)type).BaseTypes);
            if (c == 0)
                return;

            _files = _parsingServices.Installation.FindFile(id, new AssetCategoryValue(c));
        }
        else if (typeof(TValue) == typeof(GuidOrId))
        {
            GuidOrId guidOrId = Unsafe.As<TValue, GuidOrId>(ref Unsafe.AsRef(in value.Value));
            _files = _parsingServices.Installation.FindFile(guidOrId);
        }
    }
}