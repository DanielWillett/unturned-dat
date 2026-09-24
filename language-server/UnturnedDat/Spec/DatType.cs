using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;
using UnturnedDat.Data.Values;

namespace UnturnedDat.Data.Spec;

/// <summary>
/// Types of data specifications
/// </summary>
public enum DatSpecificationType
{
    /// <summary>
    /// A non-asset type of .dat file specification. Represented by a <see cref="DatFileType"/> object.
    /// </summary>
    /// <remarks>Of type <see cref="DatFileType"/>.</remarks>
    File,

    /// <summary>
    /// An asset type of .dat file specification.
    /// </summary>
    /// <remarks>Of type <see cref="DatAssetFileType"/>.</remarks>
    AssetFile,

    /// <summary>
    /// A data structure which is defined in a specification and can be used within .dat files.
    /// </summary>
    /// <remarks>Of type <see cref="DatCustomType"/>.</remarks>
    Custom,

    /// <summary>
    /// A data structure which is defined in a specification and can be used within .dat files.
    /// </summary>
    /// <remarks>Of type <see cref="DatCustomAssetType"/>.</remarks>
    CustomAsset,

    /// <summary>
    /// A set of allowed values which are defined in a specification and can be used within .dat files.
    /// </summary>
    /// <remarks>Of type <see cref="DatEnumType"/>.</remarks>
    Enum,

    /// <summary>
    /// A set of allowed values which are defined in a specification and can be used within .dat files and combined using bitwise operators.
    /// <para>
    /// The C# definition should be annotated with the <see cref="FlagsAttribute"/>.
    /// </para>
    /// </summary>
    /// <remarks>Of type <see cref="DatFlagEnumType"/>.</remarks>
    FlagEnum
}

/// <summary>
/// Contains information about the properties and behavior available for a .dat file type or custom type.
/// </summary>
public abstract class DatType : BaseType<DatType>, IDatSpecificationObject
{
    internal string DisplayNameIntl;

    /// <summary>
    /// The root object of this type, unless it was created at runtime (ex. during a unit test).
    /// </summary>
    public JsonElement DataRoot { get; }

    /// <summary>
    /// The file in which this type is defined. For <see cref="DatFileType"/> objects, this property is equal to the defining object (this).
    /// </summary>
    public abstract DatFileType Owner { get; }

    /// <summary>
    /// The fully-qualified type of the object this type of file defines.
    /// </summary>
    public QualifiedType TypeName { get; }

    /// <inheritdoc />
    public override string Id => TypeName.Type;

    /// <summary>
    /// The type of specification this type defines.
    /// </summary>
    public abstract DatSpecificationType Type { get; }

    /// <summary>
    /// The base type of this type specification, or <see langword="null"/> if this type doesn't have a base type.
    /// </summary>
    public DatTypeWithProperties? BaseType { get; }


    /// <summary>
    /// The display name of the object this type of file defines.
    /// </summary>
    public override string DisplayName => DisplayNameIntl;

    /// <summary>
    /// URL to the SDG docs for this type of file.
    /// </summary>
    public string? Docs { get; internal set; }

    /// <summary>
    /// Whether or not this file type is a project file for this library as opposed to a file read by Unturned.
    /// </summary>
    public bool IsProjectFile { get; internal set; }

    /// <summary>
    /// The version of Unturned this type was added in.
    /// </summary>
    public UnturnedVersion Version { get; internal set; }

    private protected abstract string FullName { get; }

    private protected DatType(QualifiedType type, DatTypeWithProperties? baseType, JsonElement element)
    {
        TypeName = type;
        DisplayNameIntl = type.Type;
        BaseType = baseType;
        DataRoot = element;
    }

    /// <summary>
    /// Creates a new enum type for the given <paramref name="typeName"/>.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the type being created.</param>
    /// <param name="isFlagEnum">Whether or not the enum represents a bitwise flag enum.</param>
    /// <param name="element">The JSON element this type was read from.</param>
    /// <param name="file">The file this type is defined in.</param>
    /// <returns>The newly-created <see cref="DatEnumType"/> or <see cref="DatFlagEnumType"/> instance.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static DatEnumType CreateEnumType(QualifiedType typeName, bool isFlagEnum, JsonElement element, DatFileType file, IDatSpecificationReadContext context)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        if (string.IsNullOrEmpty(typeName.Type))
            throw new ArgumentNullException(nameof(typeName));

