using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using UnturnedDat.Data.Diagnostics;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Project;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.Data.Values;

/// <summary>
/// Pushed to the <see cref="DatObjectStack"/> when parsing a <see cref="DatCustomType"/>.
/// </summary>
internal sealed class ObjectStackObjectContext : IObjectStackContext, ITypeVisitor, IReferencedPropertySink
{
    private FileEvaluationContext _evalContext;
    private TypeParserArgs<DatObjectValue> _parserArgs;
    private FileEvaluationContext _altEvalContext;

    // Unspecified = failed to setup alt context
    private SpecPropertyContext _altEvalFileType;

    private readonly DatProperty[] _propertyMap;
    private DatObjectPropertyValue[]? _values;
    private LightweightBitArray _propertyVisitedMap;


    public DatCustomType Type { get; }
    public PropertyResolutionContext Context { get; }
    public IObjectStackContext? Parent { get; set; }

    private string? _baseKey;
    private bool? _explicitlyReferencedParentNode;
    private bool _implicitlyReferencedParentNode;
    private int _referenceCount;

    public ObjectStackObjectContext(
        PropertyResolutionContext context,
        DatCustomType type,
        ref FileEvaluationContext evalContext,
        ref TypeParserArgs<DatObjectValue> parserArgs)
    {
        Context = context;
        Type = type;
        _evalContext = evalContext;
        _parserArgs = parserArgs;
        _propertyMap = type.AllPropertiesMap;

        _altEvalFileType = _evalContext.PropertyContext is SpecPropertyContext.Localization or SpecPropertyContext.CrossReferenceLocalization
            ? SpecPropertyContext.Property
            : SpecPropertyContext.Localization;

        _propertyVisitedMap = new LightweightBitArray(_propertyMap.Length);
    }

    /// <summary>
    /// Get the value of a property while the object is being read.
    /// </summary>
    /// <remarks>This is here so later object properties can be referenced by other properties in the same object without creating infinite loops or extra work.</remarks>
    public bool TryGetPropertyValue(DatProperty property, [NotNullWhen(true)] out IValue? value)
    {
        int index = Array.IndexOf(_propertyMap, property);
        if (index < 0)
            throw new ArgumentException($@"Property {property.FullName} doesn't belong to {Type.TypeName.GetTypeName()}.", nameof(property));

        if (_values == null)
            throw new InvalidOperationException("Not currently being parsed.");

        if (_propertyVisitedMap[index])
        {
            value = _values[index].Value ?? throw new InvalidOperationException($"Circular reference detected near {property.FullName}.");
            return true;
        }

        if (!TryVisitProperty(index, out _, out _))
        {
            value = null;
            return false;
        }

        value = _values[index].Value;
        return true;
    }

