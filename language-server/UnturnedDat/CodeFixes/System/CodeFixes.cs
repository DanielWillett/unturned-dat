using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Properties;

namespace UnturnedDat.Data.CodeFixes;

public sealed class GlobalCodeFixes
{
    // private readonly IFilePropertyVirtualizer _virtualizer;
    // private readonly IAssetSpecDatabase _database;
    // private readonly InstallationEnvironment _installEnv;
    // private readonly IWorkspaceEnvironment _workspaceEnv;

    internal ICodeFix[] AllArray { get; }

    public IReadOnlyList<ICodeFix> All { get; }

    public GlobalCodeFixes(
        IFileRelationalModelProvider modelProvider,
        IParsingServices parsingServices)
    {
        // _virtualizer = virtualizer;
        // _database = database;
        // _installEnv = installEnv;
        // _workspaceEnv = workspaceEnv;

        AllArray =
        [
            new BlueprintUseThisKeyword(modelProvider, parsingServices),
            new GenerateNewGuid(modelProvider, parsingServices),
            new UnknownProperty(modelProvider, parsingServices)
        ];

        for (int i = 0; i < AllArray.Length; ++i)
        {
            AllArray[i].Index = i;
        }

        All = new ReadOnlyCollection<ICodeFix>(AllArray);
    }
}
