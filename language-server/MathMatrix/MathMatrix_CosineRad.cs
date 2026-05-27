using System;
using System.Runtime.CompilerServices;

namespace DanielWillett.UnturnedDataFileLspServer.Data.Utility;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

unsafe partial class MathMatrix
{
    // note: most of these functions will fail with very large numbers due to the way decimals are represented
    // see https://stackoverflow.com/questions/66030225/different-results-between-c-and-c-sharp-sin-function-with-large-values
    // for more info

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor, TIdealOut>(string inVal, ref TVisitor visitor)
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TIdealOut : IEquatable<TIdealOut>
    {
        CosRadVisitor<TVisitor, TIdealOut> visitorProxy;
        visitorProxy.Result = false;
        fixed (TVisitor* ptr = &visitor)
        {
            visitorProxy.Visitor = ptr;
            ConvertToNumber<TIdealOut, CosRadVisitor<TVisitor, TIdealOut>>(inVal, ref visitorProxy);
        }
        return visitorProxy.Result;
    }
    
    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(ulong inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(uint inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(ushort inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(byte inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(long inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(int inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(short inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(sbyte inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(float inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(MathF.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(double inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos(inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TVisitor>(decimal inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Cos((double)inVal));
        return true;
    }

    /// <inheritdoc cref="CosRad{TIn,TVisitor,TIdealOut}"/>
    public static bool CosRad<TIn, TVisitor>(TIn inVal, ref TVisitor visitor)
        where TIn : IEquatable<TIn>
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return CosRad<TIn, TVisitor, TIn>(inVal, ref visitor);
    }

    /// <summary>
    /// Performs a cosine operation returning a value in radians.
    /// </summary>
    /// <typeparam name="TIn">Input value.</typeparam>
    /// <typeparam name="TVisitor">Visitor type which will accept the result.</typeparam>
    /// <typeparam name="TIdealOut">The ideal type to return. This is usually used when the input type is a <see cref="string"/>.</typeparam>
    public static bool CosRad<TIn, TVisitor, TIdealOut>(TIn? inVal, ref TVisitor visitor)
        where TIn : IEquatable<TIn>
        where TIdealOut : IEquatable<TIdealOut>
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

    {
        if (typeof(TIn) == typeof(ulong))
            return CosRad(As<TIn, ulong>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(uint))
            return CosRad(As<TIn, uint>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(ushort))
            return CosRad(As<TIn, ushort>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(byte))
            return CosRad(As<TIn, byte>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(long))
            return CosRad(As<TIn, long>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(int))
            return CosRad(As<TIn, int>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(short))
            return CosRad(As<TIn, short>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(sbyte))
            return CosRad(As<TIn, sbyte>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(float))
            return CosRad(As<TIn, float>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(double))
            return CosRad(As<TIn, double>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(decimal))
            return CosRad(As<TIn, decimal>(inVal!), ref visitor);

        if (inVal == null)
        {
            visitor.Accept<string>(null);
            return true;
        }

#if SUBSEQUENT_COMPILE
        IVectorTypeProvider<TIn>? vectorProvider = VectorTypes.TryGetProvider<TIn>();

        if (vectorProvider != null)
        {
            TIn abs = vectorProvider.TrigOperation(inVal, op: 1, deg: false);
            visitor.Accept(abs);
            return true;
        }
#endif

        if (typeof(TIn) == typeof(string))
        {
            return CosRad<TVisitor, TIdealOut>(As<TIn, string>(inVal), ref visitor);
        }

        return false;
    }

    private struct CosRadVisitor<TVisitor, TIdealOut> : IGenericVisitor
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
            Result = CosRad<T, TVisitor, TIdealOut>(value, ref Unsafe.AsRef<TVisitor>(Visitor));
        }
    }
}