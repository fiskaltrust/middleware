using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models
{
    public enum Booleano
    {
        [XmlEnum("SI")]
        Vera,
        [XmlEnum("NO")]
        Falsa
    }

    public static class BooleanoExtensions
    {
        public static bool ToBoolean(this Booleano value) => value == Booleano.Vera;
    }

    public static class BooleanExtensions
    {
        public static Booleano ToBooleano(this bool value) => value ? Booleano.Vera : Booleano.Falsa;
    }
}