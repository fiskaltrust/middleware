using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.ES.Common.Models;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;

public static class ReceiptRequestHelpers
{
    public static bool TryGetMiddlewareCustomer(this ReceiptRequest receiptRequest, out MiddlewareCustomer? customer)
    {
        if (receiptRequest.cbCustomer != null)
        {
            customer = JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return customer != null;
        }
        customer = null;
        return false;
    }

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
}
