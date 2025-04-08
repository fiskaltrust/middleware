using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE
{

    // public static class ChargeItemExtensions
    // {
    //     public static bool IsAgencyBusiness(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) == 0x60;
    // }

    public static class ReceiptRequestExtensions
    {
        public static bool ContainsCustomerInfo(this ReceiptRequest receiptRequest)
        {
            if (receiptRequest.cbCustomer != null)
            {
                return JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) != null;
            }
            return false;
        }

        public static MiddlewareCustomer? GetCustomerOrNull(this ReceiptRequest receiptRequest)
        {
            if (receiptRequest.cbCustomer != null)
            {
                return JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return null;
        }

        public static bool HasGreeceCustomer(this ReceiptRequest receiptRequest)
        {
            var customer = receiptRequest.GetCustomerOrNull();
            if (customer != null)
            {
                if (customer.CustomerCountry == "GR" || string.IsNullOrEmpty(customer.CustomerCountry))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasGreeceCountryCode(this ReceiptRequest receiptRequest)
        {
            return ((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000) == 0x4752_0000_0000_0000;
        }

        public static bool HasNonEUCountryCode(this ReceiptRequest receiptRequest)
        {
            return ((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000) == 0x0000_0000_0000_0000;
        }

        public static bool HasOnlyServiceItems(this ReceiptRequest receiptRequest) => receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService));

        public static bool HasEUCountryCode(this ReceiptRequest receiptRequest)
        {
            return EU_CountryCodes.Contains((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000);
        }

        public static List<ulong> EU_CountryCodes = new List<ulong> { 0x4555_0000_0000_0000, 0x4752_0000_0000_0000, 0x4154_0000_0000_0000 };

        public static bool HasEUCustomer(this ReceiptRequest receiptRequest)
        {
            var customer = receiptRequest.GetCustomerOrNull();
            if (customer != null)
            {
                if (customer.CustomerCountry == "AT")
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasNonEUCustomer(this ReceiptRequest receiptRequest)
        {
            var customer = receiptRequest.GetCustomerOrNull();
            if (customer != null)
            {
                if (customer.CustomerCountry == "US")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
