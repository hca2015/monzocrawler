using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace IntegrationTests;

public sealed class CrawlerSiteFixture : IDisposable
{
    public const string BaseUrl = "http://localhost:9999";

    public WireMockServer Server { get; }

    public CrawlerSiteFixture()
    {
        Server = WireMockServer.Start(9999);

        foreach (string page in new[] { "A", "B", "C", "D" })
        {
            string fileName = $"Page_{page}.html";
            string html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, fileName));

            Server.Given(Request.Create().WithPath($"/{fileName}").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/html; charset=utf-8")
                    .WithBody(html));
        }
    }

    public void Dispose()
    {
        Server.Dispose();
    }
}
