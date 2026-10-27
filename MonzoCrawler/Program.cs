using System.Diagnostics;

namespace MonzoCrawler
{
    public class Program
    {
        private static CrawlerOptions AppOptions = new();

        public static async Task Main(string[] args)
        {
            Stopwatch sw = new();
            sw.Start();

            bool continueExecution = ParseArgsToOptions(args);
            if (!continueExecution)
            {
                return;
            }

            HttpClientHandler handler = new()
            {
                AllowAutoRedirect = false
            };

            using HttpClient httpClient = new(handler)
            {
                BaseAddress = AppOptions.TargetUrl,
                Timeout = AppOptions.Timeout
            };

            UrlDiscoveryService urlDiscoveryService = new(AppOptions);

            HashSet<string> discoveredUrls = await urlDiscoveryService.DiscoverUrls(httpClient);

            sw.Stop();
            TimeSpan elapsed = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);

            Console.WriteLine("Discovered {0} unique urls in {1} seconds", discoveredUrls.Count, elapsed.TotalSeconds);
        }

        private static bool ParseArgsToOptions(string[] args)
        {
            Dictionary<string, ArgumentEnum> argumentAliases = new()
            {
                ["-u"] = ArgumentEnum.URL,
                ["--url"] = ArgumentEnum.URL,
                ["-c"] = ArgumentEnum.CONCURRENCY,
                ["--concurrency"] = ArgumentEnum.CONCURRENCY,
                ["-t"] = ArgumentEnum.TIMEOUT,
                ["--timeout"] = ArgumentEnum.TIMEOUT,
                ["--trace"] = ArgumentEnum.TRACE_ENABLED
            };

            if (args.Length == 0 || args.Contains("/?"))
            {
                Console.WriteLine(@"MonzoCrawler
Options:
-u, --url <url>             Target URL (required)
-c, --concurrency <number>  Concurrency (default 10, max 100)
-t, --timeout <number>      Individual GET timeout in seconds (default 5 seconds, max 30s)
--trace <true/false>        Trace enabled (default false)
");
                return false;
            }


            Dictionary<ArgumentEnum, string> executionParameters = [];

            for (int i = 0; i < args.Length; i += 2)
            {
                if (!argumentAliases.TryGetValue(args[i], out ArgumentEnum key))
                {
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    return false;
                }

                if (i + 1 >= args.Length || argumentAliases.ContainsKey(args[i + 1]))
                {
                    Console.WriteLine($"Missing value for argument: {args[i]}");
                    return false;
                }

                if (!executionParameters.TryAdd(key, args[i + 1]))
                {
                    Console.WriteLine($"Duplicate argument: {args[i]}");
                    return false;
                }
            }

            bool traceEnabled = false;

            if (executionParameters.TryGetValue(ArgumentEnum.TRACE_ENABLED, out string traceValue))
            {
                bool.TryParse(traceValue, out traceEnabled);
            }

            int concurrency = 10;

            if (executionParameters.TryGetValue(ArgumentEnum.CONCURRENCY, out string concurrencyValue))
            {
                if (!int.TryParse(concurrencyValue, out concurrency))
                {
                    concurrency = 10;
                    Console.WriteLine("Using default concurrency [10]");
                }
                else
                {
                    if (concurrency is <= 0 or > 100)
                    {
                        Console.WriteLine("Concurrency value must be between 1 and 100");
                        return false;
                    }
                }
            }

            TimeSpan timeout = TimeSpan.FromSeconds(5);

            if (executionParameters.TryGetValue(ArgumentEnum.TIMEOUT, out string timeoutArgValue))
            {
                if (!double.TryParse(timeoutArgValue, out double timeoutValue))
                {
                    Console.WriteLine("Using default timeout [5 seconds]");
                }
                else
                {
                    if (timeoutValue is <= 0 or > 30)
                    {
                        Console.WriteLine("Timeout value must be between 1 and 30");
                        return false;
                    }

                    timeout = TimeSpan.FromSeconds(timeoutValue);
                }
            }

            executionParameters.TryGetValue(ArgumentEnum.URL, out string targetUrl);

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                Console.WriteLine("Url argument is required");
                return false;
            }

            if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri parsedTargetUrl) || (parsedTargetUrl.Scheme != Uri.UriSchemeHttp && parsedTargetUrl.Scheme != Uri.UriSchemeHttps))
            {
                Console.WriteLine("Malformed URL [ {0} ]. Must begin with http:// or https://", targetUrl);
                return false;
            }

            AppOptions = new()
            {
                Concurrency = concurrency,
                TargetUrl = parsedTargetUrl,
                Timeout = timeout,
                TraceEnabled = traceEnabled
            };
            return true;
        }
    }
}