    /// <summary>
    /// Try to parse an entire object.
    /// </summary>
    public bool TryParse([NotNullWhen(true)] out DatObjectValue? parsedObject)
    {
        if (_values != null)
            throw new InvalidOperationException("Nested TryParse call.");

        if (_propertyMap.Length == 0)
        {
            parsedObject = new DatObjectValue(Type, ImmutableArray<DatObjectPropertyValue>.Empty);
            return true;
        }

        bool anyPassed = _propertyMap.Length == 0;
        bool anyDidntUseDefaultValue = anyPassed;

        DatObjectPropertyValue[] values = new DatObjectPropertyValue[_propertyMap.Length];

        if (Context == PropertyResolutionContext.Legacy)
        {
            string? baseKey = _parserArgs.BaseKey;
            if (!string.IsNullOrEmpty(baseKey) && baseKey[^1] != '_')
            {
                baseKey += "_";
            }

            _baseKey = baseKey;

            _explicitlyReferencedParentNode = null;
            _implicitlyReferencedParentNode = false;
        }

        _values = values;
        try
        {
            for (int i = 0; i < _propertyMap.Length; ++i)
            {
                if (_propertyVisitedMap[i])
                    continue;

                if (!TryVisitProperty(i, out _, out _))
                {
                    continue;
                }

                anyPassed = true;

                if (_visitorResult is TypeParserResult.UsedDefaultValue
                    or TypeParserResult.UsedDefaultValueNoneAvailable
                    or TypeParserResult.UsedIncludedDefaultValue
                    or TypeParserResult.UsedIncludedDefaultValueNoneAvailable)
                {
                    continue;
                }

                anyDidntUseDefaultValue = true;
            }
        }
        finally
        {
            _values = null;
            _baseKey = null;
        }

        // was legacy base property referenced? ex. was "Condition_0" referenced instead of just it's properties, like "Condition_0_ID"
        if (Context == PropertyResolutionContext.Legacy
            && (_implicitlyReferencedParentNode ? _explicitlyReferencedParentNode is false : _explicitlyReferencedParentNode is not true)
            && _parserArgs.ParentNode is IPropertySourceNode prop)
        {
            _parserArgs.ReferencedPropertySink?.AcceptDereferencedProperty(prop);
        }

        if (!anyPassed)
        {
            parsedObject = null;
            return false;
        }

        if (!anyDidntUseDefaultValue)
        {
            switch (_parserArgs.MissingValueBehavior)
            {
                default:
                case TypeParserMissingValueBehavior.ErrorOnlyIfValueNotProvided:
                    if (_parserArgs.ParentNode is not IPropertySourceNode)
                        goto case TypeParserMissingValueBehavior.FallbackToDefaultValue;

                    goto case TypeParserMissingValueBehavior.ErrorIfValueOrPropertyNotProvided;

                case TypeParserMissingValueBehavior.ErrorIfValueOrPropertyNotProvided:
                    if (Context == PropertyResolutionContext.Modern)
                        _parserArgs.DiagnosticSink?.UNT2004_NoDictionary(ref _parserArgs, _parserArgs.ParentNode);
                    else
                        _parserArgs.DiagnosticSink?.UNT2004_NoValue(ref _parserArgs, _parserArgs.ParentNode);

                    _parserArgs.Result = TypeParserResult.Failed;
                    break;

                case TypeParserMissingValueBehavior.FallbackToDefaultValue:
                    bool useIncludedDefault = _parserArgs.ParentNode is IPropertySourceNode;
                    if (_parserArgs.Property == null)
                    {
                        _parserArgs.Result = useIncludedDefault
                            ? TypeParserResult.UsedIncludedDefaultValueNoneAvailable
                            : TypeParserResult.UsedDefaultValueNoneAvailable;
                        parsedObject = null;
                        return false;
                    }

                    bool success = _parserArgs.Property.TryEvaluateDefaultValue(
                        useIncludedDefault,
                        ref _evalContext,
                        out Optional<DatObjectValue> parsedObjectOpt,
                        out TypeParserResult result
                    );

                    _parserArgs.Result = result;
                    if (success && parsedObjectOpt.HasValue)
                    {
                        parsedObject = parsedObjectOpt.Value;
                        return true;
                    }

                    parsedObject = null;
                    return false;
            }
        }

        parsedObject = new DatObjectValue(Type, values.UnsafeFreeze(), isConcrete: true);
        return true;
    }

