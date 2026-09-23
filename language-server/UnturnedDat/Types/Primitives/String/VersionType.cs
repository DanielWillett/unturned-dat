using System.Text.Json;
using UnturnedDat.Data.Diagnostics;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Utility;
using UnturnedDat.Data.Values;

namespace UnturnedDat.Data.Types;

/// <summary>
/// A string parsed as a 4 digit Unturned version, supporting a <c>"o:"</c> prefix to deal with version ambiguity.
/// <para>Currently unused in Unturned</para>
/// <code>
/// Prop 3.26.1.0
///
/// // Indicates that this is referencing the 3.25.2.0 version released in July 2018,
/// // not the one released in March 2025, which would be the default.
/// Prop o:3.25.2.0
/// </code>
/// </summary>
public sealed class VersionType : PrimitiveType<UnturnedVersion, VersionType>, ITypeParser<UnturnedVersion>
{
    public const string TypeId = "Version";
    
    public override string Id => TypeId;

    public override string DisplayName => Resources.Type_Name_Version;

    public override ITypeParser<UnturnedVersion> Parser => this;

    public VersionType() { }

    public bool TryParse(ref TypeParserArgs<UnturnedVersion> args, ref FileEvaluationContext ctx, out Optional<UnturnedVersion> value)
    {
        if (TypeParsers.TryApplyMissingValueBehavior(ref args, ref ctx, out value, out bool rtn))
        {
            return rtn;
        }

        if (!TypeParsers.TryParseStringValueOnly(ref args, out IValueSourceNode? valueNode))
        {
            value = Optional<UnturnedVersion>.Null;
            args.Result = TypeParserResult.Failed;
            return false;
        }

        if (!UnturnedVersion.TryParse(valueNode.Value, out UnturnedVersion version))
        {
            args.DiagnosticSink?.UNT2004_Generic(ref args, valueNode.Value, this);
            args.Result = TypeParserResult.Failed;
            return false;
        }

        value = version;
        args.Result = TypeParserResult.Successful;
        return true;
    }

    #region JSON

    public bool TryReadValueFromJson<TDataRefReadContext>(
        in JsonElement json,
        out Optional<UnturnedVersion> value,
        IType<UnturnedVersion> valueType,
        ref TDataRefReadContext dataRefContext
    ) where TDataRefReadContext : IDataRefReadContext?
    {
        TypeConverterParseArgs<UnturnedVersion> args = new TypeConverterParseArgs<UnturnedVersion>(this);
        return TypeConverters.Version.TryReadJson(in json, out value, ref args);
    }

    public void WriteValueToJson(Utf8JsonWriter writer, UnturnedVersion value, IType<UnturnedVersion> valueType, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    #endregion

    public override int GetHashCode()
    {
        return 333922896;
    }
}