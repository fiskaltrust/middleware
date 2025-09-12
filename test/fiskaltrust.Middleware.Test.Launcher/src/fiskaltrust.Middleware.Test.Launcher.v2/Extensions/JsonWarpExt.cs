using System.Text.Json;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Extensions;

public static class JsonWarpExt
{
    public static T? JsonWarp<T>(this T value) where T : new() => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
    public static T? NewtonsoftJsonWarp<T>(this T value) => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Newtonsoft.Json.JsonConvert.SerializeObject(value));

    public static Func<T, Task<U?>> JsonWarpingAsync<T, U>(this Func<string, Task<string>> func) => async value =>
        JsonSerializer.Deserialize<U>(await func(JsonSerializer.Serialize(value)).ConfigureAwait(false));
}