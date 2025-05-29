using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Soenneker.Extensions.Task;

namespace Soenneker.Extensions.String.Html;

/// <summary>
/// A collection of helpful HTML string extension methods.
/// </summary>
public static class HtmlStringExtension
{
    private static readonly Lazy<ReverseMarkdown.Converter> _converter = new(() => new ReverseMarkdown.Converter(), true);

    private static readonly Lazy<IBrowsingContext> _lazyContext = new(() => BrowsingContext.New(Configuration.Default.WithDefaultLoader()), true);

    private static readonly Lazy<PrettyMarkupFormatter> _lazyFormatter = new(() => new PrettyMarkupFormatter(), true);

    /// <summary>
    /// Converts an HTML string to a Markdown string.
    /// </summary>
    /// <param name="html">The HTML string to convert.</param>
    /// <returns>The Markdown representation of the HTML string. If the input is null or whitespace, the original input is returned.</returns>
    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static string? ToMarkdown(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        return _converter.Value.Convert(html);
    }

    /// <summary>
    /// Formats the specified HTML string with indentation and consistent structure using a pretty markup formatter.
    /// </summary>
    /// <param name="html">The HTML string to format. If <c>null</c> or whitespace, the original value is returned.</param>
    /// <returns>
    /// A <see cref="string"/> containing the formatted HTML, or <c>null</c> if <paramref name="html"/> is <c>null</c>.
    /// </returns>
    [Pure]
    public static async ValueTask<string?> Format(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        IBrowsingContext context = _lazyContext.Value;
        var parser = context.GetService<IHtmlParser>();
        IHtmlDocument document = await parser.ParseDocumentAsync(html).NoSync();

        await using var sw = new StringWriter();
        document.ToHtml(sw, _lazyFormatter.Value);
        return sw.ToString();
    }

    /// <summary>
    /// Strips all HTML tags and returns only the inner text.
    /// </summary>
    /// <param name="html">The HTML string to strip.</param>
    /// <returns>The plain text content, or <c>null</c> if <paramref name="html"/> is <c>null</c>.</returns>
    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static async ValueTask<string?> StripTags(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        IBrowsingContext context = _lazyContext.Value;
        var parser = context.GetService<IHtmlParser>();
        IHtmlDocument document = await parser.ParseDocumentAsync(html).NoSync();

        return document.Body?.TextContent?.Trim();
    }

    /// <summary>
    /// Determines whether the string contains HTML tags.
    /// </summary>
    /// <param name="html">The string to check.</param>
    /// <returns><c>true</c> if the string contains HTML; otherwise, <c>false</c>.</returns>
    [Pure]
    public static bool ContainsHtml(this string? html)
    {
        if (html.IsNullOrEmpty())
            return false;

        return html.Contains('<') && html.Contains('>') && html.IndexOf("</", StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// Minifies the HTML by removing unnecessary whitespace and newlines.
    /// </summary>
    /// <param name="html">The HTML string to minify.</param>
    /// <returns>The minified HTML string, or <c>null</c> if <paramref name="html"/> is <c>null</c>.</returns>
    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static async ValueTask<string?> Minify(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        IBrowsingContext context = _lazyContext.Value;
        var parser = context.GetService<IHtmlParser>();
        IHtmlDocument document = await parser.ParseDocumentAsync(html).NoSync();

        await using var sw = new StringWriter();
        document.ToHtml(sw, HtmlMarkupFormatter.Instance); // Compact formatter
        return sw.ToString();
    }

    /// <summary>
    /// Checks whether the HTML contains an element with the given tag name.
    /// </summary>
    /// <param name="html">The HTML string.</param>
    /// <param name="tagName">The tag name to search for (e.g., "img", "script").</param>
    /// <returns><c>true</c> if an element with the given tag name exists; otherwise, <c>false</c>.</returns>
    [Pure]
    public static async ValueTask<bool> HasElement(this string? html, string tagName)
    {
        if (html.IsNullOrWhiteSpace())
            return false;

        IBrowsingContext context = _lazyContext.Value;
        var parser = context.GetService<IHtmlParser>();
        IHtmlDocument document = await parser.ParseDocumentAsync(html).NoSync();

        return document.QuerySelector(tagName) != null;
    }
}