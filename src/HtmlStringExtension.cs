using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Extensions.String.Html;

/// <summary>
/// A collection of helpful HTML string extension methods.
/// </summary>
public static class HtmlStringExtension
{
    // Parsing HTML strings does not require a loader; keep the config lean.
    private static readonly Lazy<IBrowsingContext> _context = new(() => BrowsingContext.New(Configuration.Default), isThreadSafe: true);

    private static readonly Lazy<PrettyMarkupFormatter> _prettyFormatter = new(() => new PrettyMarkupFormatter(), isThreadSafe: true);

    // AngleSharp's HtmlParser and ReverseMarkdown.Converter are safer treated as not thread-safe.
    // ThreadLocal avoids locks and avoids cross-thread shared mutable state.
    private static readonly ThreadLocal<IHtmlParser> _parser = new(() => new HtmlParser(new HtmlParserOptions(), _context.Value), trackAllValues: false);

    private static readonly ThreadLocal<ReverseMarkdown.Converter> _mdConverter = new(() => new ReverseMarkdown.Converter(), trackAllValues: false);

    private static IHtmlParser Parser => _parser.Value!;
    private static ReverseMarkdown.Converter MarkdownConverter => _mdConverter.Value!;

    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static string? ToMarkdownFromHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return html;

        // Optional fast-fail: if it doesn't even resemble HTML, skip conversion.
        // (Remove this if you want markdown conversion for non-HTML input too.)
        if (!LooksLikeHtml(html))
            return html;

        return MarkdownConverter.Convert(html);
    }

    [Pure]
    public static ValueTask<string?> FormatAsHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return new ValueTask<string?>(html);

        if (!LooksLikeHtml(html))
            return new ValueTask<string?>(html);

        IHtmlDocument document = Parser.ParseDocument(html);

        using var sw = new StringWriter();
        document.ToHtml(sw, _prettyFormatter.Value);
        return new ValueTask<string?>(sw.ToString());
    }

    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static ValueTask<string?> StripTagsFromHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return new ValueTask<string?>(html);

        if (!LooksLikeHtml(html))
            return new ValueTask<string?>(html.Trim());

        IHtmlDocument document = Parser.ParseDocument(html);

        // Avoid null-prop chains allocations; Trim() creates a new string only if needed.
        string? text = document.Body?.TextContent;
        if (text is null)
            return new ValueTask<string?>((string?)null);

        return new ValueTask<string?>(text.Trim());
    }

    [Pure]
    public static bool ContainsHtml(this string? html)
    {
        if (html.IsNullOrEmpty())
            return false;

        return LooksLikeHtml(html);
    }

    [Pure]
    [return: NotNullIfNotNull(nameof(html))]
    public static ValueTask<string?> MinifyHtml(this string? html)
    {
        if (html.IsNullOrWhiteSpace())
            return new ValueTask<string?>(html);

        if (!LooksLikeHtml(html))
            return new ValueTask<string?>(html);

        IHtmlDocument document = Parser.ParseDocument(html);

        using var sw = new StringWriter();
        document.ToHtml(sw, HtmlMarkupFormatter.Instance); // compact formatter
        return new ValueTask<string?>(sw.ToString());
    }

    [Pure]
    public static ValueTask<bool> HasHtmlElement(this string? html, string tagName)
    {
        if (html.IsNullOrWhiteSpace() || tagName.IsNullOrWhiteSpace())
            return new ValueTask<bool>(false);

        // Cheap pre-check to avoid parsing most of the time.
        // Handles "<tag", "<tag>", "<tag ", "<tag\n", etc.
        if (!ContainsOpenTag(html, tagName))
            return new ValueTask<bool>(false);

        IHtmlDocument document = Parser.ParseDocument(html);
        return new ValueTask<bool>(document.QuerySelector(tagName) is not null);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LooksLikeHtml(string s)
    {
        // Fast-ish heuristic: <...> and a closing tag marker.
        // Keep it cheap and non-allocating.
        int lt = s.IndexOf('<');
        if (lt < 0)
            return false;

        int gt = s.IndexOf('>', lt + 1);
        if (gt < 0)
            return false;

        return s.IndexOf("</", lt + 1, StringComparison.Ordinal) >= 0;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsOpenTag(string html, string tagName)
    {
        // Case-insensitive search for "<tag"
        // (AngleSharp itself is HTML5/case-insensitive for tag names; this matches that intent.)
        ReadOnlySpan<char> h = html.AsSpan();
        ReadOnlySpan<char> t = tagName.AsSpan();

        // Build pattern "<" + tagName without allocating.
        for (int i = 0; i < h.Length; i++)
        {
            if (h[i] != '<')
                continue;

            int start = i + 1;
            if (start + t.Length > h.Length)
                return false;

            if (h.Slice(start, t.Length)
                 .Equals(t, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}