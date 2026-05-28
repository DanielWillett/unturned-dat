#if NET9_0_OR_GREATER

// Note this requires .NET 9 because it relies on the 'allows ref struct' constraint.
// Otherwise NET7_0_OR_GREATER would work.
#define __USE_REF_STRUCTS

#endif

using System;
using System.Runtime.CompilerServices;
using UnturnedDat.Data.Files;
using UnturnedDat.Data.Types;
using UnturnedDat.Data.Utility;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace UnturnedDat.Data.Values.Expressions;


internal struct ExpressionEvaluator
{
    private readonly IFunctionExpressionNode _root;
    private readonly IExpressionFunction _func;
    private int _arg;

    internal ExpressionEvaluator(IFunctionExpressionNode root, int arg = -1)
    {
        _root = root;
        _func = root.Function;
        _arg = arg;
    }

    public unsafe bool Evaluate<TIdealOut, TVisitor>(ref TVisitor resultVisitor, bool concreteOnly, ref FileEvaluationContext ctx)
        where TIdealOut : IEquatable<TIdealOut>
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (_root.Count <= 0)
        {
            return _func.Evaluate<TIdealOut, TVisitor>(ref resultVisitor);
        }

        _arg = 0;

#if __USE_REF_STRUCTS
        scoped
#endif
            Arg0Eval<TIdealOut, TVisitor> v;
        v.WasSuccessful = false;
        v.ConcreteOnly = concreteOnly;
#if __USE_REF_STRUCTS
        v.EvalCtx = ref ctx;
        v.Evaluator = ref this;
        fixed (TVisitor* resultVisitorPtr = &resultVisitor)
        {
            v.ResultVisitor = resultVisitorPtr;
            EvaluateArgument(ref v, concreteOnly, ref ctx);
        }
#else
        fixed (TVisitor* resultVisitorPtr = &resultVisitor)
        fixed (FileEvaluationContext* ctxPtr = &ctx)
        fixed (ExpressionEvaluator* e = &this)
        {
            v.ResultVisitor = resultVisitorPtr;
            v.EvalCtx = ctxPtr;
            v.Evaluator = e;
            EvaluateArgument(ref v, concreteOnly, ref ctx);
        }
#endif

        return v.WasSuccessful;
    }

    private unsafe bool EvaluateArgument<TArgVisitor>(ref TArgVisitor argVisitor, bool concreteOnly, ref FileEvaluationContext ctx)
        where TArgVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        switch (_root[_arg])
        {
            case IFunctionExpressionNode function:
                IType? idealType = _func.GetIdealArgumentType(_arg);
                if (idealType is NumericAnyType or null)
                {
                    ExpressionEvaluator evaluator = new ExpressionEvaluator(function);
                    return evaluator.Evaluate<double, TArgVisitor>(ref argVisitor, concreteOnly, ref ctx);
                }

                TypeEvaluateVisitor<TArgVisitor> evalVisitor;
                evalVisitor.Function = function;
                evalVisitor.ConcreteOnly = concreteOnly;
                evalVisitor.Visited = false;
                fixed (TArgVisitor* argVisitorPtr = &argVisitor)
                fixed (FileEvaluationContext* c = &ctx)
                {
                    evalVisitor.Visitor = argVisitorPtr;
                    evalVisitor.EvalCtx = c;
                    idealType.Visit(ref evalVisitor);
                }

                return evalVisitor.Visited;

            case IValueExpressionNode simpleValue:
                return concreteOnly
                    ? simpleValue.VisitConcreteValueGeneric(ref argVisitor)
                    : simpleValue.VisitValueGeneric(ref argVisitor, ref ctx);

            case IPropertyReferenceExpressionNode propRef:
                if (concreteOnly)
                    return false;

                return propRef.Value.VisitValueGeneric(ref argVisitor, ref ctx);

            case IDataRefExpressionNode dataRef:
                if (concreteOnly)
                    return false;

                return dataRef.DataRef.VisitValueGeneric(ref argVisitor, ref ctx);
        }

        return false;
    }

    private unsafe struct TypeEvaluateVisitor<TVisitor> : ITypeVisitor
        where TVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public IFunctionExpressionNode Function;
        public TVisitor* Visitor;
        public FileEvaluationContext* EvalCtx;
        public bool ConcreteOnly;
        public bool Visited;

        public void Accept<TValue>(IType<TValue> type) where TValue : IEquatable<TValue>
        {
            ref FileEvaluationContext ctx = ref Unsafe.AsRef<FileEvaluationContext>(EvalCtx);

            ExpressionEvaluator evaluator = new ExpressionEvaluator(Function);
            Visited = evaluator.Evaluate<TValue, TVisitor>(ref Unsafe.AsRef<TVisitor>(Visitor), ConcreteOnly, ref ctx);
        }
    }

    private unsafe
#if __USE_REF_STRUCTS
        ref
#endif
        struct Arg0Eval<TIdealOut, TResultVisitor> : IGenericVisitor
        where TIdealOut : IEquatable<TIdealOut>
        where TResultVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public TResultVisitor* ResultVisitor;