        return isFlagEnum
            ? new DatFlagEnumType(typeName, element, file, context)
            : new DatEnumType(typeName, element, file, context);
    }

    /// <summary>
    /// Creates a new custom type for the given <paramref name="typeName"/>.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the type being created.</param>
    /// <param name="element">The JSON element this type was read from.</param>
    /// <param name="baseType">The base type of this custom type.</param>
    /// <param name="file">The file this type is defined in.</param>
    /// <returns>The newly-created <see cref="DatCustomType"/> or <see cref="DatCustomAssetType"/> instance.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static DatCustomType CreateCustomType(QualifiedType typeName, JsonElement element, DatTypeWithProperties? baseType, DatFileType file, IDatSpecificationReadContext context)
    {
        if (string.IsNullOrEmpty(typeName.Type))
            throw new ArgumentNullException(nameof(typeName));

        return file is DatAssetFileType assetFileType
            ? new DatCustomAssetType(typeName, baseType, element, assetFileType, context)
            : new DatCustomType(typeName, baseType, element, file, context);
    }

    /// <summary>
    /// Creates a new file type for the given <paramref name="typeName"/>.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the type being created.</param>
    /// <param name="isAssetFileType">Whether or not this type is assignable to "SDG.Unturned.Asset".</param>
    /// <param name="element">The JSON element this type was read from.</param>
    /// <param name="baseType">The base type of this custom type.</param>
    /// <returns>The newly-created <see cref="DatFileType"/> or <see cref="DatAssetFileType"/> instance.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static DatFileType CreateFileType(QualifiedType typeName, bool isAssetFileType, JsonElement element, DatFileType? baseType, IDatSpecificationReadContext context)
    {
        if (string.IsNullOrEmpty(typeName.Type))
            throw new ArgumentNullException(nameof(typeName));

        return isAssetFileType
            ? new DatAssetFileType(typeName, baseType, element)
            : new DatFileType(typeName, baseType, element);
    }

    /// <summary>
    /// Attempt to get a type by it's fully-qualified type name. Types in this type or any base types will be returned.
    /// </summary>
    public bool TryGetType(QualifiedType typeName, [NotNullWhen(true)] out DatType? type)
    {
        if (!typeName.IsCaseInsensitive)
        {
            typeName = typeName.CaseInsensitive;
        }

        for (DatType? baseType = this; baseType != null; baseType = baseType.BaseType)
        {
            if (typeName.Equals(baseType.TypeName))
            {
                type = baseType;
                return true;
            }

            if (baseType.TryGetTypeInFileIntl(typeName, out type))
            {
                return true;
            }
        }

        type = null;
        return false;
    }

    protected virtual bool TryGetTypeInFileIntl(QualifiedType typeName, [NotNullWhen(true)] out DatType? type)
    {
        type = null;
        return false;
    }

    string IDatSpecificationObject.FullName => FullName;

    public override void Visit<TVisitor>(ref TVisitor visitor) { }
    public override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TypeName.Type);
    }

    /// <inheritdoc />
    protected override bool Equals(DatType other)
    {
        return (object)other == this;
    }

    /// <inheritdoc />
    public override int GetHashCode() => TypeName.GetHashCode();
}

/// <summary>
/// Base type for any <see cref="DatType"/> implementations that have properties.
/// </summary>
public abstract class DatTypeWithProperties : DatType
{
    private bool _hasDoneLocalPropertiesCalculation;
    private DatProperty[]? _propertyMap;
    private int _propertyMapLength, _localizationPropertyMapLength;

    // -2 = uncached, -1 = none, otherwise index in Properties
    private int _subtypeSwitch = -2;

    internal ImmutableArray<DatProperty>.Builder? PropertiesBuilder;

    /// <summary>
    /// List of all available properties that can be stored in this type of file.
    /// </summary>
    public ImmutableArray<DatProperty> Properties { get; internal set; }

    /// <summary>
    /// If <see langword="true"/>, all property keys have the exact same name as their field/property name in code unless otherwise defined.
    /// </summary>
    public bool AutoGeneratedKeys { get; internal set; }

    /// <summary>
    /// If <see langword="true"/>, properties without values are parsed as their default value and don't raise an error.
    /// </summary>
    public bool OverridableProperties { get; internal set; }

