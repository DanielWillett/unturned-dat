using UnturnedDat.Data.Project;
using UnturnedDat.Data.Spec;
using UnturnedDat.LanguageServer.Files;

namespace UnturnedDat.LanguageServer.Project;

internal class LspProjectFileProvider : IProjectFileProvider
{
    private readonly LspWorkspaceEnvironment _workspaceEnvironment;
    private readonly IAssetSpecDatabase _database;

    internal LspProjectFileProvider(LspWorkspaceEnvironment workspaceEnvironment, IAssetSpecDatabase database)
    {
        _workspaceEnvironment = workspaceEnvironment;
        _database = database;
    }

    /// <inheritdoc />
    public IEnumerable<ProjectFile> EnumerateProjectFiles(IWorkspaceFile? fileContext)
    {
        return Array.Empty<ProjectFile>();
    }

    /// <inheritdoc />
    public IPropertyOrderFile GetScaffoldedOrderfile(IWorkspaceFile? fileContext)
    {
        return _database.GlobalOrderFile;
    }

    /// <inheritdoc />
    public T? AggregateProjectFiles<T>(IWorkspaceFile? fileContext, Func<ProjectFile, T> selector)
        where T : class?
    {
        return null;
    }

    /// <inheritdoc />
    public T? AggregateProjectFiles<T>(IWorkspaceFile? fileContext, Func<ProjectFile, T?> selector)
        where T : struct
    {
        return null;
    }
}
