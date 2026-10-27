namespace MonzoCrawler
{
    public static class UriExtensions
    {
        public static Uri NormalizeUrl(this Uri uri)
        {
            return new UriBuilder(uri)
            {
                Fragment = ""
            }.Uri;
        }
    }
}