    private bool TryVisitProperty(int index, out bool isIncluded, out bool hasValue)
    {
        DatProperty property = _propertyMap[index];

        isIncluded = false;
        hasValue = false;

        // alternate context is used when, for example, an asset file is reading an object that also has a localization property
        // the alt context has a reference to the localization file instead.
        bool useAltContext;
        switch (property.Context)
        {
            case SpecPropertyContext.Localization:
            case SpecPropertyContext.CrossReferenceLocalization:
                useAltContext = _altEvalFileType == SpecPropertyContext.Localization;
                break;

            case SpecPropertyContext.BundleAsset:
                // todo
                return false;

            default:
                useAltContext = _altEvalFileType == SpecPropertyContext.Property;
                break;
        }

        if (useAltContext && _altEvalContext.File == null)
        {
            if (_altEvalFileType == SpecPropertyContext.Unspecified || !SetupAltContext())
            {
                if (_evalContext.PropertyContext is SpecPropertyContext.Localization or SpecPropertyContext.CrossReferenceLocalization)
                    _parserArgs.DiagnosticSink?.UNT1030_RequiredAsset(ref _parserArgs, property.GetFirstKey(Context.ToKeyFilter()));
                else    
                    _parserArgs.DiagnosticSink?.UNT1030_RequiredLocal(ref _parserArgs, property.GetFirstKey(Context.ToKeyFilter()));
                return false;
            }
        }

        ref FileEvaluationContext ctx = ref _evalContext;
        if (useAltContext)
            ctx = ref _altEvalContext;

        // before evaluating anything (type)
        _propertyVisitedMap[index] = true;
        _values![index] = new DatObjectPropertyValue(NullValue.Instance, property);

        if (!property.Type.TryEvaluateType(out IType? propertyType, ref ctx))
        {
            return false;
        }

        // find relevant property node
        bool foundProperty;
        IDictionarySourceNode? parentDictionary;
        IPropertySourceNode? propertyNode;
        if (Context == PropertyResolutionContext.Legacy)
        {
            if (useAltContext)
            {
                parentDictionary = _altEvalContext.File!;
            }
            else
            {
                // parent dictionary is the dictionary the parent property is contained in
                parentDictionary = _parserArgs.ParentNode as IDictionarySourceNode ?? (_parserArgs.ParentNode as IPropertySourceNode)?.Parent as IDictionarySourceNode;
                if (parentDictionary == null)
                    throw new InvalidOperationException("Parent node should be a property or dictionary.");
            }

            foundProperty = parentDictionary.TryGetProperty(property, ref ctx, out propertyNode, LegacyExpansionFilter.Legacy, _baseKey);
            if (foundProperty && propertyNode == _parserArgs.ParentNode)
                _implicitlyReferencedParentNode = true;
        }
        else
        {
            if (useAltContext)
            {
                parentDictionary = _altEvalContext.File!;
            }
            else
            {
                // parent dictionary is this object's dictionary
                parentDictionary = _parserArgs.ValueNode as IDictionarySourceNode;
                if (parentDictionary == null)
                    throw new InvalidOperationException("Value node should be a dictionary.");
            }

            foundProperty = parentDictionary.TryGetProperty(property, ref ctx, out propertyNode, LegacyExpansionFilter.Modern);
        }

        if (!foundProperty && propertyType.TrimmingBehavior <= PropertySearchTrimmingBehavior.CreatesSiblingPropertiesInSameFile)
        {
            if (property.TryEvaluateDefaultValue(false, ref ctx, out IValue? defaultValue, out _))
            {
                _values![index] = new DatObjectPropertyValue(defaultValue, property);
                return true;
            }

            return HandleExcludedPropertyHasNoDefaultValue(property, ref ctx, parentDictionary);
        }

        if (propertyType.TrimmingBehavior <= PropertySearchTrimmingBehavior.CreatesSiblingPropertiesInSameFile)
        {
            // foundProperty is checked above, will always be true here
            isIncluded = true;
        }

        if (foundProperty)
        {
            _parserArgs.ReferencedPropertySink?.AcceptReferencedProperty(propertyNode!);
        }

        int startingRefCount = _referenceCount;

        _visitorValue = null;
        _visitingIndex = index;
        _parentNode = (IParentSourceNode?)propertyNode ?? parentDictionary;
        _valueNode = propertyNode?.Value;
        _visitorUseAltContext = useAltContext;
        _visitorResult = TypeParserResult.Failed;

        ObjectStackObjectContext visitor = this;
        propertyType.Visit(ref visitor);

        if (_visitorValue == null)
        {
            return false;
        }

        if (_referenceCount > startingRefCount)
        {
            isIncluded = true;
        }

        hasValue = _visitorResult
            is TypeParserResult.Successful
            or TypeParserResult.UsedIncludedDefaultValueNoneAvailable
            or TypeParserResult.UsedIncludedDefaultValue
            or TypeParserResult.Failed;

        IValue value = _visitorValue;
        _visitorValue = null;
        _values![index] = new DatObjectPropertyValue(value, property, propertyNode);
        return true;
    }

    #region DataRef Properties

    public bool EvaluateIsIncluded(DatProperty? includedProperty, in IncludedProperty property)
    {
        if (includedProperty == null)
        {
            if (Context == PropertyResolutionContext.Legacy)
            {
                // not really supported?
                return true;
            }

            if (_parentNode is IPropertySourceNode propNode)
            {
                return !property.RequireValue || propNode.HasValue;
            }

            return false;
        }

        int index = Array.IndexOf(_propertyMap, includedProperty);
        if (index < 0)
            return false;

        if (TryVisitProperty(index, out bool isIncluded, out bool hasValue))
        {
            return isIncluded && (!property.RequireValue || hasValue);
        }

        return isIncluded && !property.RequireValue;
    }

