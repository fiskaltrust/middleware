namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;

public class Araba : ITicketBaiTerritory
{
    public string PolicyIdentifier => "https://ticketbai.araba.eus/tbai/sinadura/";

    public string PolicyDigest => "d69VEBc4ED4QbwnDtCA2JESgJiw+rwzfutcaSl5gYvM=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => "https://ticketbai.araba.eus/TicketBAI/v1";

    public string SandboxEndpoint => "https://pruebas-ticketbai.araba.eus/TicketBAI/v1";

    public string QrCodeValidationEndpoint => "https://ticketbai.araba.eus/tbai/qrtbai/";

    public string QrCodeSandboxValidationEndpoint => "https://pruebas-ticketbai.araba.eus/tbai/qrtbai/";

    public string SubmitInvoices => "/facturas";

    public string CancelInvoices => "/anulaciones";

    public string SubmitZuzendu => "/facturas/subsanarmodificar";

    public string CancelZuzendu => "/anulaciones/baja";
}
