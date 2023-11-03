using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public class ProcessResponseHelpers
{
    public static ProcessResponse CreateResponse(ReceiptResponse response, string? stateData, List<SignaturItem> signaturItems)
    {
        if (response.ftSignatures.Length > 0)
        {
            var list = new List<SignaturItem>();
            list.AddRange(response.ftSignatures);
            list.AddRange(signaturItems);
            response.ftSignatures = list.ToArray();
        }
        else
        {
            response.ftSignatures = signaturItems.ToArray();
        }
        response.ftStateData = stateData;
        return new ProcessResponse
        {
            ReceiptResponse = response
        };
    }

    public static ProcessResponse CreateResponse(ReceiptResponse response, List<SignaturItem> signaturItems)
    {
        if (response.ftSignatures.Length > 0)
        {
            var list = new List<SignaturItem>();
            list.AddRange(response.ftSignatures);
            list.AddRange(signaturItems);
            response.ftSignatures = list.ToArray();
        }
        else
        {
            response.ftSignatures = signaturItems.ToArray();
        }

        return new ProcessResponse
        {
            ReceiptResponse = response
        };
    }

}