#if __USE_REF_STRUCTS
        public ref ExpressionEvaluator Evaluator;
        public ref FileEvaluationContext EvalCtx;
#else
        public ExpressionEvaluator* Evaluator;
        public FileEvaluationContext* EvalCtx;
#endif
        public bool ConcreteOnly;
        public bool WasSuccessful;

        public void Accept<T>(T? value) where T : IEquatable<T>
        {
            ReducerVisitor rv;
            rv.ResultVisitor = ResultVisitor;
            rv.WasSuccessful = false;
#if __USE_REF_STRUCTS
            rv.Evaluator = ref Evaluator;
            rv.EvalCtx = ref EvalCtx;
#else
            rv.Evaluator = Evaluator;
            rv.EvalCtx = EvalCtx;
#endif
            rv.ConcreteOnly = ConcreteOnly;

#if __USE_REF_STRUCTS
            if (!Evaluator._root.Function.ReduceToKnownTypes
#else
            if (!Evaluator->_root.Function.ReduceToKnownTypes
#endif
                || MathMatrix.IsValidMathExpressionInputType<T>()
                || !MathMatrix.TryReduce(value!, ref rv))
            {
                rv.Accept(value);
            }

            WasSuccessful = rv.WasSuccessful;
        }

        private
#if __USE_REF_STRUCTS
            ref
#endif
            struct ReducerVisitor : IGenericVisitor
        {
            public TResultVisitor* ResultVisitor;
#if __USE_REF_STRUCTS
            public ref ExpressionEvaluator Evaluator;
            public ref FileEvaluationContext EvalCtx;
#else
            public ExpressionEvaluator* Evaluator;
            public FileEvaluationContext* EvalCtx;
#endif
            public bool WasSuccessful;
            public bool ConcreteOnly;

            public void Accept<T>(T? value) where T : IEquatable<T>
            {
#if __USE_REF_STRUCTS
                if (Evaluator._root.Count == 1)
                {
                    WasSuccessful = Evaluator._func.Evaluate<T, TIdealOut, TResultVisitor>(value!, ref Unsafe.AsRef<TResultVisitor>(ResultVisitor));
                    return;
                }
#else
                if (Evaluator->_root.Count == 1)
                {
                    WasSuccessful = Evaluator->_func.Evaluate<T, TIdealOut, TResultVisitor>(value!, ref Unsafe.AsRef<TResultVisitor>(ResultVisitor));
                    return;
                }
#endif

                Arg1Eval<T, TIdealOut, TResultVisitor> v;
#if __USE_REF_STRUCTS
                v.EvalCtx = ref EvalCtx;
                v.Evaluator = ref Evaluator;
#else
                v.Evaluator = Evaluator;
                v.EvalCtx = EvalCtx;
#endif
                v.ResultVisitor = ResultVisitor;
                v.WasSuccessful = false;
                v.ConcreteOnly = ConcreteOnly;
                v.Arg0 = value;
#if __USE_REF_STRUCTS
                Evaluator._arg = 1;
                Evaluator.EvaluateArgument(ref v, ConcreteOnly, ref EvalCtx);
#else
                Evaluator->_arg = 1;
                Evaluator->EvaluateArgument(ref v, ConcreteOnly, ref Unsafe.AsRef<FileEvaluationContext>(EvalCtx));
#endif
                WasSuccessful = v.WasSuccessful;

            }
        }
    }

    private unsafe
#if __USE_REF_STRUCTS
        ref
#endif
        struct Arg1Eval<TArg0, TIdealOut, TResultVisitor> : IGenericVisitor
        where TArg0 : IEquatable<TArg0>
        where TIdealOut : IEquatable<TIdealOut>
        where TResultVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public TArg0? Arg0;
        public bool WasSuccessful;
        public bool ConcreteOnly;

        public TResultVisitor* ResultVisitor;
#if __USE_REF_STRUCTS
        public ref ExpressionEvaluator Evaluator;
        public ref FileEvaluationContext EvalCtx;
#else
        public ExpressionEvaluator* Evaluator;
        public FileEvaluationContext* EvalCtx;
