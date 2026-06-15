using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Soenneker.Extensions.Spans.ReadOnly.Chars.Html;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.PooledStringBuilders;
using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AngleSharpContextType = Soenneker.AngleSharp.Parser.Enums.AngleSharpContextType;
using AngleSharpParser = Soenneker.AngleSharp.Parser.AngleSharpParser;

namespace Soenneker.Extensions.String.Html;

/// <summary>
/// A collection of helpful HTML string extension methods.
/// </summary>
public static class HtmlStringExtension
{
    private static readonly PrettyMarkupFormatter _prettyFormatter = new();
    private static readonly AngleSharpParser _angleSharpParser = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask<HtmlParser> GetParser(CancellationToken cancellationToken)
    {
        return _angleSharpParser.Get(AngleSharpContextType.Fast, cancellationToken);
    }

    /// <summary>
    /// Converts an HTML-formatted string to its Markdown representation.
    /// </summary>
    /// <remarks>If the input string does not resemble HTML, the method returns the input unchanged. This
    /// method is designed to handle null or white-space inputs gracefully.</remarks>
    /// <param name="html">The HTML string to convert. If the value is null or consists only of white-space characters, the method returns
    /// the input unchanged.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A string containing the Markdown representation of the input HTML. Returns null if the input is null.</returns>
    [Pure]
    public static async ValueTask<string?> ToMarkdownFromHtml(this string? html,
        CancellationToken cancellationToken = default)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        // Optional fast-fail: if it doesn't even resemble HTML, skip conversion.
        if (!html.AsSpan().LooksLikeHtml())
            return html;

