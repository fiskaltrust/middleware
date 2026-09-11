using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;

namespace fiskaltrust.Middleware.Localization.QueuePL.AcceptanceTest.Helpers;

/// <summary>
/// Behaves like a fiscalized Polish register: owns the fiscal document numbering, reports its
/// identity through the generic InfoData blob (contract: SCU.PL.Abstraction PLDeviceInfo) and
/// enriches responses with the numer unikatowy and the fiscal document number.
/// </summary>
public class MockPLSSCD : IPLSSCD
{
    public const string UniqueDeviceNumber = "ZAS0000000001";

    public int ProcessReceiptCalls { get; private set; }
    private long _fiscalDocumentNumber;

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<PLSSCDInfo> GetInfoAsync() => Task.FromResult(new PLSSCDInfo
    {
        InfoData = JsonSerializer.Serialize(new
        {
            DeviceSerialNumber = "MCK1234567",
            UniqueDeviceNumber,
            FiscalizationState = 2,
            VatRateTable = new[]
            {
                new { PtuSlot = "A", VatRatePercent = (decimal?)23m, IsExempt = false },
                new { PtuSlot = "B", VatRatePercent = (decimal?)8m, IsExempt = false },
                new { PtuSlot = "C", VatRatePercent = (decimal?)5m, IsExempt = false },
                new { PtuSlot = "D", VatRatePercent = (decimal?)0m, IsExempt = false },
                new { PtuSlot = "G", VatRatePercent = (decimal?)null, IsExempt = true },
            },
            CrkReachable = true,
            EReceiptCapable = true,
            CurrentZReportNumber = 0,
        })
    });

    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        ProcessReceiptCalls++;
        var response = request.ReceiptResponse;
        response.ftCashBoxIdentification = UniqueDeviceNumber;
        if (request.ReceiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Receipt))
        {
            var number = ++_fiscalDocumentNumber;
            response.ftReceiptIdentification += number;
            response.ftSignatures.Add(new SignatureItem
            {
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = (SignatureType)0x504C_2000_0000_0101,
                Caption = "Numer dokumentu fiskalnego",
                Data = number.ToString(),
            });
        }
        return Task.FromResult(new ProcessResponse { ReceiptResponse = response });
    }
}
