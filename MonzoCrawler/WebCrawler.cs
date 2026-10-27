using HtmlAgilityPack;
using System.Text;

namespace MonzoCrawler
{
    public class WebCrawler
    {
        private readonly CrawlerOptions crawlerOptions;

        public WebCrawler(CrawlerOptions crawlerOptions)
        {
            this.crawlerOptions = crawlerOptions;
        }

        public async Task<ICollection<string>> CrawlAsync(HttpClient client, string url = null)
        {
            HashSet<string> discoveredUrls = [];
            Uri currentPage = new Uri(client.BaseAddress, url ?? "").NormalizeUrl();

            int attempt = 0;
            string html = null;
            do
            {
                try
                {
                    using HttpResponseMessage response = await client.GetAsync(currentPage);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Endpoint is unreachable statusCode: {0}", response.StatusCode);
                        return discoveredUrls;
                    }

                    html = await response.Content.ReadAsStringAsync();
                    break;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("HttpException /GET {0} {1}", currentPage.AbsoluteUri, e.Message);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Timed out /GET {0}", currentPage.AbsoluteUri);
                }
                finally
                {
                    attempt++;
                }
            } while (attempt < 3);

            if (html is null)
            {
                return discoveredUrls;
            }

            StringBuilder output = new();
            output.AppendLine($"Page {currentPage.AbsoluteUri}");

            HtmlDocument document = new();
            document.LoadHtml(html);

            HtmlNodeCollection anchors = document.DocumentNode.SelectNodes("//a[@href]");

            if (anchors != null && anchors.Count > 0)
            {
                foreach (HtmlNode anchor in anchors)
                {
                    string href = anchor.GetAttributeValue("href", "");

                    if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(currentPage, href, out Uri absoluteUrl))
                    {
                        if (crawlerOptions.TraceEnabled)
                        {
                            output.AppendLine($"Invalid href {href}");
                        }

                        continue;
                    }

                    output.AppendLine($"\t{absoluteUrl.AbsoluteUri}");

                    if (absoluteUrl.Scheme != Uri.UriSchemeHttp && absoluteUrl.Scheme != Uri.UriSchemeHttps)
                    {
                        if (crawlerOptions.TraceEnabled)
                        {
                            output.AppendLine($"Url scheme not accepted (http,https) {absoluteUrl.Scheme}");
                        }

                        continue;
                    }

                    if (!string.Equals(absoluteUrl.Host, client.BaseAddress.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        if (crawlerOptions.TraceEnabled)
                        {
                            output.AppendLine($"Link does not belong to subdomain {absoluteUrl.Host}");
                        }

                        continue;
                    }

                    Uri normalizedUri = absoluteUrl.NormalizeUrl();

                    discoveredUrls.Add(normalizedUri.AbsoluteUri);
                }
            }
            else
            {
                if (crawlerOptions.TraceEnabled)
                {
                    output.AppendLine($"Page {currentPage.AbsoluteUri} no anchors <a> found");
                }
            }

            Console.Write(output.ToString());
            return discoveredUrls;
        }
    }
}