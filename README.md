# MonzoCrawler

A C# web crawler that visits pages and subpages, following links on the same host as the starting URL and avoiding duplicate URLs.

Requires the .NET 10 SDK. Run these commands from the solution directory.

## Build

```sh
dotnet build MonzoCrawler.slnx
```

## Test

```sh
dotnet test MonzoCrawler.slnx
```

Tests use WireMock. Port 9999 must be available.

## Run

```sh
dotnet run --project MonzoCrawler -- --url https://crawlme.monzo.com --concurrency 10 --trace true --timeout 5
```

- `--url` or `-u`: starting URL (required).
- `--concurrency` or `-c`: maximum number of concurrent tasks. Default: 10; range: 1 to 100.
- `--trace`: enables detailed output (`true` or `false`). Default: `false`.
- `--timeout` or `-t`: timeout per request, in seconds. Default: 5; must be greater than 0 and at most 30.

## Help

```sh
dotnet run --project MonzoCrawler -- /?
```