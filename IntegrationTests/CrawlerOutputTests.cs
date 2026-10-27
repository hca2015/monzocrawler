using MonzoCrawler;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace IntegrationTests;

[Collection("Crawler site")]
public class CrawlerOutputTests(CrawlerSiteFixture fixture)
{
    [Fact]
    public async Task Main_PrintsEachPageWithItsLinks_IncludingExternalLinks()
    {
        fixture.Server.Given(Request.Create().WithPath("/output-start.html").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "text/html")
                .WithBody("""
                    <a href="#top">This page</a>
                    <a href="/Page_A.html">A</a>
                    <a href="/Page_B.html">B</a>
                    <a href="/Page_C.html">C</a>
                    <a href="/Page_D.html">D</a>
                    """));

        using StringWriter standardOutput = new();
        TextWriter originalOutput = Console.Out;

        try
        {
            Console.SetOut(standardOutput);
            await Program.Main(
            [
                "--url", $"{CrawlerSiteFixture.BaseUrl}/output-start.html#top",
                "--concurrency", "4",
                "--trace", "false",
                "--timeout", "5"
            ]);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        string output = standardOutput.ToString();

        foreach (string page in new[] { "output-start", "Page_A", "Page_B", "Page_C", "Page_D" })
        {
            Assert.Contains($"Page {CrawlerSiteFixture.BaseUrl}/{page}.html", output);
        }

        Assert.Contains($"\t{CrawlerSiteFixture.BaseUrl}/Page_B.html", output);
        Assert.Contains("\thttps://www.facebook.com/monzobank/", output);
        Assert.Contains("Discovered 5 unique urls", output);
    }
}
