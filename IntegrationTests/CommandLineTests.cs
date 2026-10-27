using MonzoCrawler;

namespace IntegrationTests;

[Collection("Crawler site")]
public class CommandLineTests
{
    [Theory]
    [InlineData(new[] { "--url" }, "Missing value for argument: --url")]
    [InlineData(new[] { "--url", "--trace" }, "Missing value for argument: --url")]
    [InlineData(new[] { "--unknown", "value" }, "Unknown argument: --unknown")]
    public async Task Main_RejectsMalformedArgumentsWithoutCrashing(string[] arguments, string expectedMessage)
    {
        using StringWriter output = new();
        TextWriter originalOutput = Console.Out;

        try
        {
            Console.SetOut(output);
            await Program.Main(arguments);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        Assert.Contains(expectedMessage, output.ToString());
    }
}
