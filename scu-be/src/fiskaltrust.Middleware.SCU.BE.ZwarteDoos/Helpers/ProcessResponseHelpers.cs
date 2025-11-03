using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Helpers;

public class ProcessResponseHelpers
{
    public static ProcessResponse CreateResponse(ReceiptResponse response, string? stateData, List<SignatureItem> signaturItems)
    {
        if (response.ftSignatures.Count > 0)
        {
            var list = new List<SignatureItem>();
            list.AddRange(response.ftSignatures);
            list.AddRange(signaturItems);
            response.ftSignatures = list;
        }
        else
        {
            response.ftSignatures = signaturItems;
        }
        response.ftStateData = stateData;
        return new ProcessResponse
        {
            ReceiptResponse = response
        };
    }

    public static ProcessResponse CreateResponse(ReceiptResponse response, List<SignatureItem> signaturItems)
    {
        if (response.ftSignatures.Count > 0)
        {
            var list = new List<SignatureItem>();
            list.AddRange(response.ftSignatures);
            list.AddRange(signaturItems);
            response.ftSignatures = list;
        }
        else
        {
            response.ftSignatures = signaturItems;
        }

        return new ProcessResponse
        {
            ReceiptResponse = response
        };
    }

    public static ProcessResponse CreateResponse(ReceiptResponse receiptResponse)
    {
        return new ProcessResponse
        {
            ReceiptResponse = receiptResponse
        };
    }
}
