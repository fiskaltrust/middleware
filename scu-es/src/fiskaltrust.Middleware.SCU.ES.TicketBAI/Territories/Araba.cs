namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;

public class Araba : ITicketBaiTerritory
{
    public string PolicyIdentifier => "https://ticketbai.araba.eus/tbai/sinadura/";

    public string PolicyDigest => "d69VEBc4ED4QbwnDtCA2JESgJiw+rwzfutcaSl5gYvM=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => throw new System.NotImplementedException();

    public string SandboxEndpoint => throw new System.NotImplementedException();

    public string QrCodeValidationEndpoint => throw new System.NotImplementedException();

    public string SubmitInvoices => throw new System.NotImplementedException();

    public string CancelInvoices => throw new System.NotImplementedException();

    public string SubmitZuzendu => throw new System.NotImplementedException();

    public string CancelZuzendu => throw new System.NotImplementedException();
}
