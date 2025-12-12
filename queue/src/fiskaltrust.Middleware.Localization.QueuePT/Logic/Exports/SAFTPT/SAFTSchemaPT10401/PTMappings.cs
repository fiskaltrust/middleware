using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

public static class PTMappings
{
    // https://taxfoundation.org/data/all/eu/value-added-tax-2024-vat-rates-europe/
    public static string GetIVATAxCode(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.Vat() switch
    {
        ChargeItemCase.UnknownService => throw new NotImplementedException("There is no unkown rate in Portugal"),
        ChargeItemCase.DiscountedVatRate1 => "RED",
        ChargeItemCase.DiscountedVatRate2 => "INT",
        ChargeItemCase.NormalVatRate => "NOR",
        ChargeItemCase.SuperReducedVatRate1 => throw new NotImplementedException("There is no super-reduced-1 rate in Portugal"),
        ChargeItemCase.SuperReducedVatRate2 => throw new NotImplementedException("There is no super-reduced-2 rate in Portugal"),
        ChargeItemCase.ParkingVatRate => throw new NotImplementedException("There is no parking vat rate in Portugal"),
        ChargeItemCase.ZeroVatRate => throw new NotImplementedException("There is no zero rate in Portugal"),
        ChargeItemCase.NotTaxable => "ISE",
        ChargeItemCase c => throw new NotImplementedException($"The given tax scheme 0x{c:X} is not supported in Portugal"),
    };

    public static string GetTaxExemptionCode(ChargeItem chargeItem)
    {
        var taxExemptCode = (int) chargeItem.ftChargeItemCase.NatureOfVat();
        if(Constants.TaxExemptionDictionary.TaxExemptionTable.ContainsKey((Constants.TaxExemptionCode) taxExemptCode))
        {
            return Constants.TaxExemptionDictionary.TaxExemptionTable[((Constants.TaxExemptionCode) taxExemptCode)].Code;
        }
        return "";
    }

    public static string GetTaxExemptionReason(ChargeItem chargeItem)
    {
        var taxExemptCode = (int) chargeItem.ftChargeItemCase.NatureOfVat();
        if (Constants.TaxExemptionDictionary.TaxExemptionTable.ContainsKey((Constants.TaxExemptionCode) taxExemptCode))
        {
            return Constants.TaxExemptionDictionary.TaxExemptionTable[((Constants.TaxExemptionCode) taxExemptCode)].Mention;
        }
        return "";

    }

    public static string GetPaymentMecahnism(PayItem payItem) => payItem.ftPayItemCase.Case() switch
    {
        PayItemCase.UnknownPaymentType => "OU", // Unknown � Other means not mentioned
        PayItemCase.CashPayment => "NU", // Cash
        PayItemCase.NonCash => "OU", // Non Cash � Other means not mentioned
        PayItemCase.CrossedCheque => "CH", // Bank cheque
        PayItemCase.DebitCardPayment => "CD", // Debit Card
        PayItemCase.CreditCardPayment => "CC", // Credit Card
        PayItemCase.VoucherPaymentCouponVoucherByMoneyValue => "CO", // Voucher Gift cheque or gift card;
        PayItemCase.OnlinePayment => "OU", // Online payment � Other means not mentioned
        PayItemCase.LoyaltyProgramCustomerCardPayment => "OU", // Online payment � Other means not mentioned
        _ => "OU", // Other � Other means not mentioned
    };

    public static string GetProductType(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.TypeOfService() switch
    {
        ChargeItemCaseTypeOfService.UnknownService => "O", // Unknown type of service / - Others (e.g. charged freights, advance payments received or sale of assets);
        ChargeItemCaseTypeOfService.Delivery => "P", // Delivery (supply of goods) / Products
        ChargeItemCaseTypeOfService.OtherService => "S", // Other service (supply of service) / Services
        ChargeItemCaseTypeOfService.Tip => "S", // Tip / Services
        ChargeItemCaseTypeOfService.Voucher => "O", // Voucher / ???
        ChargeItemCaseTypeOfService.CatalogService => "O", // Catalog Service / Services
        ChargeItemCaseTypeOfService.NotOwnSales => "O", // Not own sales Agency busines / ???
        ChargeItemCaseTypeOfService.OwnConsumption => "O", // Own Consumption / ???
        ChargeItemCaseTypeOfService.Grant => "O", // Grant / ???
        ChargeItemCaseTypeOfService.Receivable => "O", // Receivable / ???
        ChargeItemCaseTypeOfService.CashTransfer => "O", // Receivable / ???
        _ => throw new NotImplementedException($"The given ChargeItemCase {chargeItem.ftChargeItemCase} type is not supported"),
    };
}