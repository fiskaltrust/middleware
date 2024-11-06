using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.SAFT.CLI;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE
{
    public static class ChargeItemExtensions
    {
        public static bool IsAgencyBusiness(this ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) == 0x60;
    }

    public static class ReceiptRequestExtensions 
    {
        public static bool ContainsCustomerInfo(this ReceiptRequest receiptRequest)
        {
            if (receiptRequest.cbCustomer != null)
            {
                return JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer)) != null;
            }
            return false;
        }

        public static MiddlewareCustomer? GetCustomerOrNull(this ReceiptRequest receiptRequest)
        {
            if (receiptRequest.cbCustomer != null)
            {
                return JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer));
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
