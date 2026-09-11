using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Models;

/// <summary>
/// The device-neutral fiscal operations a Polish SCU must be able to perform. Device SCUs
/// (e.g. Posnet) map these to their protocol frames; the command set follows the receipt flow
/// trinit → trline → trpayment → trend plus the report and status operations.
/// </summary>
public enum PLFiscalCommandType
{
    GetStatus,
    GetDeviceInfo,
    BeginReceipt,
    AddReceiptLine,
    AddPayment,
    EndReceipt,
    CancelReceipt,
    DailyReport,
    PeriodicReport,
}

/// <summary>
/// A single device-neutral fiscal command. Deliberately loosely typed while the Posnet protocol
/// implementation (market-pl#3) settles the field glossary; device SCUs translate
/// <see cref="Parameters"/> into their protocol-specific representation.
/// </summary>
public class PLFiscalCommand
{
    public PLFiscalCommandType CommandType { get; set; }

    public Dictionary<string, string> Parameters { get; set; } = new();
}
