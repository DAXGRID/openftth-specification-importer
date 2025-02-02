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
    private static JsonSerializerOptions _jsonSerializer = new JsonSerializerOptions
    {
        PropertyNamingPolicy = null,
    };

    public static async Task Main(string[] args)
    {
        const string specificationFilesRootPath = "";

        var logger = GetLogger();

        await ProcessAsync(logger, specificationFilesRootPath).ConfigureAwait(false);
    }

    private static async Task ProcessAsync(Logger logger, string specificationFilesRootPath)
    {
        var filePaths = Directory.EnumerateFiles(
            specificationFilesRootPath,
            "*.*",
            SearchOption.AllDirectories);

        var specifications = Specification.BuildSpecification(
            filePaths.Select(path => File.ReadAllText(path)));

        logger.Information(
            "Generated the following output {JsonSpecification}",
            JsonSerializer.Serialize(specifications));

        var clientId = "";
        var clientSecret = "";
        var tokenEndpoint = $"";

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("");

        using var authGraphQLClient = new AuthGraphQlClient(
            httpClient,
            new(clientId, clientSecret, tokenEndpoint));

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
                json = JsonSerializer.Serialize(specifications, _jsonSerializer)
            }
        };

        var response = await authGraphQLClient
            .MutationAsync<dynamic>(request)
            .ConfigureAwait(false);

        logger.Information(JsonSerializer.Serialize(response));
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
