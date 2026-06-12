namespace MotorcycleDealer.WebApi.Security;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "SpaCorsPolicy";

    public static IReadOnlyList<string> DefaultAllowedOrigins { get; } =
    [
        "http://localhost:4200",
        "http://127.0.0.1:4200"
    ];

    public string[] AllowedOrigins { get; set; } = [];

    public string[] ResolveEffectiveOrigins()
    {
        var sanitized = (AllowedOrigins ?? [])
            .Where(origin => !string.IsNullOrWhiteSpace(origin) && origin != "*")
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return sanitized.Length > 0 ? sanitized : [.. DefaultAllowedOrigins];
    }
}
