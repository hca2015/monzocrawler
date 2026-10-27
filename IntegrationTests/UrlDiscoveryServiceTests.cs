using MonzoCrawler;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace IntegrationTests;

[Collection("Crawler site")]
public class UrlDiscoveryServiceTests
{
    private readonly CrawlerSiteFixture fixture;

    public UrlDiscoveryServiceTests(CrawlerSiteFixture fixture)
    {
        this.fixture = fixture;
        fixture.Server.ResetLogEntries();
    }

    [Theory]
    [InlineData("")]
    [InlineData("#top")]
    public async Task DiscoverUrls_WhenStartingPageLinksToItself_RequestsItOnlyOnce(string fragment)
    {
        fixture.Server.Given(Request.Create().WithPath("/self.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<a href='/self.html'>This page</a>"));

        Uri pageUrl = new($"{CrawlerSiteFixture.BaseUrl}/self.html{fragment}");
        using HttpClient client = new() { BaseAddress = pageUrl };

        UrlDiscoveryService service = new(new CrawlerOptions
        {
            TargetUrl = pageUrl,
            Concurrency = 2
        });

        HashSet<string> result = await service.DiscoverUrls(client)
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal($"{CrawlerSiteFixture.BaseUrl}/self.html", Assert.Single(result));
        Assert.Single(fixture.Server.FindLogEntries(Request.Create().WithPath("/self.html").UsingGet()));
    }


    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task DiscoverUrls_TraversesSiteAndRequestsEachPageOnlyOnce(int concurrency)
    {
        Uri pageUrl = new($"{CrawlerSiteFixture.BaseUrl}/Page_A.html");
        using HttpClient client = new()
        {
            BaseAddress = pageUrl,
            Timeout = TimeSpan.FromSeconds(5)
        };

        UrlDiscoveryService service = new(new CrawlerOptions
        {
            TargetUrl = pageUrl,
            Concurrency = concurrency,
            Timeout = client.Timeout
        });

        HashSet<string> result = await service.DiscoverUrls(client)
            .WaitAsync(TimeSpan.FromSeconds(10));

        string[] expected = new[] { "A", "B", "C", "D" }
            .Select(page => $"{CrawlerSiteFixture.BaseUrl}/Page_{page}.html")
            .ToArray();

        Assert.Equal(expected.Order(), result.Order());

        foreach (string page in new[] { "A", "B", "C", "D" })
        {
            Assert.Single(fixture.Server.FindLogEntries(Request.Create().WithPath($"/Page_{page}.html").UsingGet()));
        }
    }

    [Fact]
    public async Task DiscoverUrls_WaitsForRunningPagesThatDiscoverMoreLinks()
    {
        fixture.Server.Given(Request.Create().WithPath("/branch-start.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("""
                    <a href="/left.html">Left</a>
                    <a href="/right.html">Right</a>
                    """));

        fixture.Server.Given(Request.Create().WithPath("/left.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<p>No links.</p>"));

        fixture.Server.Given(Request.Create().WithPath("/right.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<a href='/last.html'>Last page</a>")
                .WithDelay(TimeSpan.FromMilliseconds(250)));

        fixture.Server.Given(Request.Create().WithPath("/last.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<p>Finished.</p>"));

        Uri pageUrl = new($"{CrawlerSiteFixture.BaseUrl}/branch-start.html");
        using HttpClient client = new()
        {
            BaseAddress = pageUrl,
            Timeout = TimeSpan.FromSeconds(5)
        };

        UrlDiscoveryService service = new(new CrawlerOptions
        {
            TargetUrl = pageUrl,
            Concurrency = 2,
            Timeout = client.Timeout
        });

        HashSet<string> result = await service.DiscoverUrls(client)
            .WaitAsync(TimeSpan.FromSeconds(10));

        string[] expectedPaths = ["branch-start.html", "left.html", "right.html", "last.html"];

        IEnumerable<string> expected = expectedPaths.Select(path => $"{CrawlerSiteFixture.BaseUrl}/{path}");
        Assert.Equal(expected.Order(), result.Order());
    }
}
