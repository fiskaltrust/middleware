using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;

public class SignOrderData
{
    public string PosId { get; set; } = null!;
    public int PosFiscalTicketNo { get; set; }
    public string PosDateTime { get; set; } = null!;
    public string TerminalId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string EventOperation { get; set; } = null!;
    public FdmReferenceInput FdmRef { get; set; } = null!;
    public string FdmSwVersion { get; set; } = null!;
    public string DigitalSignature { get; set; } = null!;
    public decimal BufferCapacityUsed { get; set; }
    public List<ApiMessage> Warnings { get; set; } = [];
    public List<ApiMessage> Informations { get; set; } = [];
    public List<string> Footer { get; set; } = null!;
}