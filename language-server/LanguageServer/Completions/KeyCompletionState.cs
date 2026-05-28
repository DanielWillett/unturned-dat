using UnturnedDat.Data.Files;
using UnturnedDat.Data.Spec;
using UnturnedDat.LanguageServer.Files;

namespace UnturnedDat.LanguageServer.Completions;

public struct KeyCompletionState
{
    public ISourceNode? Node { get; }
    public DatProperty Property { get; set; }
    public OpenedFile File { get; }
    public FilePosition Position { get; }
    public bool IsOnNewLine { get; }
    public InverseTypeHierarchy TypeHierarchy { get; }
    public string? Alias { get; set; }

    public KeyCompletionState(ISourceNode? node, FilePosition position, bool isOnNewLine, InverseTypeHierarchy typeHierarchy, string? alias, DatProperty property, OpenedFile file)
    {
        Node = node;
        Position = position;
        IsOnNewLine = isOnNewLine;
        TypeHierarchy = typeHierarchy;
        Alias = alias;
        Property = property;
        File = file;
    }
}