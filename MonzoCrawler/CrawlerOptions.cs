namespace MonzoCrawler
{
    public class CrawlerOptions
    {
        public Uri TargetUrl { get; set; }
        public int Concurrency { get; set; }
        public bool TraceEnabled { get; set; }
        public TimeSpan Timeout { get; set; }
    }

}