namespace fiskaltrust.Middleware.Localization.v2.Configuration;

public enum ValidationLevel
{
    Error = 0,
    Warning = 1,
    Info = 2,
}

public class ValidationConfiguration
{
    public ValidationLevel? ValidationLevel { get; init; }
    public bool ValidationsInSignatures { get; init; }

    public static ValidationConfiguration FromConfiguration(Dictionary<string, object> configuration)
    {
        ValidationLevel? level = null;
        if (configuration.TryGetValue("ValidationLevel", out var raw) && raw is not null)
            if (Enum.TryParse<ValidationLevel>(raw.ToString(), ignoreCase: true, out var parsed))
                level = parsed;

        var inSignatures = configuration.TryGetValue("ValidationsInSignatures", out var vis)
            && bool.TryParse(vis?.ToString(), out var visBool)
            && visBool;

        return new ValidationConfiguration { ValidationLevel = level, ValidationsInSignatures = inSignatures };
    }
}
