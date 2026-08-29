[![](https://img.shields.io/nuget/v/soenneker.extensions.string.html.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.string.html/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.string.html/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.string.html/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.extensions.string.html.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.string.html/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.string.html/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.string.html/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.String.Html
A collection of helpful html string extension methods.

## Installation

```bash
dotnet add package Soenneker.Extensions.String.Html
```

## Quick start

```csharp
using Soenneker.Extensions.String.Html;

// Given an existing string? named html:
var result = html.ToMarkdownFromHtml();
```

## Common operations

- `ToMarkdownFromHtml()` - Converts an HTML-formatted string to its Markdown representation. Returns a string containing the Markdown representation of the input HTML. Returns null if the input is null. If the input string does not resemble HTML, the method returns the input unchanged.
- `FormatAsHtml()` - Formats the specified string as HTML if it appears to be valid HTML content. Returns the formatted HTML string, or null if the input was null or whitespace. If the input string does not resemble HTML, it is returned unchanged.
- `StripTagsFromHtml()` - Removes all HTML tags from the specified string and returns the plain text content. If the input string does not appear to contain HTML, the method trims and returns the input as is.
- `ContainsHtml()` - Determines whether the specified string contains HTML content.
- `MinifyHtml()` - Minifies the specified HTML string by removing unnecessary whitespace and formatting, producing a more compact representation. Returns a minified version of the input HTML string, or the original value if the input is null, whitespace, or does not resemble HTML.
- `HasHtmlElement()` - Determines whether the specified HTML string contains an element with the given tag name.