#endif

        public void Accept<T>(T? value) where T : IEquatable<T>
        {
            ReducerVisitor rv;
            rv.ResultVisitor = ResultVisitor;
            rv.WasSuccessful = false;
#if __USE_REF_STRUCTS
            rv.Evaluator = ref Evaluator;
            rv.EvalCtx = ref EvalCtx;
#else
            rv.Evaluator = Evaluator;
            rv.EvalCtx = EvalCtx;
#endif
            rv.ConcreteOnly = ConcreteOnly;
            rv.Arg0 = Arg0;

#if __USE_REF_STRUCTS
            if (!Evaluator._root.Function.ReduceToKnownTypes
#else
            if (!Evaluator->_root.Function.ReduceToKnownTypes
#endif
                || MathMatrix.IsValidMathExpressionInputType<T>()
                || !MathMatrix.TryReduce(value!, ref rv))
            {
                rv.Accept(value);
            }

            WasSuccessful = rv.WasSuccessful;
        }

        private
#if __USE_REF_STRUCTS
            ref
#endif
            struct ReducerVisitor : IGenericVisitor
        {
            public TArg0? Arg0;
            public bool WasSuccessful;
            public bool ConcreteOnly;
            public TResultVisitor* ResultVisitor;
#if __USE_REF_STRUCTS
            public ref ExpressionEvaluator Evaluator;
            public ref FileEvaluationContext EvalCtx;
#else
            public ExpressionEvaluator* Evaluator;
            public FileEvaluationContext* EvalCtx;
#endif

            public void Accept<T>(T? value) where T : IEquatable<T>
            {
#if __USE_REF_STRUCTS
                if (Evaluator._root.Count == 2)
                {
                    WasSuccessful = Evaluator._func.Evaluate<TArg0, T, TIdealOut, TResultVisitor>(Arg0!, value!, ref Unsafe.AsRef<TResultVisitor>(ResultVisitor));
                    return;
                }
#else
                if (Evaluator->_root.Count == 2)
                {
                    WasSuccessful = Evaluator->_func.Evaluate<TArg0, T, TIdealOut, TResultVisitor>(Arg0!, value!, ref Unsafe.AsRef<TResultVisitor>(ResultVisitor));
                    return;
                }
#endif

                Arg2Eval<TArg0, T, TIdealOut, TResultVisitor> v;
                v.ResultVisitor = ResultVisitor;
                v.WasSuccessful = false;
#if __USE_REF_STRUCTS
                v.Evaluator = ref Evaluator;
#else
                v.Evaluator = Evaluator;
#endif
                v.Arg0 = Arg0;
                v.Arg1 = value;
#if __USE_REF_STRUCTS
                Evaluator._arg = 2;
                Evaluator.EvaluateArgument(ref v, ConcreteOnly, ref EvalCtx);
#else
                Evaluator->_arg = 2;
                Evaluator->EvaluateArgument(ref v, ConcreteOnly, ref Unsafe.AsRef<FileEvaluationContext>(EvalCtx));
#endif
                WasSuccessful = v.WasSuccessful;
            }
        }
    }

    private unsafe
#if __USE_REF_STRUCTS
        ref
#endif
        struct Arg2Eval<TArg0, TArg1, TIdealOut, TResultVisitor> : IGenericVisitor
        where TArg0 : IEquatable<TArg0>
        where TArg1 : IEquatable<TArg1>
        where TIdealOut : IEquatable<TIdealOut>
        where TResultVisitor : IGenericVisitor
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public TArg0? Arg0;
        public TArg1? Arg1;
        public bool WasSuccessful;

        public TResultVisitor* ResultVisitor;
#if __USE_REF_STRUCTS
        public ref ExpressionEvaluator Evaluator;
#else
        public ExpressionEvaluator* Evaluator;
#endif

        public void Accept<T>(T? value) where T : IEquatable<T>
        {
            ReducerVisitor rv;
            rv.ResultVisitor = ResultVisitor;
            rv.WasSuccessful = false;
#if __USE_REF_STRUCTS
            rv.Evaluator = ref Evaluator;
#else
            rv.Evaluator = Evaluator;
#endif
            rv.Arg0 = Arg0;
            rv.Arg1 = Arg1;

#if __USE_REF_STRUCTS
            if (!Evaluator._root.Function.ReduceToKnownTypes
#else
            if (!Evaluator->_root.Function.ReduceToKnownTypes
#endif
                || MathMatrix.IsValidMathExpressionInputType<T>()
                || !MathMatrix.TryReduce(value!, ref rv))
            {
                rv.Accept(value);
            }

            WasSuccessful = rv.WasSuccessful;
        }

        private
#if __USE_REF_STRUCTS
            ref
#endif
            struct ReducerVisitor : IGenericVisitor
        {
            public TArg0? Arg0;
            public TArg1? Arg1;
            public bool WasSuccessful;

            public TResultVisitor* ResultVisitor;
#if __USE_REF_STRUCTS
            public ref ExpressionEvaluator Evaluator;
#else
            public ExpressionEvaluator* Evaluator;
#endif

            public void Accept<T>(T? value) where T : IEquatable<T>
            {
#if __USE_REF_STRUCTS
                if (Evaluator._root.Count == 3)
                {
                    WasSuccessful = Evaluator._func.Evaluate<TArg0, TArg1, T, TIdealOut, TResultVisitor>(Arg0!, Arg1!, value!, ref Unsafe.AsRef<TResultVisitor>(ResultVisitor));
                    return;
                }

                throw new FormatException($"Too many arguments supplied for function \"{Evaluator._func.FunctionName}\".");
#else
                if (Evaluator->_root.Count == 3)
                {
                    WasSuccessful = Evaluator->_func.Evaluate<TArg0, TArg1, T, TIdealOut, TResultVisitor>(Arg0!, Arg1!, value!, ref Unsafe.AsRef<TResultVisitor>(ResultVisitor));
                    return;
                }

                throw new FormatException($"Too many arguments supplied for function \"{Evaluator->_func.FunctionName}\".");
#endif
            }
        }
    }
}