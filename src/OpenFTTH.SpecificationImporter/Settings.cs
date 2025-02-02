using System.Text.Json.Serialization;

namespace OpenFTTH.SpecificationImporter;

internal sealed record Settings
{
    [JsonPropertyName("specificationFilesRootPath")]
    public required string SpecificationFilesRootPath { get; init; }

    [JsonPropertyName("clientId")]
    public required string ClientId { get; init; }

    [JsonPropertyName("clientSecret")]
    public required string ClientSecret { get; init; }

    [JsonPropertyName("tokenEndPoint")]
    public required string TokenEndPoint { get; init; }

    [JsonPropertyName("graphQlAddress")]
    public required string GraphQlAddress { get; init; }
}
