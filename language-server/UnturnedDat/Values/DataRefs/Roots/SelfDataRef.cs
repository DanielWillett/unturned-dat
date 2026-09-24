using System.Diagnostics.CodeAnalysis;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Spec;

namespace UnturnedDat.Data.Values;

/// <summary>
/// Data-ref referencing the current property.
/// </summary>
public sealed class SelfDataRef : RootDataRef<SelfDataRef>
{
    /// <summary>
    /// The property being referred to by this data-ref.
    /// </summary>
    public DatProperty Owner { get; }

    public SelfDataRef(DatProperty owner)
    {
        Owner = owner;
    }

    /// <inheritdoc />
    public override string PropertyName => "Self";

    protected override bool IsPropertyNameKeyword => true;

    /// <inheritdoc />
    public override bool VisitValue<TVisitor>(ref TVisitor visitor, ref FileEvaluationContext ctx)
    {
        // NOTE: it doesn't make sense to return the value of the current property
        //       since that's what's being evaluated in this function.

        return false;
    }

    protected override bool AcceptProperty(in IncludedProperty property, ref FileEvaluationContext ctx, out bool value)
    {
        if (PropertyReference.TryFindPropertyOwnerFromContext(Owner, out ObjectStackObjectContext? context))
        {
            value = context?.EvaluateIsIncluded(Owner, in property) ?? Owner.IsIncluded(property.RequireValue, ref ctx);
            return true;
        }

        value = false;
        return false;
    }

    protected override bool AcceptProperty(in ExcludedProperty property, ref FileEvaluationContext ctx, out bool value)
    {
        if (PropertyReference.TryFindPropertyOwnerFromContext(Owner, out ObjectStackObjectContext? context))
        {
            value = context?.EvaluateIsExcluded(Owner, in property) ?? Owner.IsExcluded(ref ctx);
            return true;
        }

        value = false;
        return false;
    }

    protected override bool AcceptProperty(in KeyProperty property, ref FileEvaluationContext ctx, [NotNullWhen(true)] out string? value)
    {
        if (PropertyReference.TryFindPropertyOwnerFromContext(Owner, out ObjectStackObjectContext? context))
        {
            string key = context != null
                ? context.EvaluateKey(Owner, in property)
                : PropertyDataRef.GetPropertyKey(Owner, ref ctx);

            value = key;
            return key != null;
        }

        value = null;
        return false;
    }

    protected override bool AcceptProperty<TVisitor>(in IndicesProperty property, ref FileEvaluationContext ctx, ref TVisitor visitor)
    {
        return IndicesProperty.TryGetCurrentIndices(Owner, in property, ref visitor);
    }

    protected override bool AcceptProperty(in ValueTypeProperty property, ref FileEvaluationContext ctx, [NotNullWhen(true)] out string? value)
    {
        if (PropertyReference.TryFindPropertyOwnerFromContext(Owner, out ObjectStackObjectContext? context))
        {
            SourceValueType valueType;
            if (context != null)
            {
                if (!context.TryEvaluateValueType(Owner, in property, out valueType))
                {
                    value = null;
                    return false;
                }
            }
            else
            {
                valueType = Owner.GetValueType(ref ctx);
            }

            value = ValueTypeProperty.GetTypeName(valueType);
            return true;
        }

        value = null;
        return false;
    }

    protected override bool AcceptProperty(in CountProperty property, ref FileEvaluationContext ctx, out int value)
    {
        if (CountProperty.TryGetCurrentCount(Owner, in property, out value, ref ctx))
        {
            return true;
        }

        value = 0;
        return false;
    }

    protected override bool AcceptProperty<TVisitor>(in ComponentProperty property, ref FileEvaluationContext ctx, ref TVisitor visitor)
    {
        if (Owner is not DatBundleAsset bundleAsset)
        {
            return false;
        }

        return property.GetValue(bundleAsset, ref ctx, ref visitor);
    }

    /// <inheritdoc />
    protected override bool Equals(SelfDataRef other)
    {
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return 1176347863;
    }
}