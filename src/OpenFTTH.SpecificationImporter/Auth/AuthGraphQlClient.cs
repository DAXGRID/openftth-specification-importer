using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Text.Json;

namespace OpenFTTH.SpecificationImporter.Auth;

internal sealed class AuthGraphQlClient : IDisposable
{
    private readonly AuthGraphQlClientSettings _settings;
    private readonly Dictionary<string, string> _values;
    private readonly HttpClient _httpClient;
    private readonly GraphQLHttpClient _graphQLClient;
    private JsonWebToken? _token;

    public AuthGraphQlClient(HttpClient httpClient, AuthGraphQlClientSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;

        _values = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", settings.ClientId },
            { "client_secret", settings.ClientSecret },
        };

        _graphQLClient = new(
            options: new(),
            serializer: new NewtonsoftJsonSerializer(),
            httpClient: _httpClient);
    }

    public async Task<GraphQLResponse<T>> MutationAsync<T>(GraphQLRequest request)
    {
        if (_token is null || IsTokenExpired(_token))
        {
            var token = await GetToken().ConfigureAwait(false);
            _token = token;

            // We cannot just update the header, so we have to remove it first.
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }

            _httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {_token.AccessToken}");
        }

        return await _graphQLClient
            .SendMutationAsync<T>(request)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        _graphQLClient.Dispose();
    }

    private static bool IsTokenExpired(JsonWebToken jsonWebToken) =>
        DateTime.UtcNow > jsonWebToken.ExpiresAt;

    private async Task<JsonWebToken> GetToken()
    {
        using var content = new FormUrlEncodedContent(_values);

        var response = await _httpClient
            .PostAsync(new Uri(_settings.TokenEndPoint), content)
            .ConfigureAwait(false);

        var tokenResponseBody = await response.Content
            .ReadAsStringAsync()
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Could not receive token, received statuscode: '{response.StatusCode}'. Response body '{tokenResponseBody}'.");
        }

        return JsonSerializer.Deserialize<JsonWebToken>(tokenResponseBody) ??
            throw new InvalidOperationException("Could not deserialize the JWT.");
    }
}
