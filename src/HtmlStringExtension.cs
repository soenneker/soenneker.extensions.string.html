using System;
using System.Diagnostics.Contracts;

namespace Soenneker.Extensions.String.Html;

/// <summary>
/// A collection of helpful html string extension methods
/// </summary>
public static class HtmlStringExtension
{
    private static readonly Lazy<ReverseMarkdown.Converter> _converter = new(() => new ReverseMarkdown.Converter());

    /// <summary>
    /// Converts an HTML string to a Markdown string.
    /// </summary>
    /// <param name="html">The HTML string to convert.</param>
    /// <returns>The Markdown representation of the HTML string. If the input is null or whitespace, the original input is returned.</returns>
    /// <remarks>
    /// This method uses the ReverseMarkdown library to perform the conversion. The converter instance is lazily initialized and cached for reuse.
    /// </remarks>
    [Pure]
    public static string ToMarkdown(this string html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        string result = _converter.Value.Convert(html);
        return result;
    }
}