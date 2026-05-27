using System;
using System.Runtime.CompilerServices;

namespace DanielWillett.UnturnedDataFileLspServer.Data.Utility;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

unsafe partial class MathMatrix
{
    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor, TIdealOut>(string inVal, ref TVisitor visitor)
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TIdealOut : IEquatable<TIdealOut>
    {
        AsinDegVisitor<TVisitor, TIdealOut> visitorProxy;
        visitorProxy.Result = false;
        fixed (TVisitor* ptr = &visitor)
        {
            visitorProxy.Visitor = ptr;
            ConvertToNumber<TIdealOut, AsinDegVisitor<TVisitor, TIdealOut>>(inVal, ref visitorProxy);
        }
        return visitorProxy.Result;
    }
    
    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(ulong inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(uint inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(ushort inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(byte inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(long inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(int inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(short inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(sbyte inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(float inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(MathF.Asin(inVal) * (180f / MathF.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(double inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin(inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TVisitor>(decimal inVal, ref TVisitor visitor) where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        visitor.Accept(Math.Asin((double)inVal) * (180 / Math.PI));
        return true;
    }

    /// <inheritdoc cref="AsinDeg{TIn,TVisitor,TIdealOut}"/>
    public static bool AsinDeg<TIn, TVisitor>(TIn inVal, ref TVisitor visitor)
        where TIn : IEquatable<TIn>
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return AsinDeg<TIn, TVisitor, TIn>(inVal, ref visitor);
    }

    /// <summary>
    /// Performs an inverse sine operation returning a value in degrees.
    /// </summary>
    /// <typeparam name="TIn">Input value.</typeparam>
    /// <typeparam name="TVisitor">Visitor type which will accept the result.</typeparam>
    /// <typeparam name="TIdealOut">The ideal type to return. This is usually used when the input type is a <see cref="string"/>.</typeparam>
    public static bool AsinDeg<TIn, TVisitor, TIdealOut>(TIn? inVal, ref TVisitor visitor)
        where TIn : IEquatable<TIn>
        where TIdealOut : IEquatable<TIdealOut>
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

    {
        if (typeof(TIn) == typeof(ulong))
            return AsinDeg(As<TIn, ulong>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(uint))
            return AsinDeg(As<TIn, uint>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(ushort))
            return AsinDeg(As<TIn, ushort>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(byte))
            return AsinDeg(As<TIn, byte>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(long))
            return AsinDeg(As<TIn, long>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(int))
            return AsinDeg(As<TIn, int>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(short))
            return AsinDeg(As<TIn, short>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(sbyte))
            return AsinDeg(As<TIn, sbyte>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(float))
            return AsinDeg(As<TIn, float>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(double))
            return AsinDeg(As<TIn, double>(inVal!), ref visitor);
        if (typeof(TIn) == typeof(decimal))
            return AsinDeg(As<TIn, decimal>(inVal!), ref visitor);

        if (inVal == null)
        {
            visitor.Accept<string>(null);
            return true;
        }

#if SUBSEQUENT_COMPILE
        IVectorTypeProvider<TIn>? vectorProvider = VectorTypes.TryGetProvider<TIn>();

        if (vectorProvider != null)
        {
            TIn abs = vectorProvider.TrigOperation(inVal, op: 3, deg: true);
            visitor.Accept(abs);
            return true;
        }
#endif

        if (typeof(TIn) == typeof(string))
        {
            return AsinDeg<TVisitor, TIdealOut>(As<TIn, string>(inVal), ref visitor);
        }

        return false;
    }

    private struct AsinDegVisitor<TVisitor, TIdealOut> : IGenericVisitor
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
            Result = AsinDeg<T, TVisitor, TIdealOut>(value, ref Unsafe.AsRef<TVisitor>(Visitor));
        }
    }
}