﻿using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

public static class PTMappings
{
    public static string GetWorkType(ReceiptRequest receiptRequest)
    {
        return receiptRequest.ftReceiptCase.Case() switch
        {
            _ => "PF"
        };
    }

    public static string GetPaymentType(ReceiptRequest receiptRequest)
    {
        return receiptRequest.ftReceiptCase.Case() switch
        {
            _ => "RG"
        };
    }

    public static string GetInvoiceType(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001) && receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            return "NC";
        }

        return receiptRequest.ftReceiptCase.Case() switch
        {
            ReceiptCase.UnknownReceipt0x0000 => "FS",
            ReceiptCase.PointOfSaleReceipt0x0001 => "FS",
            ReceiptCase.PaymentTransfer0x0002 => "FS",
            ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 => "FS",
            ReceiptCase.ECommerce0x0004 => "FS",
            ReceiptCase.DeliveryNote0x0005 => "FS", // no invoicetype.. workign document?
            ReceiptCase.InvoiceUnknown0x1000 => "FT",
            ReceiptCase.InvoiceB2C0x1001 => "FT",
            ReceiptCase.InvoiceB2B0x1002 => "FT",
            ReceiptCase.InvoiceB2G0x1003 => "FT",
            _ => "FS"
        };
    }

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

    public static string GetTaxExemptionCode(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.NatureOfVat() switch
    {
        _ => "M16",
    };

    public static string GetTaxExemptionReason(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.NatureOfVat() switch
    {
        _ => "Isento Artigo 14.  do RITI (ou similar)",
    };

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
        ChargeItemCaseTypeOfService.Voucher => "?", // Voucher / ???
        ChargeItemCaseTypeOfService.CatalogService => "S", // Catalog Service / Services
        ChargeItemCaseTypeOfService.NotOwnSales => "?", // Not own sales Agency busines / ???
        ChargeItemCaseTypeOfService.OwnConsumption => "?", // Own Consumption / ???
        ChargeItemCaseTypeOfService.Grant => "?", // Grant / ???
        ChargeItemCaseTypeOfService.Receivable => "?", // Receivable / ???
        ChargeItemCaseTypeOfService.CashTransfer => "?", // Receivable / ???
        _ => throw new NotImplementedException($"The given ChargeItemCase {chargeItem.ftChargeItemCase} type is not supported"),
    };
}