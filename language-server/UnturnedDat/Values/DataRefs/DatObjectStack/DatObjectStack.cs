using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace UnturnedDat.Data.Values;

/// <summary>
/// Keeps track of a stack of values keeping track of the current object type, a reference to it, etc. Also tracks lists.
/// </summary>
internal static class DatObjectStack
{
    private static ThreadLocal<Stack<IObjectStackContext>>? _stackLocal;

    [MemberNotNull(nameof(_stackLocal))]
    private static void EnsureLocalCreated()
    {
        if (_stackLocal != null)
            return;

        ThreadLocal<Stack<IObjectStackContext>> stackLocale = new ThreadLocal<Stack<IObjectStackContext>>();
        ThreadLocal<Stack<IObjectStackContext>>? old = Interlocked.CompareExchange(ref _stackLocal, stackLocale, null);
        if (old == null)
            return;

        stackLocale.Dispose();
    }

    /// <summary>
    /// Push a new context to this thread's object stack.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not <see cref="IObjectStackContext.Legacy"/> or <see cref="IObjectStackContext.Modern"/>.</exception>
    public static void Push(IObjectStackContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        EnsureLocalCreated();

        Stack<IObjectStackContext>? value = _stackLocal.Value;
        if (value == null)
        {
            value = new Stack<IObjectStackContext>(8);
            _stackLocal.Value = value;
        }

        if (value.Count > 0)
        {
            context.Parent = value.Peek();
        }
        value.Push(context);
    }

    /// <summary>
    /// Remove the latest context from this thread's object stack.
    /// </summary>
    /// <returns>Whether or not there was anything on the stack to pop.</returns>
    public static bool Pop()
    {
        EnsureLocalCreated();

        Stack<IObjectStackContext>? value = _stackLocal.Value;
        if (value == null || value.Count == 0)
        {
            return false;
        }

        value.Pop();
        return true;
    }

    /// <summary>
    /// Get the top-most context on this thread's object stack.
    /// </summary>
    /// <returns>Whether or not there was anything on the stack to peek.</returns>
    public static bool TryGetCurrent([NotNullWhen(true)] out IObjectStackContext? currentContext)
    {
        EnsureLocalCreated();

        Stack<IObjectStackContext>? value = _stackLocal.Value;
        if (value == null || value.Count == 0)
        {
            currentContext = null;
            return false;
        }

        currentContext = value.Peek();
        return true;
    }
    
    internal static void Dispose()
    {
        Interlocked.Exchange(ref _stackLocal, null)?.Dispose();
    }

    /// <summary>
    /// Creates an enumerable that walks down the stack, starting at the current context and moving to the root-most context.
    /// </summary>
    public static DatObjectStackEnumerable AsEnumerable()
    {
        return DatObjectStackEnumerable.Instance;
    }

    public struct DatObjectStackEnumerable : IEnumerable<IObjectStackContext>
    {
        internal static readonly DatObjectStackEnumerable Instance = default;

        public DatObjectStackEnumerator GetEnumerator()
        {
            return new DatObjectStackEnumerator();
        }

        IEnumerator<IObjectStackContext> IEnumerable<IObjectStackContext>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct DatObjectStackEnumerator : IEnumerator<IObjectStackContext>
    {
        private readonly Stack<IObjectStackContext>? _stack;
        private Stack<IObjectStackContext>.Enumerator _enumerator;

        public int Count => _stack?.Count ?? 0;

#nullable disable

        /// <inheritdoc />
        public IObjectStackContext Current { get; private set; }

#nullable restore

        public DatObjectStackEnumerator()
        {
            EnsureLocalCreated();

            Stack<IObjectStackContext>? value = _stackLocal.Value;
            if (value == null || value.Count == 0)
            {
                return;
            }

            _stack = value;
            _enumerator = value.GetEnumerator();
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_stack == null)
                return false;

            if (!_enumerator.MoveNext())
                return false;

            Current = _enumerator.Current;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_stack == null)
                return;

            _enumerator.Dispose();
            _enumerator = _stack.GetEnumerator();
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        object? IEnumerator.Current => Current;
    }
}