    public bool EvaluateIsExcluded(DatProperty? includedProperty, in ExcludedProperty property)
    {
        if (includedProperty == null)
        {
            if (Context == PropertyResolutionContext.Legacy)
            {
                // not really supported?
                return false;
            }

            return _parentNode is not IPropertySourceNode;
        }

        int index = Array.IndexOf(_propertyMap, includedProperty);
        if (index < 0)
            return true;

        TryVisitProperty(index, out bool isIncluded, out _);
        return !isIncluded;
    }

    [return: NotNullIfNotNull(nameof(includedProperty))]
    public string? EvaluateKey(DatProperty? includedProperty, in KeyProperty property)
    {
        IPropertySourceNode? propertyNode;
        if (includedProperty == null)
        {
            if (Context == PropertyResolutionContext.Legacy)
            {
                string? baseKey = _parserArgs.BaseKey;
                if (!string.IsNullOrEmpty(baseKey) && baseKey[^1] == '_')
                    baseKey = baseKey[..^1];

                return baseKey;
            }

            propertyNode = _parserArgs.ParentNode as IPropertySourceNode;
            return propertyNode?.Key;
        }

        int index = Array.IndexOf(_propertyMap, includedProperty);
        if (index < 0)
            return includedProperty.GetFirstKey(Context.ToKeyFilter());

        if (!_propertyVisitedMap[index])
        {
            if (!TryVisitProperty(index, out _, out _))
                return includedProperty.GetFirstKey(Context.ToKeyFilter());
        }

        ref DatObjectPropertyValue value = ref _values![index];

        propertyNode = value.Node as IPropertySourceNode;
        if (propertyNode == null)
            return includedProperty.GetFirstKey(Context.ToKeyFilter());

        string key = propertyNode.Key;
        if (Context == PropertyResolutionContext.Legacy
            && !string.IsNullOrEmpty(_parserArgs.BaseKey)
            && key.Length > _parserArgs.BaseKey.Length
            && key.StartsWith(_parserArgs.BaseKey, StringComparison.OrdinalIgnoreCase))
        {
            if (_parserArgs.BaseKey[^1] == '_')
                return key.Substring(_parserArgs.BaseKey.Length);

            if (key.Length > _parserArgs.BaseKey.Length + 1)
                return key.Substring(_parserArgs.BaseKey.Length + 1);
        }

        return key;
    }

    public bool TryEvaluateValueType(DatProperty? includedProperty, in ValueTypeProperty property, out SourceValueType type)
    {
        type = SourceValueType.Value;

        IPropertySourceNode? propertyNode;
        if (includedProperty == null)
        {
            if (Context == PropertyResolutionContext.Legacy)
            {
                // not really supported i guess? they'd all be values anyways.
                return false;
            }

            propertyNode = _parserArgs.ParentNode as IPropertySourceNode;
            if (propertyNode == null)
                return false;

            type = propertyNode.ValueKind;
            return true;

        }

        int index = Array.IndexOf(_propertyMap, includedProperty);
        if (index < 0)
            return false;

        if (!_propertyVisitedMap[index])
        {
            if (!TryVisitProperty(index, out _, out _))
                return false;
        }

        ref DatObjectPropertyValue value = ref _values![index];

        propertyNode = value.Node as IPropertySourceNode;
        if (propertyNode == null)
            return false;

        type = propertyNode.ValueKind;
        return true;
    }

    #endregion

    #region ITypeVisitor

    private int _visitingIndex;
    private IParentSourceNode? _parentNode;
    private IAnyValueSourceNode? _valueNode;
    private bool _visitorUseAltContext;
    private IValue? _visitorValue;
    private TypeParserResult _visitorResult;

