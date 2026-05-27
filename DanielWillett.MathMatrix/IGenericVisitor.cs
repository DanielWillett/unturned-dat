using System;

namespace DanielWillett.UnturnedDataFileLspServer.Data.Utility;

/// <summary>
/// Visitor that accepts a generic equatable value.
/// </summary>
public interface IGenericVisitor
{
    /// <summary>
    /// Usually called by a function to return a strongly typed value.
    /// <para>For example, the following usage would be appropriate:</para>
    /// <code language="cs">
    /// void CastToNumber&lt;TVisitor&gt;(string str, ref TVisitor visitor)
    ///     where TVisitor : IGenericVisitor, allows ref struct
    /// {
    ///     if (long.TryParse(str, out long l))
    ///     {
    ///         visitor.Accept&lt;long&gt;(l);
    ///     }
    ///     else if (double.TryParse(str, out double d))
    ///     {
    ///         visitor.Accept&lt;double&gt;(d);
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type of value to return.</typeparam>
    /// <param name="value">Value to accept of the specified type.</param>
    void Accept<T>(T? value) where T : IEquatable<T>;
}