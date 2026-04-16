namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;

public class GetPdfResponse
{
    public bool ok { get; set; }
    public string? filename { get; set; }
    public string? base64 { get; set; }
}
