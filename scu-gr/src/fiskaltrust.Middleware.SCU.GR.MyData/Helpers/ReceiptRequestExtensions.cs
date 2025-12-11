using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
public class MiddlewareCustomer
{
    public string? CustomerName { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerType { get; set; }
    public string? CustomerStreet { get; set; }
    public string? CustomerZip { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerCountry { get; set; }
    public string? CustomerVATId { get; set; }
}
/// <summary>
/// Defines the country category of a customer for Greek tax purposes
/// </summary>
public enum CustomerCountryCategory
{
    /// <summary>
    /// Customer is from Greece (domestic)
    /// </summary>
    Domestic,
    
    /// <summary>
    /// Customer is from an EU country (non-Greek)
    /// </summary>
    EU,
    
    /// <summary>
    /// Customer is from a non-EU country
    /// </summary>
    ThirdCountry
}

public static class ReceiptRequestExtensions
{
    public static bool TryDeserializeftReceiptCaseData<T>(this ReceiptRequest request, out T? result) where T : class
    {
        result = default;
        try
        {
            if (request.ftReceiptCaseData is null)
            {
                return false;
            }
            result = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(request.ftReceiptCaseData), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result != null;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }

    public static List<(ChargeItem chargeItem, List<ChargeItem> modifiers)> GetGroupedChargeItemsModifyPositionsIfNotSet(this ReceiptRequest receiptRequest)
    {
        var data = new List<(ChargeItem chargeItem, List<ChargeItem> modifiers)>();
        var currentPos = 1;
        foreach (var receiptChargeItem in receiptRequest.cbChargeItems)
        {
            if (receiptChargeItem.Position == 0)
            {
                receiptChargeItem.Position = currentPos;
            }
            if (receiptChargeItem.IsVoucherRedeem() || receiptChargeItem.IsDiscountOrExtra())
            {
                var last = data.LastOrDefault();
                if (last == default)
                {
                    data.Add((receiptChargeItem, new List<ChargeItem>()));
                }
                else
                {
                    last.modifiers.Add(receiptChargeItem);
                }
            }
            else
            {
                data.Add((receiptChargeItem, new List<ChargeItem>()));
                currentPos++;
            }
        }
        return data;
    }


    public static List<(ChargeItem chargeItem, List<ChargeItem> modifiers)> GetGroupedChargeItems(this ReceiptRequest receiptRequest)
    {
        var data = new List<(ChargeItem chargeItem, List<ChargeItem> modifiers)>();
        foreach (var receiptChargeItem in receiptRequest.cbChargeItems)
        {
            if (receiptChargeItem.IsVoucherRedeem() || receiptChargeItem.IsDiscountOrExtra())
            {
                var last = data.LastOrDefault();
                if (last == default)
                {
                    data.Add((receiptChargeItem, new List<ChargeItem>()));
                }
                else
                {
                    last.modifiers.Add(receiptChargeItem);
                }
            }
            else
            {
                data.Add((receiptChargeItem, new List<ChargeItem>()));
            }
        }
        return data;
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

    public static bool HasOnlyServiceItems(this ReceiptRequest receiptRequest) => receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService));

    public static bool HasAtLeastOneServiceItemAndOnlyUnknowns(this ReceiptRequest receiptRequest) => 
        receiptRequest.cbChargeItems.Count(receiptRequest => receiptRequest.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService)) > 0 &&
        receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService) || x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.UnknownService));

    /// <summary>
    /// Checks if all charge items are service items, excluding special taxes from the evaluation.
    /// Special taxes are ignored when determining if an invoice is a service invoice.
    /// </summary>
    /// <param name="receiptRequest">The receipt request to evaluate</param>
    /// <returns>True if all non-special-tax items are service items</returns>
    public static bool HasOnlyServiceItemsExcludingSpecialTaxes(this ReceiptRequest receiptRequest)
    {
        var nonSpecialTaxItems = receiptRequest.cbChargeItems.Where(x => !SpecialTaxMappings.IsSpecialTaxItem(x));
        
        // If there are no non-special-tax items, we consider this as not being a service invoice
        if (!nonSpecialTaxItems.Any())
        {
            return false;
        }
        
        return nonSpecialTaxItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService));
    }

    /// <summary>
    /// Checks if there is at least one service item and only unknowns among the rest, excluding special taxes from the evaluation.
    /// Special taxes are ignored when determining if an invoice is a service invoice.
    /// </summary>
    /// <param name="receiptRequest">The receipt request to evaluate</param>
    /// <returns>True if there's at least one service item and the rest are unknowns (excluding special taxes)</returns>
    public static bool HasAtLeastOneServiceItemAndOnlyUnknownsExcludingSpecialTaxes(this ReceiptRequest receiptRequest)
    {
        var nonSpecialTaxItems = receiptRequest.cbChargeItems.Where(x => !SpecialTaxMappings.IsSpecialTaxItem(x));
        
        // If there are no non-special-tax items, we consider this as not being a service invoice
        if (!nonSpecialTaxItems.Any())
        {
            return false;
        }
        
        return nonSpecialTaxItems.Count(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService)) > 0 &&
               nonSpecialTaxItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService) || 
                                          x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.UnknownService));
    }

    /// <summary>
    /// Gets the customer's country category based on their country code
    /// </summary>
    /// <param name="receiptRequest">The receipt request containing customer information</param>
    /// <returns>The customer's country category (Domestic, EU, or ThirdCountry)</returns>
    /// <summary>
    /// Gets the customer's country category based on their country code
    /// </summary>
    /// <param name="receiptRequest">The receipt request containing customer information</param>
    /// <returns>The customer's country category (Domestic, EU, or ThirdCountry)</returns>
    public static CustomerCountryCategory GetCustomerCountryCategory(this ReceiptRequest receiptRequest)
    {
        var customer = receiptRequest.GetCustomerOrNull();
        
        // Default to domestic if no customer information is available
        if (customer == null)
        {
            return CustomerCountryCategory.Domestic;
        }

        var countryCode = customer.CustomerCountry;
        
        // Default to domestic if no country code is provided
        if (string.IsNullOrEmpty(countryCode))
        {
            return CustomerCountryCategory.Domestic;
        }
        
        // Normalize the country code for comparison
        countryCode = countryCode.Trim().ToUpperInvariant();
        
        // Check if domestic (Greek)
        if (string.Equals(countryCode, "GR", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(countryCode, "EL", StringComparison.OrdinalIgnoreCase))
        {
            return CustomerCountryCategory.Domestic;
        }
        
        // Check if EU country
        if (EU_Countries.Contains(countryCode))
        {
            return CustomerCountryCategory.EU;
        }
        
        // Otherwise, it's a third country
        return CustomerCountryCategory.ThirdCountry;
    }
    
    /// <summary>
    /// List of EU country codes (ISO 3166-1 alpha-2 codes)
    /// </summary>
    private static readonly HashSet<string> EU_Countries = new HashSet<string>
    {
        "AT", // Austria
        "BE", // Belgium
        "BG", // Bulgaria
        "HR", // Croatia
        "CY", // Cyprus
        "CZ", // Czech Republic
        "DK", // Denmark
        "EE", // Estonia
        "FI", // Finland
        "FR", // France
        "DE", // Germany
        "GR", // Greece
        "EL", // Greece (alternative code)
        "HU", // Hungary
        "IE", // Ireland
        "IT", // Italy
        "LV", // Latvia
        "LT", // Lithuania
        "LU", // Luxembourg
        "MT", // Malta
        "NL", // Netherlands
        "PL", // Poland
        "PT", // Portugal
        "RO", // Romania
        "SK", // Slovakia
        "SI", // Slovenia
        "ES", // Spain
        "SE"  // Sweden
    };
}