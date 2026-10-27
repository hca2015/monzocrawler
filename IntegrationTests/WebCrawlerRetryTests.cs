using MonzoCrawler;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace IntegrationTests;

public class WebCrawlerRetryTests
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SlowResponseDelay = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CrawlAsync_AfterTimeouts_ReturnsLinksFromSuccessfulRetry(int timeouts)
    {
        using WireMockServer server = WireMockServer.Start();
        const string scenario = "Timeout then success";
        const string path = "/retry.html";

        server.Given(Request.Create().WithPath(path).UsingGet())
            .InScenario(scenario)
            .WillSetStateTo("attempt-1")
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("<a href='/wrong.html'>Late response</a>")
                .WithDelay(SlowResponseDelay));

        for (int attempt = 1; attempt < timeouts; attempt++)
        {
            server.Given(Request.Create().WithPath(path).UsingGet())
                .InScenario(scenario)
                .WhenStateIs($"attempt-{attempt}")
                .WillSetStateTo($"attempt-{attempt + 1}")
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody("<a href='/wrong.html'>Late response</a>")
                    .WithDelay(SlowResponseDelay));
        }

        server.Given(Request.Create().WithPath(path).UsingGet())
            .InScenario(scenario)
            .WhenStateIs($"attempt-{timeouts}")
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<a href='/Page_B.html'>Recovered</a>"));

        Uri pageUrl = new($"http://localhost:{server.Port}{path}");
        using HttpClient client = new()
        {
            BaseAddress = pageUrl,
            Timeout = RequestTimeout
        };

        WebCrawler crawler = new(new CrawlerOptions
        {
            TargetUrl = pageUrl,
            Timeout = client.Timeout
        });

        ICollection<string> result = await crawler.CrawlAsync(client).WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(new Uri(pageUrl, "/Page_B.html").AbsoluteUri, Assert.Single(result));
    }

    [Fact]
    public async Task CrawlAsync_WhenEveryAttemptTimesOut_ReturnsEmptyCollection()
    {
        using WireMockServer server = WireMockServer.Start();

        server.Given(Request.Create().WithPath("/always-slow.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<a href='/Page_B.html'>Too late</a>")
                .WithDelay(SlowResponseDelay));

        Uri pageUrl = new($"http://localhost:{server.Port}/always-slow.html");
        using HttpClient client = new()
        {
            BaseAddress = pageUrl,
            Timeout = RequestTimeout
        };

        WebCrawler crawler = new(new CrawlerOptions
        {
            TargetUrl = pageUrl,
            Timeout = client.Timeout
        });

        ICollection<string> result = await crawler.CrawlAsync(client)
            .WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Empty(result);
    }
}
