using System;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Contracts.Extensions
{
    public struct StateDetail{
        public bool SigningDeviceAvailable;
        public int FailedReceiptCount;
        public DateTime? FailMoment;
    }

    public static class ReceiptResponseExtensions
    {
        public static void SetFtStateData(this ReceiptResponse receiptResponse, StateDetail stateDetail)
        {
            receiptResponse.ftStateData = JsonConvert.SerializeObject(stateDetail);
        }
   }
}
