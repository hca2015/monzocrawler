namespace MonzoCrawler
{
    public class UrlDiscoveryService
    {
        private readonly CrawlerOptions crawlerOptions;
        private readonly WebCrawler webCrawler;

        public UrlDiscoveryService(CrawlerOptions crawlerOptions)
        {
            this.crawlerOptions = crawlerOptions;
            webCrawler = new(crawlerOptions);
        }

        public async Task<HashSet<string>> DiscoverUrls(HttpClient httpClient)
        {
            Uri startingUrl = crawlerOptions.TargetUrl.NormalizeUrl();
            HashSet<string> discoveredUrls = [startingUrl.AbsoluteUri];
            Queue<string> pendingUrls = new();
            pendingUrls.Enqueue(startingUrl.AbsoluteUri);

            List<Task<ICollection<string>>> tasks = [];

            while (pendingUrls.Count > 0 || tasks.Count > 0)
            {
                if (tasks.Count < crawlerOptions.Concurrency && pendingUrls.TryDequeue(out string url))
                {
                    tasks.Add(webCrawler.CrawlAsync(httpClient, url));
                    continue;
                }

                Task<ICollection<string>> completed = await Task.WhenAny(tasks);
                tasks.Remove(completed);

                ICollection<string> discovered = await completed;
                foreach (string item in discovered)
                {
                    if (discoveredUrls.Add(item))
                    {
                        pendingUrls.Enqueue(item);

                        if (crawlerOptions.TraceEnabled)
                        {
                            Console.WriteLine("Valid link discovered: {0}", item);
                        }
                    }
                }
            }

            return discoveredUrls;
        }
    }
}
