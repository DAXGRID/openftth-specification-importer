using System.Text.Json.Serialization;

namespace OpenFTTH.SpecificationImporter.Auth;

internal sealed record JsonWebToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresInSeconds { get; set; }

    public DateTime ExpiresAt { get; set; }

    [JsonConstructor]
    public JsonWebToken(string accessToken, int expiresInSeconds)
    {
        if (String.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }

        AccessToken = accessToken;
        ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }
}
