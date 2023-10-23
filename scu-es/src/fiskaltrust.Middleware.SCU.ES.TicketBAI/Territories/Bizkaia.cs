using System;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public class Bizkaia : ITicketBaiTerritory
{
    public string PolicyIdentifier => "https://www.batuz.eus/fitxategiak/batuz/ticketbai/sinadura_elektronikoaren_zehaztapenak_especificaciones_de_la_firma_electronica_v1_0.pdf";

    public string PolicyDigest => "Quzn98x3PMbSHwbUzaj5f5KOpiH0u8bvmwbbbNkO9Es=";

    public string Algorithm => "SHA256";

    public string ProdEndpoint => "https://sarrerak.bizkaia.eus";

    public string SandboxEndpoint => "https://pruesarrerak.bizkaia.eus";

    public string QrCodeValidationEndpoint => "https://batuz.eus/QRTBAI/";

    public string QrCodeSandboxValidationEndpoint => "https://batuz.eus/QRTBAI/"; 

    public string SubmitInvoices => "/N3B4000M/aurkezpena";

    public string CancelInvoices => "/N3B4000M/aurkezpena";

    public string GetInvoices => "/N3B4001M/kontsulta";

    public string SubmitZuzendu => throw new NotImplementedException();

    public string CancelZuzendu => throw new NotImplementedException();

    public static Bizkaian3Header GenerateHeader(string nif, string nrs, string mode, string ejer)
    {
        var header = new Bizkaian3Header
        {
            inte = new Bizkaian3HeaderInte
            {
                nif = nif,
                nrs = nrs
            },
            drs = new Bizkaian3HeaderDrs
            {
                mode = mode,
                ejer = ejer
            }
        };
        return header;
    }
}

public class Bizkaian3Header
{
    public string con => "LROE";
    public string apa => "1.1";
    public Bizkaian3HeaderInte? inte { get; set; }
    public Bizkaian3HeaderDrs? drs { get; set; }
}

public class Bizkaian3HeaderInte
{
    public string? nif { get; set; }
    public string? nrs { get; set; }
}

public class Bizkaian3HeaderDrs
{
    public string? mode { get; set; }
    public string? ejer { get; set; }
}