        HtmlParser parser = await GetParser(cancellationToken).NoSync();
        return ToMarkdownFromHtmlCore(html, parser);
    }

    private static string ToMarkdownFromHtmlCore(string html, HtmlParser parser)
    {
        IHtmlDocument document = parser.ParseDocument(html);
        var sb = new PooledStringBuilder(html.Length);

        try
        {
            AppendMarkdown(document.Body ?? document.DocumentElement, ref sb, 0);
            return NormalizeMarkdown(ref sb);
        }
        catch
        {
            sb.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Formats the specified string as HTML if it appears to be valid HTML content.
    /// </summary>
    /// <remarks>If the input string does not resemble HTML, it is returned unchanged. The method uses an HTML
    /// parser to format the content, ensuring proper HTML structure.</remarks>
    /// <param name="html">The HTML string to format. If null or whitespace, it is returned unchanged.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The formatted HTML string, or null if the input was null or whitespace.</returns>
    [Pure]
    public static async ValueTask<string?> FormatAsHtml(this string? html,
        CancellationToken cancellationToken = default)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        if (!html.AsSpan().LooksLikeHtml())
            return html;

        HtmlParser parser = await GetParser(cancellationToken).NoSync();
        IHtmlDocument document = await parser.ParseDocumentAsync(html, cancellationToken).NoSync();
        await using var sw = new StringWriter(CultureInfo.InvariantCulture);
        document.ToHtml(sw, _prettyFormatter);
        return sw.ToString();
    }

    /// <summary>
    /// Removes all HTML tags from the specified string and returns the plain text content.
    /// </summary>
    /// <remarks>If the input string does not appear to contain HTML, the method trims and returns the input
    /// as is. This method is useful for sanitizing user input or extracting readable text from HTML
    /// documents.</remarks>
    /// <param name="html">The HTML string from which to remove tags. If the value is null or consists only of white-space characters, the
    /// method returns the input unchanged.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A string containing the plain text extracted from the HTML input. Returns null if the input is null.</returns>
    [Pure]
    public static async ValueTask<string?> StripTagsFromHtml(this string? html,
        CancellationToken cancellationToken = default)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        if (!html.AsSpan().LooksLikeHtml())
            return html.Trim();

        HtmlParser parser = await GetParser(cancellationToken).NoSync();
        IHtmlDocument document = await parser.ParseDocumentAsync(html, cancellationToken).NoSync();

        // Trim() allocates only if needed.
        string? text = document.Body?.TextContent;
        return text?.Trim() ?? html.Trim();
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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A minified version of the input HTML string, or the original value if the input is null, whitespace, or does not
    /// resemble HTML.</returns>
    [Pure]
    public static async ValueTask<string?> MinifyHtml(this string? html, CancellationToken cancellationToken = default)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        if (!html.AsSpan().LooksLikeHtml())
            return html;

        HtmlParser parser = await GetParser(cancellationToken).NoSync();
        IHtmlDocument document = await parser.ParseDocumentAsync(html, cancellationToken).NoSync();
        await using var sw = new StringWriter(CultureInfo.InvariantCulture);
        document.ToHtml(sw, HtmlMarkupFormatter.Instance); // compact formatter
        return sw.ToString();
    }

    /// <summary>
    /// Determines whether the specified HTML string contains an element with the given tag name.
    /// </summary>
    /// <remarks>This method performs a preliminary check for the presence of the opening tag to improve
    /// performance. If the tag is not found, the method returns false without parsing the HTML.</remarks>
    /// <param name="html">The HTML string to search. Cannot be null or consist only of white-space characters.</param>
    /// <param name="tagName">The name of the HTML tag to search for. Cannot be null or consist only of white-space characters.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>true if an element with the specified tag name is found in the HTML string; otherwise, false.</returns>
    [Pure]
    public static async ValueTask<bool> HasHtmlElement(this string? html, string tagName,
        CancellationToken cancellationToken = default)
    {
        if (html.IsNullOrWhiteSpace() || tagName.IsNullOrWhiteSpace())
            return false;

        // Cheap pre-check to avoid parsing most of the time.
        if (!html.AsSpan().ContainsOpenTag(tagName.AsSpan()))
            return false;

        HtmlParser parser = await GetParser(cancellationToken).NoSync();
        IHtmlDocument document = await parser.ParseDocumentAsync(html, cancellationToken).NoSync();
        return document.QuerySelector(tagName) is not null;
    }

    private static void AppendMarkdown(INode node, ref PooledStringBuilder sb, int listDepth)
    {
        foreach (INode child in node.ChildNodes)
        {
            if (child is IText text)
            {
                AppendCollapsedText(ref sb, text.Data);
                continue;
            }

            if (child is not IElement element)
                continue;

            string tagName = element.TagName.ToLowerInvariant();

            switch (tagName)
            {
                case "br":
                    sb.AppendLine();
                    break;

                case "p":
                    AppendMarkdownBlock(element, ref sb, listDepth);
                    break;

                case "strong":
                case "b":
                    WrapInline(element, ref sb, listDepth, "**");
                    break;

                case "em":
                case "i":
                    WrapInline(element, ref sb, listDepth, "*");
                    break;

                case "a":
                    AppendLink(element, ref sb, listDepth);
                    break;

                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    AppendHeading(element, ref sb, listDepth, tagName[1] - '0');
                    break;

                case "ul":
                case "ol":
                    AppendList(element, ref sb, listDepth, tagName == "ol");
                    break;

                case "li":
                    AppendMarkdown(element, ref sb, listDepth);
                    break;

                case "blockquote":
                    AppendBlockQuote(element, ref sb, listDepth);
                    break;

                case "code":
                    WrapInline(element, ref sb, listDepth, "`");
                    break;

                case "pre":
                    AppendPreformatted(element, ref sb);
                    break;

                default:
                    AppendMarkdown(element, ref sb, listDepth);
                    break;
            }
        }
    }

    private static void AppendMarkdownBlock(IElement element, ref PooledStringBuilder sb, int listDepth)
    {
        EnsureBlankLine(ref sb);
        AppendMarkdown(element, ref sb, listDepth);
        EnsureBlankLine(ref sb);
    }

    private static void AppendHeading(IElement element, ref PooledStringBuilder sb, int listDepth, int level)
    {
        EnsureBlankLine(ref sb);
        sb.Append('#', level);
        sb.Append(' ');
        AppendMarkdown(element, ref sb, listDepth);
        EnsureBlankLine(ref sb);
    }

    private static void AppendList(IElement element, ref PooledStringBuilder sb, int listDepth, bool ordered)
    {
        EnsureBlankLine(ref sb);
        var index = 1;

        foreach (IElement item in element.Children)
        {
            if (!item.TagName.Equals("li", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.Append(' ', listDepth * 2);
            sb.Append(ordered ? $"{index}. " : "- ");
            AppendMarkdown(item, ref sb, listDepth + 1);
            TrimTrailingWhitespace(ref sb);
            sb.AppendLine();
            index++;
        }

        EnsureBlankLine(ref sb);
    }

    private static void AppendLink(IElement element, ref PooledStringBuilder sb, int listDepth)
    {
        string? href = element.GetAttribute("href");

        if (href.IsNullOrWhiteSpace())
        {
            AppendMarkdown(element, ref sb, listDepth);
            return;
        }

        sb.Append('[');
        AppendMarkdown(element, ref sb, listDepth);
        sb.Append("](");
        sb.Append(href);
        sb.Append(')');
    }

    private static void AppendBlockQuote(IElement element, ref PooledStringBuilder sb, int listDepth)
    {
        var inner = new PooledStringBuilder();
        string innerText;

        try
        {
            AppendMarkdown(element, ref inner, listDepth);
            innerText = inner.ToStringAndDispose().Trim();
        }
        catch
        {
            inner.Dispose();
            throw;
        }

        EnsureBlankLine(ref sb);

        string[] lines = innerText.Split('\n');
        foreach (string line in lines)
        {
            sb.Append("> ");
            sb.AppendLine(line.TrimEnd('\r'));
        }

        EnsureBlankLine(ref sb);
    }

    private static void AppendPreformatted(IElement element, ref PooledStringBuilder sb)
    {
        EnsureBlankLine(ref sb);
        sb.AppendLine("```");
        sb.AppendLine(element.TextContent.Trim());
        sb.AppendLine("```");
        EnsureBlankLine(ref sb);
    }

    private static void WrapInline(IElement element, ref PooledStringBuilder sb, int listDepth, string marker)
    {
        sb.Append(marker);
        AppendMarkdown(element, ref sb, listDepth);
        TrimTrailingWhitespace(ref sb);
        sb.Append(marker);
    }

    private static void AppendCollapsedText(ref PooledStringBuilder sb, string text)
    {
        if (text.IsNullOrWhiteSpace())
            return;

        ReadOnlySpan<char> current = sb.AsSpan();
        bool needsSpace = current.Length > 0 && !char.IsWhiteSpace(current[^1]) &&
                          !IsMarkdownOpeningPunctuation(current[^1]);
        string collapsed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (needsSpace && !IsMarkdownClosingPunctuation(collapsed[0]))
            sb.Append(' ');

        sb.Append(collapsed);
    }

    private static string NormalizeMarkdown(ref PooledStringBuilder sb)
    {
        TrimTrailingWhitespace(ref sb);

        string result = sb.ToStringAndDispose();
        return result.Trim();
    }

    private static void EnsureBlankLine(ref PooledStringBuilder sb)
    {
        TrimTrailingWhitespace(ref sb);

        if (sb.Length == 0)
            return;

        ReadOnlySpan<char> current = sb.AsSpan();

        if (current.Length >= 2 && current[^1] == '\n' && current[^2] == '\n')
            return;

        if (current[^1] != '\n')
            sb.AppendLine();

        sb.AppendLine();
    }

    private static void TrimTrailingWhitespace(ref PooledStringBuilder sb)
    {
        ReadOnlySpan<char> current = sb.AsSpan();
        var count = 0;

        for (int i = current.Length - 1; i >= 0 && char.IsWhiteSpace(current[i]) && current[i] != '\n'; i--)
        {
            count++;
        }

        if (count > 0)
            sb.Shrink(count);
    }

    private static bool IsMarkdownOpeningPunctuation(char value)
    {
        return value is '[' or '(' or '`' or '*';
    }

    private static bool IsMarkdownClosingPunctuation(char value)
    {
        return value is '.' or ',' or ';' or ':' or '!' or '?' or ')' or ']';
    }
}