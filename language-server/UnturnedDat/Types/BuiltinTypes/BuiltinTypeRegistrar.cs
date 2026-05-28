using System.Collections.Generic;
using UnturnedDat.Data.Spec;

namespace UnturnedDat.Data.Types;

internal static class BuiltinTypeRegistrar
{
    internal static void OnFinalizingTypes(IDatSpecificationReadContext context, ICollection<DatType> types)
    {
        types.Add(Orderfile.FromDatabase(context.Database));
        types.Add(new ProjectFileType(context));
    }
}
