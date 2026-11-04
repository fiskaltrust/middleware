using System;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions
{
    public static class ReceiptResponseExtensions
    {
        public static DateTime? GetDateTimeFromSignature(this ReceiptResponse receiptResponse, SignatureTypesDE signatureType)
        {
            var signatureData = receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (long) signatureType)?.Data;
            return !string.IsNullOrEmpty(signatureData) ? new DateTime?(DateTime.ParseExact(signatureData, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)) : null;
        }
    }
}
