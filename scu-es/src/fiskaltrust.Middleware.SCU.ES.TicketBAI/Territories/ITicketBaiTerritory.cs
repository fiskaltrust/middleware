namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Territories;

public interface ITicketBaiTerritory
{
    string PolicyIdentifier { get; }
    string PolicyDigest { get; }
    string Algorithm { get; }
    string ProdEndpoint { get; }
    string SandboxEndpoint { get; }
    string QrCodeValidationEndpoint { get; }
    string SubmitInvoices { get; }
    string CancelInvoices { get; }
    string SubmitZuzendu { get; }
    string CancelZuzendu { get; }
}
