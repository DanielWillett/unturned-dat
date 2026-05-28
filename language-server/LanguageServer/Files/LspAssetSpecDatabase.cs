using Microsoft.Extensions.Logging;
using System.Text.Json;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Utility;

namespace UnturnedDat.LanguageServer.Files;

internal sealed class LspAssetSpecDatabase : AssetSpecDatabase
{
    public LspAssetSpecDatabase(
        ILoggerFactory loggerFactory,
        Lazy<IParsingServices> parsingServices,
        JsonSerializerOptions options,
        ISpecDatabaseCache cache
    ) : base(
        new SpecificationFileReader(
            allowInternet: true,
            loggerFactory,
            new Lazy<HttpClient>(() => new HttpClient()),
            options,
            new InstallDirUtility("Unturned", "304930"),
            cache: cache
        )
        {
#if DEBUG
            ReadFromCache = false
#endif
        },
        parsingServices
    )
    {
        Options = options;
    }
}