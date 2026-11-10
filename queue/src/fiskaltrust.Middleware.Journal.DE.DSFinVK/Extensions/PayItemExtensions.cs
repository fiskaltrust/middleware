using System;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions
{
    public static class PayItemExtensions
    {
        public static bool HasForeignCurrency(this PayItem payItem, PayItemCaseData payItemCase) => payItemCase?.CurrencyCode != null && payItemCase.BaseCurrencyAmount.HasValue;

        public static string GetBusinessCaseType(this PayItem payItem, ReceiptRequest request)
        {
            if (!payItem.MustBeTransformedToBusinessCase())
            {
                throw new ArgumentException($"The given Pay Item must not be transformed to a business case (ftPayItemCase: {payItem.ftPayItemCase}).");
            }

            return (payItem.ftPayItemCase & 0xFFFF) switch
            {
                0x000A => DSFinVKConstants.BusinessCaseTypes.Revenue,
                0x000D => ReverseIfVoidReceipt(request, payItem.Amount < 0, DSFinVKConstants.BusinessCaseTypes.VoucherPurchase, DSFinVKConstants.BusinessCaseTypes.VoucherRedemption),
                0x000E => ReverseIfVoidReceipt(request, payItem.Amount < 0, DSFinVKConstants.BusinessCaseTypes.ReceivableReduction, DSFinVKConstants.BusinessCaseTypes.ReceivableCreation),
                0x000F => ReverseIfVoidReceipt(request, payItem.Amount < 0, DSFinVKConstants.BusinessCaseTypes.DownPaymentCreation, DSFinVKConstants.BusinessCaseTypes.DownPaymentReduction),
                0x0010 => DSFinVKConstants.BusinessCaseTypes.TipEmployee,
                0x0011 => DSFinVKConstants.BusinessCaseTypes.GrantReal,
                0x0012 => DSFinVKConstants.BusinessCaseTypes.CashTransferToEmptyTill,
                0x0013 => ReverseIfVoidReceipt(request, payItem.Amount < 0, DSFinVKConstants.BusinessCaseTypes.CashTransferFromOwner, DSFinVKConstants.BusinessCaseTypes.CashTransferToOwner),
                0x0014 => DSFinVKConstants.BusinessCaseTypes.CashTransferFromOrToTill,
                0x0015 => DSFinVKConstants.BusinessCaseTypes.CashTransferToEmployee,
                0x0016 => ReverseIfVoidReceipt(request, payItem.Amount < 0, DSFinVKConstants.BusinessCaseTypes.CashTransferToCashBook, DSFinVKConstants.BusinessCaseTypes.CashTransferFromCashbook),
                0x0017 => DSFinVKConstants.BusinessCaseTypes.CashAmountDifference,
                _ => null
            };
        }

        private static string ReverseIfVoidReceipt(ReceiptRequest request, bool condition, string resultIfTrue, string resultIfFalse)
        {
            return !request.IsVoidReceipt()
                ? condition ? resultIfTrue : resultIfFalse
                : !condition ? resultIfTrue : resultIfFalse;
        }

        public static string GetPaymentType(this PayItem payItem)
        {
            return (payItem.ftPayItemCase & 0xFFFF) switch
            {
                0x0000 => DSFinVKConstants.PaymentTypes.Cash,
                0x0001 => DSFinVKConstants.PaymentTypes.Cash,
                0x0002 => DSFinVKConstants.PaymentTypes.Cash,
                0x0003 => DSFinVKConstants.PaymentTypes.NonCash,
                0x0004 => DSFinVKConstants.PaymentTypes.EcCard,
                0x0005 => DSFinVKConstants.PaymentTypes.CreditCard,
                0x0006 => DSFinVKConstants.PaymentTypes.ElectronicPaymentProvider,
                0x0007 => DSFinVKConstants.PaymentTypes.PrepaidCard,
                0x0008 => DSFinVKConstants.PaymentTypes.NonCash,
                0x0009 => DSFinVKConstants.PaymentTypes.NonCash,
                0x000B => DSFinVKConstants.PaymentTypes.Cash,
                0x000C => DSFinVKConstants.PaymentTypes.Cash,
                _ => null
            };
        }

        public static bool IsCashPaymentType(this PayItem payItem) => payItem.GetPaymentType() == DSFinVKConstants.PaymentTypes.Cash;

        public static bool MustBeTransformedToBusinessCase(this PayItem payItem) => payItem.GetPaymentType() == null;

        public static decimal InverseAmount(this PayItem payItem) => payItem.Amount * -1;
    }
}
