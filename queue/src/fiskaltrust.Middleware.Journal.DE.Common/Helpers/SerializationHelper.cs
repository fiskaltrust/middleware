using System;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    public static class SerializationHelper
    {
        public static PayItemCaseData GetPayItemCaseData(PayItem payItem) => GetValueOrNull<PayItemCaseData>(payItem?.ftPayItemCaseData);

        public static ChargeItemCaseData GetChargeItemCaseData(ChargeItem chargeItem) => GetValueOrNull<ChargeItemCaseData>(chargeItem?.ftChargeItemCaseData);

        public static ReceiptCaseData GetReceiptCaseData(ReceiptRequest receiptRequest) => GetValueOrNull<ReceiptCaseData>(receiptRequest?.ftReceiptCaseData);

        public static ReceiptCaseData GetReceiptCaseData(ReceiptReferencesGroupedData receiptReferencesGrouped) => GetValueOrNull<ReceiptCaseData>(receiptReferencesGrouped?.TargetReceiptCaseData);

        public static Customer GetCustomer(ReceiptRequest receiptRequest) => GetValueOrNull<Customer>(receiptRequest?.cbCustomer);

        public static StateData GetStateData(ReceiptResponse receiptResponse) => GetValueOrNull<StateData>(receiptResponse?.ftStateData);

        private static T GetValueOrNull<T>(string data) where T : class
        {
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(data);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