    /// <summary>
    /// Whether or not this file type has any localization properties in it or it's property's types.
    /// </summary>
    /// <remarks>This is effectively 'Could this type ever need a language file?'.</remarks>
    public bool HasLocalizationProperties
    {
        get
        {
            if (_hasDoneLocalPropertiesCalculation)
                return field;

            field = CalculateHasLocalizationProperties();
            _hasDoneLocalPropertiesCalculation = true;
            return field;
        }
    }

    /// <summary>
    /// An ordered array of all properties, including base types. This includes localization and bundle asset properties.
    /// </summary>
    /// <remarks>Values use a reference to indices in this array.</remarks>
    internal DatProperty[] AllPropertiesMap
    {
        get
        {
            if (_propertyMap == null)
                CalculatePropertyMap();

            return _propertyMap;
        }
    }

    /// <summary>
    /// An ordered array of all asset properties, including base types.
    /// </summary>
    internal ArraySegment<DatProperty> PropertyMap
    {
        get
        {
            if (_propertyMap == null)
                CalculatePropertyMap();

            return new ArraySegment<DatProperty>(_propertyMap, 0, _propertyMapLength);
        }
    }

    /// <summary>
    /// An ordered array of all localization properties, including base types.
    /// </summary>
    internal ArraySegment<DatProperty> LocalizationPropertyMap
    {
        get
        {
            if (_propertyMap == null)
                CalculatePropertyMap();

            return new ArraySegment<DatProperty>(_propertyMap, _propertyMapLength, _localizationPropertyMapLength);
        }
    }

    /// <summary>
    /// An ordered array of all bundle assets, including base types.
    /// </summary>
    internal ArraySegment<DatBundleAsset> BundleAssetMap
    {
        get
        {
            if (_propertyMap == null)
                CalculatePropertyMap();

            // this is pretty bad but its fine
            DatBundleAsset[] array = Unsafe.As<DatProperty[], DatBundleAsset[]>(ref _propertyMap);

            int startIndex = _propertyMapLength + _localizationPropertyMapLength;
            return new ArraySegment<DatBundleAsset>(array, startIndex, array.Length - startIndex);
        }
    }

    internal DatTypeWithProperties(QualifiedType type, DatTypeWithProperties? baseType, JsonElement element) : base(type, baseType, element)
    {
        Properties = ImmutableArray<DatProperty>.Empty;
    }

    private bool CalculateHasLocalizationProperties()
    {
        if (BaseType is { HasLocalizationProperties: true })
            return true;

        if (this is not IDatTypeWithLocalizationProperties lclProps)
            return false;

        if (!lclProps.LocalizationProperties.IsDefaultOrEmpty || lclProps.LocalizationPropertiesBuilder is { Count: > 0 })
            return true;

        IEnumerable<DatProperty> properties = PropertiesBuilder ?? Properties.AsEnumerable();
        foreach (DatProperty property in properties)
        {
            switch (property.Type)
            {
                case TypeSwitch sw:
                    foreach (ISwitchCase<IType> @case in sw.Cases.OfType<ISwitchCase<IType>>())
                    {
                        if (!@case.Value.TryGetConcreteValue(out Optional<IType> type))
                            continue;

                        if (type.HasValue && TypeCouldHaveLocalizationProperties(type.Value))
                            return true;
                    }

                    break;

                case IType type when TypeCouldHaveLocalizationProperties(type):
                    return true;
            }
        }

        return false;

        static bool TypeCouldHaveLocalizationProperties(IType type)
        {
            return type.TrimmingBehavior >= PropertySearchTrimmingBehavior.CreatesOtherPropertiesInLinkedFiles;
        }
    }

    private protected int GetSubtypeSwitchPropertyIndex()
    {
        if (_subtypeSwitch >= -1)
        {
            return _subtypeSwitch;
        }

        if (PropertiesBuilder != null)
        {
            for (int i = 0; i < PropertiesBuilder.Count; i++)
            {
                DatProperty property = PropertiesBuilder[i];
                if (!string.IsNullOrEmpty(property.SubtypeSwitchPropertyName))
                    return i;
            }

            return -1;
        }

        for (int i = 0; i < Properties.Length; i++)
        {
            DatProperty property = Properties[i];
            if (string.IsNullOrEmpty(property.SubtypeSwitchPropertyName))
                continue;

            _subtypeSwitch = i;
            return i;
        }

        _subtypeSwitch = -1;
        return -1;
    }

