using System;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using UnturnedDat.Data.Diagnostics;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Spec;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Values;

namespace UnturnedDat.Data.Project;

public class ProjectFile(string filePath)
{
    public string FilePath { get; set; } = filePath;

    public string? Orderfile { get; set; }

    public GuidStyle? GuidStyle { get; set; }

    public string? PreferredLanguage { get; set; }

    public static bool TryReadFromFile(
        ref FileEvaluationContext ctx,
        [NotNullWhen(true)] out ProjectFile? file,
        IDiagnosticSink? diagnosticSink = null,
        IReferencedPropertySink? referencedPropertySink = null
    )
    {
        ProjectFile pj = new ProjectFile(ctx.File.WorkspaceFile.File);
        if (!pj.TryUpdateFromFile(ref ctx, diagnosticSink, referencedPropertySink))
        {
            file = null;
            return false;
        }

        file = pj;
        return true;
    }

    public bool TryUpdateFromFile(
        ref FileEvaluationContext ctx,
        IDiagnosticSink? diagnosticSink = null,
        IReferencedPropertySink? referencedPropertySink = null
    )
    {
        if (!ctx.Services.Database.FileTypes.TryGetValue(new QualifiedType(ProjectFileType.TypeId, true), out DatFileType? fileType))
        {
            ctx.Services.CreateLogger<ProjectFile>().LogWarning($"Database not yet initialized, or type {ProjectFileType.TypeId} isn't available.");
            return false;
        }

        Reset();

        foreach (DatProperty property in fileType.Properties)
        {
            if (property.TryGetValue(
                    ref ctx,
                    out IValue? value,
                    out _,
                    diagnosticSink,
                    referencedPropertySink,
                    TypeParserMissingValueBehavior.FallbackToDefaultValue
                ))
            {
                HandlePropertyValue(ref ctx, property, value);
            }
            else
            {
                ctx.Services.CreateLogger<ProjectFile>().LogWarning(
                    $"Failed to parse project file \"{ctx.File.WorkspaceFile.File}\" property {ctx.RootBreadcrumbs.ToString(false, property.Key)}."
                );
            }
        }

        return true;
    }

    private void HandlePropertyValue(ref FileEvaluationContext ctx, DatProperty property, IValue? value)
    {
        switch (property.Key)
        {
            case "Orderfile":
                if (value.TryGetValueAs(ref ctx, out string? orderfile) && !string.IsNullOrEmpty(orderfile))
                {
                    Orderfile = orderfile;
                }

                break;

            case "Guid_Style":
                if (value.TryGetValueAs(ref ctx, out DatEnumValue? guidStyleValue) && guidStyleValue != null)
                {
                    GuidStyle = ProjectFileType.StyleEnumMap[guidStyleValue.Index];
                }

                break;

            case "Language":
                if (value.TryGetValueAs(ref ctx, out string? language) && !string.IsNullOrEmpty(language))
                {
                    if (language.Length < 128)
                    {
                        Span<char> newLanguage = stackalloc char[language.Length];
                        newLanguage[0] = char.ToUpperInvariant(language[0]);
                        for (int i = 1; i < language.Length; ++i)
                            newLanguage[i] = char.ToLowerInvariant(language[i]);

                        language = newLanguage.ToString();
                    }

                    PreferredLanguage = language;
                }

                break;
        }
    }

    private void Reset()
    {
        PreferredLanguage = null;
        GuidStyle = null;
        Orderfile = null;
    }
}

public enum GuidStyle
{
    NormalLower,
    NormalUpper,
    DashesLower,
    DashesUpper,
    BracesLower,
    BracesUpper,
    ParenthesisLower,
    ParenthesisUpper,
    HexLower,
    HexUpper
}