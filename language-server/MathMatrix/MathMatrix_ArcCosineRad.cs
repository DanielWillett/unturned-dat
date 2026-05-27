using System;
using System.Runtime.CompilerServices;

namespace DanielWillett.UnturnedDataFileLspServer.Data.Utility;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

unsafe partial class MathMatrix
{
    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor, TIdealOut>(string inVal, ref TVisitor visitor)
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TIdealOut : IEquatable<TIdealOut>
    {
        AcosRadVisitor<TVisitor, TIdealOut> visitorProxy;
        visitorProxy.Result = false;
        fixed (TVisitor* ptr = &visitor)
        {
            visitorProxy.Visitor = ptr;
            ConvertToNumber<TIdealOut, AcosRadVisitor<TVisitor, TIdealOut>>(inVal, ref visitorProxy);
        }
        return visitorProxy.Result;
    }
    
    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(ulong inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(uint inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(ushort inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(byte inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(long inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(int inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(short inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(sbyte inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(float inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(MathF.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(double inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos(inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TVisitor>(decimal inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Acos((double)inVal));
        return true;
    }

    /// <inheritdoc cref="AcosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool AcosRad<TIn, TVisitor>(TIn inVal, ref TVisitor visitor)
        where TIn : IEquatable<TIn>
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return AcosRad<TIn, TVisitor, TIn>(inVal, ref visitor);
    }

    /// <summary>
    /// Performs an inverse cosine operation returning a value in radians.
    /// </summary>
    /// <typeparam name="TIn">Input value.</typeparam>
    /// <typeparam name="TVisitor">Visitor type which will accept the result.</typeparam>
    /// <typeparam name="TIdealOut">The ideal type to return. This is usually used when the input type is a <see cref="string"/>.</typeparam>
    public static bool AcosRad<TIn, TVisitor, TIdealOut>(TIn? inVal, ref TVisitor visitor)
        where TIn : IEquatable<TIn>
        where TIdealOut : IEquatable<TIdealOut>
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

    {
        if (typeof(TIn) == typeof(ulong))
            return AcosRad(As<TIn, ulong>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(uint))
            return AcosRad(As<TIn, uint>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(ushort))
            return AcosRad(As<TIn, ushort>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(byte))
            return AcosRad(As<TIn, byte>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(long))
            return AcosRad(As<TIn, long>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(int))
            return AcosRad(As<TIn, int>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(short))
            return AcosRad(As<TIn, short>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(sbyte))
            return AcosRad(As<TIn, sbyte>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(float))
            return AcosRad(As<TIn, float>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(double))
            return AcosRad(As<TIn, double>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(decimal))
            return AcosRad(As<TIn, decimal>(inVal!), ref visitor);

        if (inVal == null)
        {
            visitor.Accept<string>(null);
            return true;
        }
#if SUBSEQUENT_COMPILE
        IVectorTypeProvider<TIn>? vectorProvider = VectorTypes.TryGetProvider<TIn>();

        if (vectorProvider != null)
        {
            TIn abs = vectorProvider.TrigOperation(inVal, op: 4, deg: false);
            visitor.Accept(abs);
            return true;
        }
#endif

        if (typeof(TIn) == typeof(string))
        {
            return AcosRad<TVisitor, TIdealOut>(As<TIn, string>(inVal), ref visitor);
        }

        return false;
    }

    private struct AcosRadVisitor<TVisitor, TIdealOut> : IGenericVisitor
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TIdealOut : IEquatable<TIdealOut>
    {
        public TVisitor* Visitor;
        public bool Result;

        public void Accept<T>(T? value) where T : IEquatable<T>
        {
            Result = AcosRad<T, TVisitor, TIdealOut>(value, ref Unsafe.AsRef<TVisitor>(Visitor));
        }
    }
}