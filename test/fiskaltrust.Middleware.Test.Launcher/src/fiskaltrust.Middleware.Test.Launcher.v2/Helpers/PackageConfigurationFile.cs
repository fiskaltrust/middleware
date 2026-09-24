using fiskaltrust.storage.serialization.V0;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

/// <summary>
/// A single package configuration in its own file — one queue or one SCU, as it would arrive inside a
/// cashbox configuration. <see cref="CashBoxConfiguration"/> is the primary source; these files
/// remain the override, which is how one SCU is swapped for another without editing a cashbox.
/// </summary>
static class PackageConfigurationFile
{
    public static PackageConfiguration Read(string fileName)
    {
        var path = Path.Join(AppContext.BaseDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The package configuration '{fileName}' was not found at {path}.", path);
        }

        // Newtonsoft, like the cashbox configuration: the blob travels on to a bootstrapper that
        // reads it the same way.
        var configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<PackageConfiguration>(File.ReadAllText(path))
            ?? throw new System.Text.Json.JsonException($"The package configuration '{fileName}' is empty.");

        configuration.Configuration ??= [];
        return configuration;
    }
}
