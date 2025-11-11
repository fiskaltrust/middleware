using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using System;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public class ReceiptRequestValidatorPT
{
    public static void ValidateReceiptOrThrow(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            throw new Exception(ErrorMessagesPT.VoidNotSupported);
        }

        ValidateUserRequirement(receiptRequest);
        ValidateChargeItems(receiptRequest);
        ValidateCustomerTaxId(receiptRequest);
        ValidateReceiptCaseSpecificRules(receiptRequest);
    }

    private static void ValidateUserRequirement(ReceiptRequest receiptRequest)
    {
        // Check if cbUser is null or if it's a string that's empty/whitespace
        if (receiptRequest.cbUser == null || 
            (receiptRequest.cbUser is string userString && string.IsNullOrWhiteSpace(userString)))
        {
            throw new Exception(ErrorMessagesPT.EEEE_UserMissing);
        }
    }

    private static void ValidateCustomerTaxId(ReceiptRequest receiptRequest)
    {
        // Skip validation if no customer data is provided
        if (receiptRequest.cbCustomer == null)
        {
            return;
        }

        try
        {
            var middlewareCustomer = JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer));
            if (middlewareCustomer == null)
            {
                return;
            }

            // If customer tax ID is provided, validate it
            if (!string.IsNullOrWhiteSpace(middlewareCustomer.CustomerVATId))
            {
                if (!IsValidPortugueseTaxId(middlewareCustomer.CustomerVATId))
                {
                    throw new Exception(ErrorMessagesPT.EEEE_InvalidPortugueseTaxId(middlewareCustomer.CustomerVATId));
                }
            }
        }
        catch (JsonException)
        {
            // If deserialization fails, skip customer validation
            return;
        }
    }

    /// <summary>
    /// Validates a Portuguese Tax Identification Number (NIF - Número de Identificação Fiscal).
    /// Based on the algorithm described at: https://pt.wikipedia.org/wiki/N%C3%BAmero_de_identifica%C3%A7%C3%A3o_fiscal
    /// </summary>
    /// <param name="taxId">The tax ID to validate</param>
    /// <returns>True if the tax ID is valid, false otherwise</returns>
    public static bool IsValidPortugueseTaxId(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
        {
            return false;
        }

        // Clean the input: remove spaces and convert to uppercase
        taxId = taxId.Trim().ToUpper();

        // Must be exactly 9 digits
        if (taxId.Length != 9 || !taxId.All(char.IsDigit))
        {
            return false;
        }

        var digits = taxId.Select(c => int.Parse(c.ToString())).ToArray();

        // First digit must be valid (1, 2, 3, 5, 6, 7, 8, or 9)
        // 1 - Pessoa singular (natural person)
        // 2 - Pessoa singular (natural person)
        // 3 - Pessoa singular (natural person)
        // 5 - Pessoa coletiva (legal entity)
        // 6 - Administração pública (public administration)
        // 7 - Herança indivisa (undivided inheritance)
        // 8 - Empresário em nome individual (sole proprietor)
        // 9 - Pessoa coletiva (legal entity)
        var validFirstDigits = new[] { 1, 2, 3, 5, 6, 7, 8, 9 };
        if (!validFirstDigits.Contains(digits[0]))
        {
            return false;
        }

        // Calculate check digit using the Luhn-like algorithm
        // Multiply each of the first 8 digits by (9 - position)
        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += digits[i] * (9 - i);
        }

        // Calculate the check digit
        var remainder = sum % 11;
        var expectedCheckDigit = remainder < 2 ? 0 : 11 - remainder;

        // Verify the 9th digit matches the calculated check digit
        return digits[8] == expectedCheckDigit;
    }

    private static void ValidateChargeItems(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbChargeItems == null || receiptRequest.cbChargeItems.Count == 0)
        {
            return; // No charge items to validate
        }

        for (int i = 0; i < receiptRequest.cbChargeItems.Count; i++)
        {
            var chargeItem = receiptRequest.cbChargeItems[i];
            var position = i + 1;

            // Validate mandatory fields
            ValidateChargeItemMandatoryFields(chargeItem, position);

            // Validate description length
            ValidateChargeItemDescription(chargeItem, position);

            // Validate bottle restriction for specific articles
            ValidateBottleRestrictions(chargeItem, position, receiptRequest.ftReceiptCase);
        }
    }

    private static void ValidateReceiptCaseSpecificRules(ReceiptRequest receiptRequest)
    {
        // Extract the base receipt case (without flags)
        var baseReceiptCase = (ReceiptCase)((long)receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF);

        switch (baseReceiptCase)
        {
            case ReceiptCase.PointOfSaleReceipt0x0001:
                ValidatePointOfSaleReceiptRules(receiptRequest);
                break;
            case ReceiptCase.PaymentTransfer0x0002:
                ValidatePaymentTransferRules(receiptRequest);
                break;
            case ReceiptCase.ECommerce0x0004:
                ValidateECommerceRules(receiptRequest);
                break;
            // Add more cases as needed
        }
    }

    private static void ValidatePointOfSaleReceiptRules(ReceiptRequest receiptRequest)
    {
        // Specific validation for Point of Sale receipts
        // For example, stricter bottle restrictions or additional requirements
    }

    private static void ValidatePaymentTransferRules(ReceiptRequest receiptRequest)
    {
        // Specific validation for Payment Transfer receipts
        // May have different bottle restrictions or user requirements
    }

    private static void ValidateECommerceRules(ReceiptRequest receiptRequest)
    {
        // Specific validation for E-Commerce receipts
        // May have additional customer information requirements
    }

    private static void ValidateChargeItemMandatoryFields(ChargeItem chargeItem, int position)
    {
        // Description validation
        if (string.IsNullOrWhiteSpace(chargeItem.Description))
        {
            throw new Exception(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(position, "description is missing"));
        }

        // VAT Rate validation
        if (chargeItem.VATRate < 0)
        {
            throw new Exception(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(position, "VAT rate is missing or invalid"));
        }

        // Amount (price) validation
        if (chargeItem.Amount == 0)
        {
            throw new Exception(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(position, "amount (price) is missing or zero"));
        }
    }

    private static void ValidateChargeItemDescription(ChargeItem chargeItem, int position)
    {
        if (!string.IsNullOrWhiteSpace(chargeItem.Description) && chargeItem.Description.Length <= 3)
        {
            throw new Exception(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(position, "description must be longer than 3 characters"));
        }
    }

    private static void ValidateBottleRestrictions(ChargeItem chargeItem, int position, ReceiptCase receiptCase)
    {
        // Check if the charge item description or product code indicates it's a bottle under 1 liter
        if (IsBottleLessThanOneLiter(chargeItem))
        {
            // Apply stricter restrictions for certain receipt types
            var baseReceiptCase = (ReceiptCase)((long)receiptCase & 0x0000_0000_0000_FFFF);
            if (baseReceiptCase == ReceiptCase.PointOfSaleReceipt0x0001 || 
                baseReceiptCase == ReceiptCase.ECommerce0x0004)
            {
                throw new Exception(ErrorMessagesPT.EEEE_ChargeItemValidationFailed(position, "articles classified as 'Garrafão < 1 litro' are not allowed"));
            }
        }
    }

    private static bool IsBottleLessThanOneLiter(ChargeItem chargeItem)
    {
        // Check various indicators that this might be a bottle under 1 liter
        var description = chargeItem.Description?.ToLowerInvariant() ?? "";
        var productGroup = chargeItem.ProductGroup?.ToLowerInvariant() ?? "";
        var productCode = chargeItem.ProductBarcode?.ToLowerInvariant() ?? "";

        // Portuguese terms for bottles/containers under 1 liter
        var bottleIndicators = new[]
        {
            "garrafão",
            "garrafa", 
            "frasco",
            "recipiente",
            "embalagem"
        };

        var sizeIndicators = new[]
        {
            "ml",
            "centilitro",
            "cl",
            "0,", // decimal separator for values less than 1
            "0."  // decimal separator for values less than 1
        };

        // Check if description contains bottle indicators
        bool hasBottleIndicator = bottleIndicators.Any(indicator => description.Contains(indicator));
        
        if (!hasBottleIndicator)
        {
            return false;
        }

        // If it's a bottle, check if it's less than 1 liter
        bool hasSizeIndicator = sizeIndicators.Any(indicator => description.Contains(indicator) || productCode.Contains(indicator));

        // Additional checks for specific size patterns
        if (hasSizeIndicator)
        {
            // Look for patterns like "500ml", "75cl", "0.5l", etc.
            if (description.Contains("ml") || description.Contains("cl"))
            {
                // Extract numbers before ml/cl indicators
                var matches = System.Text.RegularExpressions.Regex.Matches(description, @"(\d+)\s*(ml|cl)");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (int.TryParse(match.Groups[1].Value, out int value))
                    {
                        var unit = match.Groups[2].Value;
                        // Convert to liters and check if less than 1
                        decimal liters = unit == "ml" ? value / 1000m : value / 100m;
                        if (liters < 1.0m)
                        {
                            return true;
                        }
                    }
                }
            }

            // Look for decimal patterns like "0.5", "0,75"
            var decimalMatches = System.Text.RegularExpressions.Regex.Matches(description, @"0[.,]\d+");
            if (decimalMatches.Count > 0)
            {
                return true; // Any decimal starting with 0 indicates less than 1 liter
            }
        }

        return false;
    }
}
