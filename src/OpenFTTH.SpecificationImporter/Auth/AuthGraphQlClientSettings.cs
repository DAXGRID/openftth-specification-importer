namespace OpenFTTH.SpecificationImporter.Auth;

internal sealed record AuthGraphQlClientSettings(
    string ClientId,
    string ClientSecret,
    string TokenEndPoint);
