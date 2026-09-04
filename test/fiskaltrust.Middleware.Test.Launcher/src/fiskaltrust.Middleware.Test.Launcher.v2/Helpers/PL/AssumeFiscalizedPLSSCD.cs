using System.Text.Json.Nodes;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.pl;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers.PL;

/// <summary>
/// Reports the connected register as fiscalized while passing every other call through untouched.
///
/// A Polish queue only activates against a register that reports FiscalizationState 'Fiscalized',
/// and fiscalizing a device is a serwis act. A test printer runs in non-fiscal mode (scomm answers
/// <c>fsN</c>), so the initial-operation receipt fails, the queue stays inactive and no receipt ever
/// reaches the SCU — which means the printer never prints either, even though it would happily print
/// a NIEFISKALNY document. This decorator lifts exactly that one gate, so the whole
/// queue → SCU → printer path can be exercised on a non-fiscal device.
///
/// It belongs to the test launcher on purpose: neither the queue nor the PosNet SCU may ever claim a
/// fiscalization state the device does not report.
/// </summary>
public class AssumeFiscalizedPLSSCD : IPLSSCD
{
    /// <summary>PLFiscalizationState.Fiscalized in SCU.PL.Abstraction, as the InfoData blob carries it.</summary>
    private const int Fiscalized = 2;

    private readonly IPLSSCD _plsscd;

    public AssumeFiscalizedPLSSCD(IPLSSCD plsscd)
    {
        _plsscd = plsscd;
    }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => _plsscd.EchoAsync(echoRequest);

    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => _plsscd.ProcessReceiptAsync(request);

    /// <summary>
    /// Overwrites FiscalizationState in the device's own info blob instead of building a new one, so
    /// everything else the register reports — numer unikatowy, PTU table — stays as it came from the
    /// device.
    /// </summary>
    public async Task<PLSSCDInfo> GetInfoAsync()
    {
        var info = await _plsscd.GetInfoAsync();
        if (info.InfoData is not null && JsonNode.Parse(info.InfoData) is JsonObject infoData)
        {
            infoData["FiscalizationState"] = Fiscalized;
            info.InfoData = infoData.ToJsonString();
        }
        return info;
    }
}
