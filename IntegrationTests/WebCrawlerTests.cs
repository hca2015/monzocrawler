using MonzoCrawler;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace IntegrationTests;

[Collection("Crawler site")]
public class WebCrawlerTests
{
    private readonly CrawlerSiteFixture fixture;

    public WebCrawlerTests(CrawlerSiteFixture fixture)
    {
        this.fixture = fixture;
    }

    [Theory]
    [InlineData("A", new[] { "B" })]
    [InlineData("B", new[] { "C" })]
    [InlineData("C", new[] { "D" })]
    [InlineData("D", new[] { "A", "C" })]
    public async Task CrawlAsync_ReturnsOnlyUniqueAbsoluteInternalLinks(
        string page, string[] expectedPages)
    {
        Uri pageUrl = new($"{CrawlerSiteFixture.BaseUrl}/Page_{page}.html");
        using HttpClient client = new() { BaseAddress = pageUrl };
        WebCrawler crawler = new(new CrawlerOptions { TargetUrl = pageUrl });

        ICollection<string> result = await crawler.CrawlAsync(client);

        string[] expected = expectedPages
            .Select(p => $"{CrawlerSiteFixture.BaseUrl}/Page_{p}.html")
            .ToArray();

        Assert.Equal(expected.Order(), result.Order());
    }

    [Fact]
    public async Task CrawlAsync_ResolvesLinksAgainstCurrentPageInsteadOfInitialPage()
    {
        fixture.Server.Given(Request.Create().WithPath("/nested/start.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("""
                    <a href="child.html">Child</a>
                    <a href="../Page_B.html">Parent directory</a>
                    <a href="/Page_C.html">Root directory</a>
                    """));

        Uri initialUrl = new($"{CrawlerSiteFixture.BaseUrl}/Page_A.html");
        using HttpClient client = new() { BaseAddress = initialUrl };
        WebCrawler crawler = new(new CrawlerOptions { TargetUrl = initialUrl });

        ICollection<string> result = await crawler.CrawlAsync(client,
            $"{CrawlerSiteFixture.BaseUrl}/nested/start.html");

        string[] expected =
        [
            $"{CrawlerSiteFixture.BaseUrl}/nested/child.html",
            $"{CrawlerSiteFixture.BaseUrl}/Page_B.html",
            $"{CrawlerSiteFixture.BaseUrl}/Page_C.html"
        ];

        Assert.Equal(expected.Order(), result.Order());
    }

    [Fact]
    public async Task CrawlAsync_WhenPageHasNoAnchors_ReturnsEmptyCollection()
    {
        fixture.Server.Given(Request.Create().WithPath("/empty.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<html><body><p>No links here.</p></body></html>"));

        Uri pageUrl = new($"{CrawlerSiteFixture.BaseUrl}/empty.html");
        using HttpClient client = new() { BaseAddress = pageUrl };
        WebCrawler crawler = new(new CrawlerOptions { TargetUrl = pageUrl });

        ICollection<string> result = await crawler.CrawlAsync(client);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CrawlAsync_WhenPageReturns404_ReturnsEmptyCollection()
    {
        fixture.Server.Given(Request.Create().WithPath("/missing.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        Uri pageUrl = new($"{CrawlerSiteFixture.BaseUrl}/missing.html");
        using HttpClient client = new() { BaseAddress = pageUrl };
        WebCrawler crawler = new(new CrawlerOptions { TargetUrl = pageUrl });

        ICollection<string> result = await crawler.CrawlAsync(client);

        Assert.Empty(result);
    }
}
