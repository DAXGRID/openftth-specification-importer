using GraphQL;
using OpenFTTH.SpecificationImporter.Auth;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json;

namespace OpenFTTH.SpecificationImporter;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        await ProcessAsync(GetLogger(), AppSetting.Load<Settings>()).ConfigureAwait(false);
    }

    private static async Task ProcessAsync(Logger logger, Settings settings)
    {
        var filePaths = Directory.EnumerateFiles(
            settings.SpecificationFilesRootPath,
            "*.json",
            SearchOption.AllDirectories).ToList();

        filePaths.ForEach((x) => logger.Information("Found file: {FileName}", x));

        var specifications = Specification.BuildSpecification(
            filePaths.Select(path => File.ReadAllText(path)));

        logger.Information(
            "Generated the following output {JsonSpecification}",
            JsonSerializer.Serialize(specifications));

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(settings.GraphQlAddress);

        using var authGraphQLClient = new AuthGraphQlClient(
            httpClient,
            new(settings.ClientId, settings.ClientSecret, settings.TokenEndPoint));

        var request = new GraphQLRequest
        {
            Query = @"
            mutation ($json: String!) {
              specifications {
                importFromJsonString(json: $json) {
                  isSuccess
                  errorCode
                  errorMessage
                }
              }
            }",
            Variables = new
            {
                json = JsonSerializer.Serialize(specifications)
            }
        };

        logger.Information("Sending request to GraphQL endpoint.");

        var response = await authGraphQLClient
            .MutationAsync<object>(request)
            .ConfigureAwait(false);

        logger.Information(JsonSerializer.Serialize(response.Data));
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
