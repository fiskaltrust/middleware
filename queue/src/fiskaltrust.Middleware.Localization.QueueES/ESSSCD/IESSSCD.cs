using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

public interface IESSSCD
{
    Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

    Task<ESSSCDInfo> GetInfoAsync();
}


public class Encadenamiento
{
    public required string IDEmisorFactura { get; set; }

    public required string NumSerieFactura { get; set; }

    public required string FechaExpedicionFactura { get; set; }

    public required string Huella { get; set; }
}

public class StateData
{
    public required Encadenamiento? EncadenamientoAlta { get; set; }

    public required Encadenamiento? EncadenamientoAnulacion { get; set; }
}

public class ProcessRequest
{
    public required ReceiptRequest ReceiptRequest { get; set; }

    public required ReceiptResponse ReceiptResponse { get; set; }

    public required StateData StateData { get; set; }
}

public class ProcessResponse
{
    public required ReceiptResponse ReceiptResponse { get; set; }
    public required byte[] Journal { get; set; }
    public required string JournalType { get; set; }
    public required StateData StateData { get; set; }
}