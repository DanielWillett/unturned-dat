namespace UnturnedDat.Data.Diagnostics;

public interface IDiagnosticSink
{
    void AcceptDiagnostic(DatDiagnosticMessage diagnostic);
}
