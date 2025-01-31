using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json;

namespace OpenFTTH.SpecificationImporter;

internal static class Program
{
    public static void Main()
    {
        var logger = GetLogger();

        var filePaths = Directory.EnumerateFiles("", "*.*", SearchOption.AllDirectories);

        var specificationsTexts = filePaths.Select(path => File.ReadAllText(path));

        var specifications = Specification.BuildSpecification(specificationsTexts);

        logger.Information(
            "Generated the following output {JsonSpecification}",
            JsonSerializer.Serialize(specifications));
    }

    private static Logger GetLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();
    }
}
