using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class Gipuzkoa : ITicketBaiTerritory
{
    public string PolicyIdentifier => "https://www.gipuzkoa.eus/ticketbai/sinadura";

    public string PolicyDigest => "vSe1CH7eAFVkGN0X2Y7Nl9XGUoBnziDA5BGUSsyt8mg=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => "https://tbai-z.egoitza.gipuzkoa.eus";

    public string SandboxEndpoint => "https://tbai-z.prep.gipuzkoa.eus";

    public string QrCodeValidationEndpoint => "https://tbai.prep.gipuzkoa.eus/qr/"; 

    public string QrCodeSandboxValidationEndpoint => "https://tbai.prep.gipuzkoa.eus/qr/";

    public string SubmitInvoices => "/sarrerak/alta";

    public string CancelInvoices => "/sarrerak/baja";

    public string SubmitZuzendu => "/sarrerak/zuzendu-alta";

    public string CancelZuzendu => "/sarrerak/zuzendu-baja";
}