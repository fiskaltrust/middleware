using System;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ME.Common.Helpers
{
    public class X509Certificate2Converter : JsonConverter<X509Certificate2>
    {
        public override X509Certificate2 ReadJson(JsonReader reader, Type objectType, X509Certificate2? existingValue, bool hasExistingValue, JsonSerializer serializer) => new(Convert.FromBase64String((string)reader.Value!));

        public override void WriteJson(JsonWriter writer, X509Certificate2? value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}