    public void Accept<TValue>(IType<TValue> type) where TValue : IEquatable<TValue>
    {
        DatProperty property = _propertyMap[_visitingIndex];

        IReferencedPropertySink? propSink = _parserArgs.ReferencedPropertySink;
        if (Context == PropertyResolutionContext.Legacy || type.TrimmingBehavior > PropertySearchTrimmingBehavior.CreatesSiblingPropertiesInSameFile)
        {
            propSink = this;
        }

        TypeParserArgs<TValue> parseArgs = new TypeParserArgs<TValue>
        {
            DiagnosticSink = _parserArgs.DiagnosticSink,
            ReferencedPropertySink = propSink,
            KeyFilter = Context.ToKeyFilter(),
            Property = property,
            ValueNode = _valueNode,
            ParentNode = _parentNode!,
            Type = type,
            MissingValueBehavior = TypeParserMissingValueBehavior.FallbackToDefaultValue,
            Result = TypeParserResult.Failed
        };

        ref FileEvaluationContext ctx = ref _evalContext;
        if (_visitorUseAltContext)
            ctx = ref _altEvalContext;

        if (Context == PropertyResolutionContext.Legacy)
        {
            parseArgs.BaseKey = _baseKey;
        }

        if (!type.Parser.TryParse(ref parseArgs, ref ctx, out Optional<TValue> value))
            return;

        _visitorValue = type.CreateValue(value);
        _visitorResult = parseArgs.Result;
    }

    #endregion

    private bool HandleExcludedPropertyHasNoDefaultValue(DatProperty property, ref FileEvaluationContext ctx, IDictionarySourceNode parentDictionary)
    {
        if (_parserArgs.DiagnosticSink == null
            || property.Required == null
            || !property.Required.TryEvaluateValue(out Optional<bool> isRequiredOpt, ref ctx)
            || !isRequiredOpt.Value)
        {
            return false;
        }

        // emit missing required field diagnostic
        if (Context == PropertyResolutionContext.Modern)
        {
            if (parentDictionary.Parent is IPropertySourceNode propertyNode)
                _parserArgs.DiagnosticSink.UNT2014_Object(ref _parserArgs, property.GetFirstKey(LegacyExpansionFilter.Modern), PropertyBreadcrumbs.FromNode(propertyNode).ToString());
        }
        else
        {
            _parserArgs.DiagnosticSink.UNT2014_Object(ref _parserArgs, property.GetFirstKey(LegacyExpansionFilter.Legacy), _baseKey ?? string.Empty);
        }

        return false;
    }

    private bool SetupAltContext()
    {
        ISourceFile? file = _altEvalFileType switch
        {
            SpecPropertyContext.Property => (_evalContext.File as ILocalizationSourceFile)?.Asset,
            /* SpecPropertyContext.Localization */ _ => (_evalContext.File as IAssetSourceFile)?.GetDefaultLocalizationFile(
                _evalContext.Services.ProjectFileProvider.GetPreferredLanguage(_evalContext.File.WorkspaceFile)
            )
        };

        if (file == null)
        {
            _altEvalFileType = SpecPropertyContext.Unspecified;
            return false;
        }

        AssetDatPropertyPosition rootPosition = AssetDatPropertyPosition.Root;
        if (_altEvalFileType == SpecPropertyContext.Property && file is IAssetSourceFile asset && asset.GetAssetDataDictionary() != null)
        {
            rootPosition = AssetDatPropertyPosition.Asset;
        }

        _altEvalContext = new FileEvaluationContext(_evalContext.Services, file, rootPosition);
        return true;
    }

    internal void ApplyEvaluationContextChanges(ref FileEvaluationContext ctx, ref TypeParserArgs<DatObjectValue> args)
    {
        ctx = _evalContext;
        args = _parserArgs;
    }

    #region IReferencedPropertySink

    void IReferencedPropertySink.AcceptReferencedProperty(IPropertySourceNode property)
    {
        if (property == _parserArgs.ParentNode)
            _explicitlyReferencedParentNode = true;

        _parserArgs.ReferencedPropertySink?.AcceptReferencedProperty(property);
        ++_referenceCount;
    }

    void IReferencedPropertySink.AcceptDereferencedProperty(IPropertySourceNode property)
    {
        if (property == _parserArgs.ParentNode)
            _explicitlyReferencedParentNode = false;

        _parserArgs.ReferencedPropertySink?.AcceptDereferencedProperty(property);
    }

    #endregion

    ObjectStackContextType IObjectStackContext.Type => ObjectStackContextType.Object;
}