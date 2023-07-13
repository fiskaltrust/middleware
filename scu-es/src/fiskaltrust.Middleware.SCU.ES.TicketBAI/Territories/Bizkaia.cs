using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class Bizkaia : ITicketBaiTerritory
{
    public string PolicyIdentifier => ""https://www.batuz.eus/fitxategiak/batuz/ticketbai/sinadura_elektronikoaren_zehaztapenak_especificaciones_de_la_firma_electronica_v1_0.pdf";

    public string PolicyDigest => "Quzn98x3PMbSHwbUzaj5f5KOpiH0u8bvmwbbbNkO9Es=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => "https://tbai-z.egoitza.gipuzkoa.eus";

    public string SandboxEndpoint => "https://tbai-z.prep.gipuzkoa.eus";

    public string QrCodeValidationEndpoint => "/qr";

    public string SubmitInvoices => "/sarrerak/alta";

    public string CancelInvoices => "/sarrerak/baja";

    public string SubmitZuzendu => "/sarrerak/zuzendu-alta";

    public string CancelZuzendu => "/sarrerak/zuzendu-baja";
}