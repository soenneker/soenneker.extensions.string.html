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
/// A collection of helpful html string extension methods
/// </summary>
public static class HtmlStringExtension
{
    private static readonly Lazy<ReverseMarkdown.Converter> _converter = new(() => new ReverseMarkdown.Converter(), true);
    private static readonly Lazy<IBrowsingContext> _lazyContext = new(() =>
        BrowsingContext.New(Configuration.Default.WithDefaultLoader()), true);

    private static readonly Lazy<PrettyMarkupFormatter> _lazyFormatter = new(() =>
        new PrettyMarkupFormatter(), true);

    /// <summary>
    /// Converts an HTML string to a Markdown string.
    /// </summary>
    /// <param name="html">The HTML string to convert.</param>
    /// <returns>The Markdown representation of the HTML string. If the input is null or whitespace, the original input is returned.</returns>
    /// <remarks>
    /// This method uses the ReverseMarkdown library to perform the conversion. The converter instance is lazily initialized and cached for reuse.
    /// </remarks>
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
    /// <remarks>
    /// This method uses AngleSharp to parse and serialize the HTML in a normalized, indented format for improved readability.
    /// </remarks>
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
}