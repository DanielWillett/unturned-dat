using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using UnturnedDat.LanguageServer.NewtonsoftConverters;

namespace UnturnedDat.LanguageServer.Protocol;
internal class UnturnedLspSerializer : LspSerializer
{
    protected override void AddOrReplaceConverters(ICollection<JsonConverter> converters)
    {
        base.AddOrReplaceConverters(converters);
        ReplaceConverter(converters, new GuidOrIdConverter());
        ReplaceConverter(converters, new BundleReferenceConverter());
        ReplaceConverter(converters, new QualifiedTypeConverter());
        ReplaceConverter(converters, new UnityEngineVersionConverter());
    }
}
