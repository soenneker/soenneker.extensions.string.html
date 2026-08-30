[![](https://img.shields.io/nuget/v/soenneker.extensions.string.html.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.string.html/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.string.html/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.string.html/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.extensions.string.html.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.string.html/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.string.html/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.string.html/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.String.Html
Parse HTML strings to format or minify markup, extract text, convert common elements to Markdown, and find elements.

## Installation

```bash
dotnet add package Soenneker.Extensions.String.Html
```

## Convert HTML to Markdown

```csharp
using Soenneker.Extensions.String.Html;

string html = "<h2>Status</h2><p>Build <strong>passed</strong>.</p>";
string? markdown = await html.ToMarkdownFromHtml();
```

The converter handles paragraphs, headings, emphasis, links, ordered and unordered lists, block quotes, inline code, preformatted blocks, and line breaks. Other elements contribute their child content. Whitespace is normalized, so this is intended for readable Markdown rather than lossless round-tripping.

Link destinations are copied as written. The method is not an HTML sanitizer or a Markdown-security boundary; sanitize untrusted markup and validate links before rendering the result as active content.

## Format or minify markup

```csharp
string? formatted = await html.FormatAsHtml();
string? compact = await html.MinifyHtml();
```

Both methods parse with AngleSharp and serialize the resulting document. Parsing can repair malformed markup and can add document structure such as `html`, `head`, and `body`; the output is not guaranteed to preserve the input byte-for-byte. Formatting changes layout, while minification uses AngleSharp's compact formatter.

Null and whitespace inputs are returned unchanged. Text that does not pass the package's lightweight HTML sniff is also returned unchanged.

## Extract text

```csharp
string? text = await "<p>Hello <b>world</b></p>".StripTagsFromHtml();
// "Hello world"
```

`StripTagsFromHtml()` returns the parsed body element's trimmed `TextContent`. This removes markup but is not sanitization, and text inside elements such as `script` or `style` may still be present. For non-HTML input it returns `Trim()` of the original text.

## Detect content and elements

```csharp
bool containsMarkup = html.ContainsHtml();
bool hasHeading = await html.HasHtmlElement("h2");
```

`ContainsHtml()` is a syntax sniff, not validation. `HasHtmlElement()` first performs an allocation-free opening-tag check, then parses the document and compares actual element names case-insensitively. It returns `false` for null/whitespace HTML or tag names.
