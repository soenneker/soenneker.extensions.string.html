using Soenneker.Tests.FixturedUnit;
using Xunit;
using Xunit.Abstractions;

namespace Soenneker.Extensions.String.Html.Tests;

[Collection("Collection")]
public class HtmlStringExtensionTests : FixturedUnitTest
{
    public HtmlStringExtensionTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }
}