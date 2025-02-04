using GraphQL;
using OpenFTTH.SpecificationImporter.Auth;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            settings.TopDirectoriesOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories)
            .Select(x => new FileInfo(x))
            .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden))
            .Select(x => x.FullName)
            .ToList();

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
            .MutationAsync<ImportFromJsonStringResponse>(request)
            .ConfigureAwait(false);

        if (response.Errors?.Length > 0)
        {
            foreach (var error in response.Errors)
            {
                logger.Information(error.Message);
            }

            throw new GraphQlFailedException("Received error response back from GraphQL, something is wrong with the mutation.");
        }

        var importJsonStringResponse = response.Data.Specifications.ImportFromJsonString;
        if (!importJsonStringResponse.IsSuccess)
        {
            throw new GraphQlFailedException($"Failed with errorcode: '{importJsonStringResponse.ErrorCode}' and error message: '{importJsonStringResponse.ErrorMessage}'.");
        }

        logger.Information(
            "{GraphQlResponseJson}",
            JsonSerializer.Serialize(importJsonStringResponse));
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

internal sealed record ImportFromJsonStringResponse
{
    [JsonPropertyName("specifications")]
    public required SpecificationsWrapper Specifications { get; init; }
}

internal sealed record SpecificationsWrapper
{
    [JsonPropertyName("importFromJsonString")]
    public required ImportFromJsonStringResult ImportFromJsonString { get; init; }
}

internal sealed record ImportFromJsonStringResult
{
    [JsonPropertyName("isSuccess")]
    public required bool IsSuccess { get; init; }

    [JsonPropertyName("errorCode")]
    public required string ErrorCode { get; init; }

    [JsonPropertyName("errorMessage")]
    public required string ErrorMessage { get; init; }
}

public class GraphQlFailedException : Exception
{
    public GraphQlFailedException() {}
    public GraphQlFailedException(string? message) : base(message) {}
    public GraphQlFailedException(string? message, Exception? innerException) : base(message, innerException) {}
}
