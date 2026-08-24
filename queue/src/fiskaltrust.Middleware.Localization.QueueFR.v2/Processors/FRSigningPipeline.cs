using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The single path through which a French receipt becomes a signed, chained entry: take the next
/// number of its chain, let the SCU sign it against the chain's previous hash, and advance the
/// chain only once the signature exists.
/// </summary>
public class FRSigningPipeline
{
    private readonly IFRSSCD _sscd;
    private readonly FRChainStateProvider _chains;

    public FRSigningPipeline(IFRSSCD sscd, FRChainStateProvider chains)
    {
        _sscd = sscd;
        _chains = chains;
    }

    public Task<ProcessCommandResponse> SignAsync(ProcessCommandRequest request, List<ftActionJournal>? actionJournals = null)
        => _chains.ExecuteInChainAsync(request.ReceiptRequest, async chain =>
        {
            var receiptResponse = request.ReceiptResponse;
            var identificationBeforeSigning = receiptResponse.ftReceiptIdentification;

            chain.Numerator++;
            ReceiptIdentificationHelper.AppendChainIdentification(receiptResponse, chain);

            try
            {
                var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request.ReceiptRequest,
                    ReceiptResponse = receiptResponse,
                }, chain.LastHash).ConfigureAwait(false);

                chain.LastHash = hash;
                return new ProcessCommandResponse(response.ReceiptResponse, actionJournals ?? new List<ftActionJournal>());
            }
            catch (Exception ex) when (FRSSCDErrorHandling.IsSigningUnavailable(ex))
            {
                // No document was issued, so the number must not be consumed - NF525 requires the
                // national numbering of every chain to be gapless.
                chain.Numerator--;
                receiptResponse.ftReceiptIdentification = identificationBeforeSigning;
                receiptResponse.SetSigningUnavailableError(ex);
                return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
            }
        });
}
