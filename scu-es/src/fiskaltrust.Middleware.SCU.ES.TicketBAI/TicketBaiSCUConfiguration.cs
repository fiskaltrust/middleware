using System;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI
{
    public class TicketBaiSCUConfiguration
    {
        [JsonConverter(typeof(X509Certificate2Converter))]
        public X509Certificate2 Certificate { get; set; } = null!;

        public TicketBaiTerritory TicketBaiTerritory { get; set; }
    }

    public class X509Certificate2Converter : JsonConverter<X509Certificate2>
    {
        public override X509Certificate2 ReadJson(JsonReader reader, Type objectType, X509Certificate2? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => new(Convert.FromBase64String((string) reader.Value!));

        public override void WriteJson(JsonWriter writer, X509Certificate2? value, JsonSerializer serializer)
            => writer.WriteValue(Convert.ToBase64String(value!.Export(X509ContentType.Pfx)));
    }

    public enum TicketBaiTerritory
    {
        Araba,
        Bizkaia,
        Gipuzkoa
    }
}