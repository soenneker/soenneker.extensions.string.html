using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Soenneker.Extensions.Spans.ReadOnly.Chars.Html;

namespace Soenneker.Extensions.String.Html;

/// <summary>
/// A collection of helpful HTML string extension methods.
/// </summary>
public static class HtmlStringExtension
{
    // Parsing HTML strings does not require a loader; keep the config lean.
    private static readonly IBrowsingContext _context = BrowsingContext.New(Configuration.Default);

    private static readonly PrettyMarkupFormatter _prettyFormatter = new();

    // Treat these as not thread-safe; keep them per-thread with no ThreadLocal overhead.
    [ThreadStatic] private static IHtmlParser? _parser;
    [ThreadStatic] private static ReverseMarkdown.Converter? _mdConverter;

    // Reuse a per-thread StringBuilder to avoid repeated allocations in ToHtml(sw,...).
    [ThreadStatic] private static StringBuilder? _sb;

    private static IHtmlParser Parser
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _parser ??= new HtmlParser(new HtmlParserOptions(), _context);
    }

    private static ReverseMarkdown.Converter MarkdownConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _mdConverter ??= new ReverseMarkdown.Converter();
    }

    private static StringBuilder AcquireStringBuilder(int initialCapacity = 1024)
    {
        // Keep a modest cap to avoid holding giant buffers forever on a hot thread.
        const int maxRetainedCapacity = 64 * 1024;

        StringBuilder? sb = _sb;
        if (sb is null)
        {
            sb = new StringBuilder(initialCapacity);
            _sb = sb;
            return sb;
        }

        if (sb.Capacity > maxRetainedCapacity)
            sb = _sb = new StringBuilder(initialCapacity);
        else
            sb.Clear();

        return sb;
    }

    /// <summary>
    /// Converts an HTML-formatted string to its Markdown representation.
    /// </summary>
    /// <remarks>If the input string does not resemble HTML, the method returns the input unchanged. This
    /// method is designed to handle null or white-space inputs gracefully.</remarks>
    /// <param name="html">The HTML string to convert. If the value is null or consists only of white-space characters, the method returns
    /// the input unchanged.</param>
    /// <returns>A string containing the Markdown representation of the input HTML. Returns null if the input is null.</returns>
    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static string? ToMarkdownFromHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        // Optional fast-fail: if it doesn't even resemble HTML, skip conversion.
        if (!html.AsSpan().LooksLikeHtml())
            return html;

        return MarkdownConverter.Convert(html);
    }

    /// <summary>
    /// Formats the specified string as HTML if it appears to be valid HTML content.
    /// </summary>
    /// <remarks>If the input string does not resemble HTML, it is returned unchanged. The method uses an HTML
    /// parser to format the content, ensuring proper HTML structure.</remarks>
    /// <param name="html">The HTML string to format. If null or whitespace, it is returned unchanged.</param>
    /// <returns>The formatted HTML string, or null if the input was null or whitespace.</returns>
    [Pure]
    public static string? FormatAsHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        if (!html.AsSpan().LooksLikeHtml())
            return html;

        IHtmlDocument document = Parser.ParseDocument(html);

        StringBuilder sb = AcquireStringBuilder(html.Length + 32);
        using (var sw = new System.IO.StringWriter(sb, CultureInfo.InvariantCulture))
        {
            document.ToHtml(sw, _prettyFormatter);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Removes all HTML tags from the specified string and returns the plain text content.
    /// </summary>
    /// <remarks>If the input string does not appear to contain HTML, the method trims and returns the input
    /// as is. This method is useful for sanitizing user input or extracting readable text from HTML
    /// documents.</remarks>
    /// <param name="html">The HTML string from which to remove tags. If the value is null or consists only of white-space characters, the
    /// method returns the input unchanged.</param>
    /// <returns>A string containing the plain text extracted from the HTML input. Returns null if the input is null.</returns>
    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static string? StripTagsFromHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        if (!html.AsSpan().LooksLikeHtml())
            return html.Trim();

        IHtmlDocument document = Parser.ParseDocument(html);

        // Trim() allocates only if needed.
        string? text = document.Body?.TextContent;
        return text?.Trim();
    }

    /// <summary>
    /// Determines whether the specified string contains HTML content.
    /// </summary>
    /// <remarks>This method first checks if the input string is null or empty before evaluating its content.
    /// It uses a span-based approach to efficiently determine if the string appears to contain HTML.</remarks>
    /// <param name="html">The string to evaluate for HTML content. This parameter can be null or empty, in which case the method returns
    /// <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if the string contains HTML content; otherwise, <see langword="false"/>.</returns>
    [Pure]
    public static bool ContainsHtml(this string? html)
    {
        if (html.IsNullOrEmpty())
            return false;

        return html.AsSpan().LooksLikeHtml();
    }

    /// <summary>
    /// Minifies the specified HTML string by removing unnecessary whitespace and formatting, producing a more compact
    /// representation.
    /// </summary>
    /// <remarks>The method first checks whether the input string appears to be HTML before attempting to
    /// minify it. If the input does not resemble HTML, it is returned unchanged. This method does not validate or
    /// correct malformed HTML.</remarks>
    /// <param name="html">The HTML string to be minified. If the value is null or consists only of whitespace, the original value is
    /// returned.</param>
    /// <returns>A minified version of the input HTML string, or the original value if the input is null, whitespace, or does not
    /// resemble HTML.</returns>
    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static string? MinifyHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        if (!html.AsSpan().LooksLikeHtml())
            return html;

        IHtmlDocument document = Parser.ParseDocument(html);

        StringBuilder sb = AcquireStringBuilder(html.Length);
        using (var sw = new System.IO.StringWriter(sb, CultureInfo.InvariantCulture))
        {
            document.ToHtml(sw, HtmlMarkupFormatter.Instance); // compact formatter
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines whether the specified HTML string contains an element with the given tag name.
    /// </summary>
    /// <remarks>This method performs a preliminary check for the presence of the opening tag to improve
    /// performance. If the tag is not found, the method returns false without parsing the HTML.</remarks>
    /// <param name="html">The HTML string to search. Cannot be null or consist only of white-space characters.</param>
    /// <param name="tagName">The name of the HTML tag to search for. Cannot be null or consist only of white-space characters.</param>
    /// <returns>true if an element with the specified tag name is found in the HTML string; otherwise, false.</returns>
    [Pure]
    public static bool HasHtmlElement(this string? html, string tagName)
    {
        if (html.IsNullOrWhiteSpace() || tagName.IsNullOrWhiteSpace())
            return false;

        // Cheap pre-check to avoid parsing most of the time.
        if (!html.AsSpan().ContainsOpenTag( tagName.AsSpan()))
            return false;

        IHtmlDocument document = Parser.ParseDocument(html);
        return document.QuerySelector(tagName) is not null;
    }
}
