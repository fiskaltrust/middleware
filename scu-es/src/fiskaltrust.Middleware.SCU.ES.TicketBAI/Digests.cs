namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public static class Digests
{
    public static class Araba
    {
        public const string POLICY_IDENTIFIER = "https://ticketbai.araba.eus/tbai/sinadura/";
        public const string POLICY_DIGEST = "d69VEBc4ED4QbwnDtCA2JESgJiw+rwzfutcaSl5gYvM=";
        public const string ALGORITHM = "SHA256";
    }

    public static class Bizkaia
    {
        public const string POLICY_IDENTIFIER = "https://www.batuz.eus/fitxategiak/batuz/ticketbai/sinadura_elektronikoaren_zehaztapenak_especificaciones_de_la_firma_electronica_v1_0.pdf";
        public const string POLICY_DIGEST = "Quzn98x3PMbSHwbUzaj5f5KOpiH0u8bvmwbbbNkO9Es=";
        public const string ALGORITHM = "SHA256";
    }

    public static class Gipuzkoa
    {
        public const string POLICY_IDENTIFIER = "https://www.gipuzkoa.eus/ticketbai/sinadura";
        public const string POLICY_DIGEST = "vSe1CH7eAFVkGN0X2Y7Nl9XGUoBnziDA5BGUSsyt8mg=";
        public const string ALGORITHM = "SHA256";
    }
}