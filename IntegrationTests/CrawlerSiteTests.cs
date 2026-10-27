using System.Net;

namespace IntegrationTests;

[Collection("Crawler site")]
public class CrawlerSiteTests
{
    public CrawlerSiteTests(CrawlerSiteFixture fixture)
    {
        _ = fixture;
    }

    [Theory]
    [InlineData("A", "Page_B.html")]
    [InlineData("B", "Page_C.html")]
    [InlineData("C", "Page_D.html")]
    [InlineData("D", "Page_A.html")]
    public async Task ServesHtmlPageWithExpectedLink(string page, string expectedLink)
    {
        using HttpClient client = new();
        using HttpResponseMessage response = await client.GetAsync($"{CrawlerSiteFixture.BaseUrl}/Page_{page}.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedLink, html);
    }
}

