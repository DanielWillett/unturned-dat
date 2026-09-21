using System;
using System.IO;
using UnturnedDat.Data.Project;
using UnturnedDat.Data.Spec;

namespace UnturnedDat.Data.Files;

/// <summary>
/// Create a <see cref="IWorkspaceFile"/> without any extra state.
/// </summary>
public class ReferencedWorkspaceFile : IWorkspaceFile
{
    private readonly string _fullText;

    public IAssetSpecDatabase Database { get; }

    /// <inheritdoc />
    public string File { get; }

    /// <inheritdoc />
    public ISourceFile SourceFile { get; }

    /// <inheritdoc />
    public IBundleProxy Bundle { get; }

    /// <inheritdoc />
    public string GetFullText()
    {
        return _fullText;
    }

    event Action<IWorkspaceFile, FileRange>? IWorkspaceFile.OnUpdated
    {
        add { }
        remove { }
    }

    public ReferencedWorkspaceFile(
        string file,
        IAssetSpecDatabase database,
        object? state,
        string text,
        Func<ReferencedWorkspaceFile, object?, string, ISourceFile> factory,
        IBundleProxy? bundle = null)
    {
        Database = database;
        File = Path.GetFullPath(file);
        Bundle = bundle ?? IBundleProxy.Null;
        SourceFile = factory(this, state, text);
        _fullText = text;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (SourceFile is IDisposable disp)
            disp.Dispose();
    }
}