    [MemberNotNull(nameof(_propertyMap))]
    private void CalculatePropertyMap()
    {
        IDatTypeWithLocalizationProperties? lclType = this as IDatTypeWithLocalizationProperties;
        IDatTypeWithBundleAssets? bndlType = this as IDatTypeWithBundleAssets;

        if (PropertiesBuilder != null || lclType?.LocalizationPropertiesBuilder != null || bndlType?.BundleAssetsBuilder != null)
            throw new InvalidOperationException("Properties not yet finalized.");

        int mainCount = Properties.Length;
        int localCount = lclType == null ? 0 : lclType.LocalizationProperties.Length;
        int bndlCount = bndlType == null ? 0 : bndlType.BundleAssets.Length;
        for (DatTypeWithProperties? t = BaseType; t != null; t = t.BaseType)
        {
            IDatTypeWithLocalizationProperties? tLclType = t as IDatTypeWithLocalizationProperties;
            IDatTypeWithBundleAssets? tBndlType = t as IDatTypeWithBundleAssets;

            if (t.PropertiesBuilder != null || tLclType?.LocalizationPropertiesBuilder != null || tBndlType?.BundleAssetsBuilder != null)
                throw new InvalidOperationException($"Properties not yet finalized on base type {t.TypeName.GetTypeName()}.");

            mainCount += t.Properties.Length;
            if (tLclType != null)
                localCount += tLclType.LocalizationProperties.Length;
            if (tBndlType != null)
                bndlCount += tBndlType.BundleAssets.Length;
        }

        if (mainCount + localCount + bndlCount == 0)
        {
            _propertyMapLength = 0;
            _localizationPropertyMapLength = 0;
            _propertyMap = Array.Empty<DatProperty>();
            return;
        }

        int mainIndex = 0,
            localIndex = mainCount,
            bndlIndex = mainCount + localCount;

        DatProperty[] propertyList = new DatProperty[mainCount + localCount + bndlCount];
        for (DatTypeWithProperties? t = this; t != null; t = t.BaseType)
        {
            ImmutableArray<DatProperty> properties = t.Properties;
            properties.CopyTo(propertyList, mainIndex);
            mainIndex += properties.Length;
            if (t is IDatTypeWithLocalizationProperties tLclType)
            {
                ImmutableArray<DatProperty> localizationProperties = tLclType.LocalizationProperties;
                localizationProperties.CopyTo(0, propertyList, localIndex, localizationProperties.Length);
                localIndex += localizationProperties.Length;
            }
            if (t is IDatTypeWithBundleAssets tBndlType)
            {
                ImmutableArray<DatBundleAsset> bundleAssets = tBndlType.BundleAssets;
                for (int i = 0; i < bundleAssets.Length; ++i)
                {
                    propertyList[bndlIndex] = bundleAssets[i];
                    ++bndlIndex;
                }
            }
        }

        _propertyMapLength = mainCount;
        _localizationPropertyMapLength = localCount;
        _propertyMap = propertyList;
    }
}

/// <summary>
/// Base interface for any <see cref="DatType"/> implementations that have localization properties.
/// </summary>
public interface IDatTypeWithLocalizationProperties
{
    internal ImmutableArray<DatProperty>.Builder? LocalizationPropertiesBuilder { get; }

    /// <summary>
    /// List of all available properties that can be stored in the localization file for this type of asset.
    /// </summary>
    ImmutableArray<DatProperty> LocalizationProperties { get; }
}

/// <summary>
/// Base interface for any <see cref="DatType"/> implementations that have bundle assets.
/// </summary>
public interface IDatTypeWithBundleAssets
{
    internal ImmutableArray<DatBundleAsset>.Builder? BundleAssetsBuilder { get; }

    /// <summary>
    /// List of all available Unity objects that can be in the bundle for this asset.
    /// </summary>
    ImmutableArray<DatBundleAsset> BundleAssets { get; }
}

