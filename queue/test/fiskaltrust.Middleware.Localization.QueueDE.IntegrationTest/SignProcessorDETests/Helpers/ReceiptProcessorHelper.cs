using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;

public class ReceiptProcessorHelper
{
    private readonly ISignProcessor _signProcessor;

    public ReceiptProcessorHelper(ISignProcessor signProcessor)
    {
        _signProcessor = signProcessor;
    }

    public async Task<ReceiptResponse> ProcessReceiptRequestAsync(ReceiptRequest request)
    {
        try
        {
            var response = await _signProcessor.ProcessAsync(request);

            if (response.ftState == 0xEEEE_EEEE)
            {
                return response; 
            }

            return response; 
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unexpected error occurred while processing receipt request.", ex);
        }
    }
}
