using System.Text.Json;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Extensions;

public static class JsonWarpExt
{
    public static T? JsonWarp<T>(this T value) where T : new() => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
    public static T? NewtonsoftJsonWarp<T>(this T value) where T : new() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Newtonsoft.Json.JsonConvert.SerializeObject(value));

    /// <summary>
    /// Re-reads a package configuration into System.Text.Json values. Newtonsoft deserializes the
    /// blob into JValue/JObject; an SCU that parses its configuration with System.Text.Json would
    /// otherwise serialize those wrapper types instead of the values they carry.
    /// </summary>
    public static Dictionary<string, object> ToSystemTextJsonValues(this Dictionary<string, object> configuration)
        => JsonSerializer.Deserialize<Dictionary<string, object>>(Newtonsoft.Json.JsonConvert.SerializeObject(configuration))!;

    public static Func<T, Task<U?>> JsonWarpingAsync<T, U>(this Func<string, Task<string>> func) => async value =>
        JsonSerializer.Deserialize<U>(await func(JsonSerializer.Serialize(value)).ConfigureAwait(false));
}