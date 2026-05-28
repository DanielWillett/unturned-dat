using UnturnedDat.Data.Project;

namespace UnturnedDat.LanguageServer.Project;

internal class LspProjectFile(string filePath, string folderPath) : ProjectFile(filePath)
{
    public string FolderPath { get; internal set; } = folderPath;

    internal ScaffoldedPropertyOrderFile? EffectiveOrderFile;
}