/// <summary>
/// Base interface for any <see cref="DatType"/> that can use a string parser.
/// </summary>
public interface IDatTypeWithStringParseableType<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// The type parser for this type.
    /// </summary>
    ITypeConverter<T>? StringParser { get; }

    /// <summary>
    /// Pre-defined fully-qualified C# type name used to parse values of this type.
    /// </summary>
    /// <remarks>This type should implement <see cref="ITypeConverter{T}"/> for the correct type.</remarks>
    QualifiedType StringParseableType { get; }
}

/// <summary>
/// A subclass of <see cref="DatType"/> which is created for non-asset types.
/// </summary>
public class DatFileType : DatTypeWithProperties
{
    // allows type properties to reference other types before they're finalized.
    internal ImmutableDictionary<QualifiedType, DatType>.Builder? TypesBuilder;

    /// <inheritdoc />
    public override DatSpecificationType Type => DatSpecificationType.File;

    /// <inheritdoc />
    public override DatFileType Owner => this;

    /// <summary>
    /// Whether or not this <see cref="DatFileType"/> was generated from the Localization .dat files.
    /// </summary>
    public bool IsLocalizationFile { get; internal set; }

    /// <summary>
    /// Whether or not this localization file should only allow keys (such as the <c>Curse_Words.txt</c> file).
    /// </summary>
    public bool IsKeyOnlyLocalizationFile { get; internal set; }

    private protected override string FullName => TypeName.GetFullTypeName();

    /// <inheritdoc cref="DatType.BaseType" />
    public DatFileType? Parent { get; }

    /// <summary>
    /// List of all available types that are defined within this file.
    /// </summary>
    public ImmutableDictionary<QualifiedType, DatType> Types { get; internal set; }

    internal DatFileType(QualifiedType type, DatFileType? baseType, JsonElement element) : base(type, baseType, element)
    {
        Types = ImmutableDictionary<QualifiedType, DatType>.Empty;
        Parent = baseType;
    }

    protected override bool TryGetTypeInFileIntl(QualifiedType typeName, [NotNullWhen(true)] out DatType? type)
    {
        if (TypesBuilder != null)
        {
            if (TypesBuilder.TryGetValue(typeName, out type))
            {
                return true;
            }
        }
        else if (Types.TryGetValue(typeName, out type))
        {
            return true;
        }

        type = null;
        return false;
    }
}


/// <summary>
/// A subclass of <see cref="DatType"/> which is only created for Asset types, including the base type, "SDG.Unturned.Asset".
/// </summary>
public class DatAssetFileType : DatFileType, IDatTypeWithLocalizationProperties, IDatTypeWithBundleAssets
{
    internal ImmutableArray<DatProperty>.Builder? LocalizationPropertiesBuilder { get; set; }

    internal ImmutableArray<DatBundleAsset>.Builder? BundleAssetsBuilder { get; set; }

    /// <inheritdoc />
    public override DatSpecificationType Type => DatSpecificationType.AssetFile;

    /// <summary>
    /// The category of the asset represented by this type.
    /// </summary>
    public AssetCategoryValue Category { get; internal set; }

    /// <summary>
    /// The first ID which isn't reserved by vanilla assets.
    /// </summary>
    public ushort? VanillaIdLimit { get; internal set; }

    /// <summary>
    /// Whether or not assets of this type must have a legacy ID to function correctly within the game.
    /// </summary>
    public bool RequireId { get; internal set; }

    /// <summary>
    /// Whether or not this type or any of it's parent types can have bundle assets.
    /// </summary>
    public bool HasBundleAssets { get; internal set; }

    /// <inheritdoc />
    public ImmutableArray<DatProperty> LocalizationProperties { get; internal set; }

    /// <inheritdoc />
    public ImmutableArray<DatBundleAsset> BundleAssets { get; internal set; }

    internal DatAssetFileType(QualifiedType type, DatFileType? baseType, JsonElement element) : base(type, baseType, element)
    {
        LocalizationProperties = ImmutableArray<DatProperty>.Empty;
        BundleAssets = ImmutableArray<DatBundleAsset>.Empty;
    }

    ImmutableArray<DatProperty>.Builder? IDatTypeWithLocalizationProperties.LocalizationPropertiesBuilder
        => LocalizationPropertiesBuilder;
    ImmutableArray<DatBundleAsset>.Builder? IDatTypeWithBundleAssets.BundleAssetsBuilder
        => BundleAssetsBuilder;
}