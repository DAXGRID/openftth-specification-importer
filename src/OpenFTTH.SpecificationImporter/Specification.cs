using System.Text.Json;

namespace OpenFTTH.SpecificationImporter;

internal static class Specification
{
    private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        AllowTrailingCommas = true,
    };

    public static Dictionary<string, List<object>> BuildSpecification(IEnumerable<string> specificationJsonTexts)
    {
        var specifications = new Dictionary<string, List<dynamic>>();

        foreach (var json in specificationJsonTexts)
        {
            var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, dynamic>>>>(
                json,
                _jsonSerializerOptions);

            ArgumentNullException.ThrowIfNull(jsonDocument);

            foreach (var x in jsonDocument)
            {
                if (!specifications.TryGetValue(x.Key, out List<dynamic>? specification))
                {
                    specification = new();
                    specifications.Add(x.Key, specification);
                }

                specification.AddRange(x.Value);
            }
        }

        return specifications;
    }
}
