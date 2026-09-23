using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnturnedDat.Data.Properties;

namespace UnturnedDat.Data.Values;

/// <summary>
/// Keeps track of a stack of values keeping track of the current object's legacy state, i.e. if it's a modern { }/[ ] object or a legacy underscore object.
/// </summary>
internal static class LegacyStateStack
{
    private static ThreadLocal<Stack<PropertyResolutionContext>>? _stackLocal;

    [MemberNotNull(nameof(_stackLocal))]
    private static void EnsureLocalCreated()
    {
        if (_stackLocal != null)
            return;

        ThreadLocal<Stack<PropertyResolutionContext>> stackLocale = new ThreadLocal<Stack<PropertyResolutionContext>>();
        ThreadLocal<Stack<PropertyResolutionContext>>? old = Interlocked.CompareExchange(ref _stackLocal, stackLocale, null);
        if (old == null)
            return;

        stackLocale.Dispose();
    }

    /// <summary>
    /// Push a new state to this thread's legacy state stack.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/> is not <see cref="PropertyResolutionContext.Legacy"/> or <see cref="PropertyResolutionContext.Modern"/>.</exception>
    public static void Push(PropertyResolutionContext state)
    {
        if (state is not PropertyResolutionContext.Legacy and not PropertyResolutionContext.Modern)
            throw new ArgumentOutOfRangeException(nameof(state));

        EnsureLocalCreated();

        Stack<PropertyResolutionContext>? value = _stackLocal.Value;
        if (value == null)
        {
            value = new Stack<PropertyResolutionContext>(8);
            _stackLocal.Value = value;
        }

        value.Push(state);
    }

    /// <summary>
    /// Remove the latest legacy state from this thread's legacy state stack.
    /// </summary>
    /// <returns>Whether or not there was anything on the stack to pop.</returns>
    public static bool Pop()
    {
        EnsureLocalCreated();

        Stack<PropertyResolutionContext>? value = _stackLocal.Value;
        if (value == null || value.Count == 0)
        {
            return false;
        }

        value.Pop();
        return true;
    }

    /// <summary>
    /// Get the top-most legacy state on this thread's legacy state stack.
    /// </summary>
    /// <returns>Whether or not there was anything on the stack to peek.</returns>
    public static bool TryGetCurrent(out PropertyResolutionContext currentState)
    {
        EnsureLocalCreated();

        Stack<PropertyResolutionContext>? value = _stackLocal.Value;
        if (value == null || value.Count == 0)
        {
            currentState = PropertyResolutionContext.Unknown;
            return false;
        }

        currentState = value.Peek();
        return true;
    }
    
    internal static void Dispose()
    {
        Interlocked.Exchange(ref _stackLocal, null)?.Dispose();
    }
}