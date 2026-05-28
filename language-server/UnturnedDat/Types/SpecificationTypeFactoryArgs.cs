using UnturnedDat.Data.Spec;

namespace UnturnedDat.Data.Types;

/// <summary>
/// Used to create types decorated with the <see cref="SpecificationTypeAttribute"/>.
/// </summary>
public readonly struct SpecificationTypeFactoryArgs
{
    public IDatSpecificationReadContext Context { get; }
    public IDatSpecificationObject Owner { get; }
    public string TypeId { get; }

    public SpecificationTypeFactoryArgs(IDatSpecificationReadContext context, IDatSpecificationObject owner, string typeId)
    {
        Context = context;
        Owner = owner;
        TypeId = typeId;
    }
}
