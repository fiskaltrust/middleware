using System;
using System.Linq;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Journal.DE.Common.Extensions
{
    public static class ReceiptRequestExtensions
    {
        private static readonly long[] _excludedForReference = {0x0002, //Zero-receipt
                                                         0x0003, //Initial operation receipt    
                                                         0x0005, //Monthly-closing
                                                         0x0006, //Yearly-closing
                                                         0x0007  //Daily-closing
                                                        };

        public static bool IsDailyClosingReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0xFFFF) == 0x0007;

        public static bool IsZeroReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0xFFFF) == 0x0002;

        public static bool IsVoidReceipt(this ReceiptRequest request) => (request.ftReceiptCase & 0x40000) == 0x40000;

        public static bool IncludeInReferences(this ReceiptRequest request) => !_excludedForReference.Contains(request.ftReceiptCase & 0xFFFF);

        public static bool IsExplicitStartUpdateOrDeltaTransaction(this ReceiptRequest request)
        {
            var isImplicit = (request.ftReceiptCase & 0x1_0000_0000) == 0x1_0000_0000;
            var isStartTransaction = (request.ftReceiptCase & 0xFFFF) == 0x0008;
            var isUpdateTransaction = (request.ftReceiptCase & 0xFFFF) == 0x0009;
            var isDeleteTransaction = (request.ftReceiptCase & 0xFFFF) == 0x000A;
            return !isImplicit && (isStartTransaction || isUpdateTransaction || isDeleteTransaction);
        }

        public static bool IsErrorState(this ReceiptResponse receiptResponse) => (receiptResponse.ftState & 0xEEEE_EEEE) == 0xEEEE_EEEE;
        public static string GetDescription(this ReceiptRequest request)
        {
            return (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => "unknown ",
                0x0001 => "pos-receipt",
                0x0002 => "zero-receipt",
                0x0003 => "initial operation receipt / start-receipt",
                0x0004 => "out of operation receipt / stop-receipt",
                0x0005 => "monthly-closing",
                0x0006 => "yearly-closing",
                0x0007 => "daily-closing",
                0x0008 => "start-transaction-receipt",
                0x0009 => "update-transaction-receipt",
                0x000A => "delta-transaction-receipt",
                0x000B => "fail-transaction-receipt",
                0x000C => "b2b-invoice",
                0x000D => "b2c-invoice",
                0x000E => "info-invoice",
                0x000F => "info-delivery-note",
                0x0010 => "info-order",
                0x0011 => "cash deposit / cash pay-in / cash pay-out / exchange",
                0x0012 => "material consumption",
                0x0013 => "info-internal",
                0x0014 => "protocol",
                0x0015 => "foreign sales",
                0x0016 => "void-receipt",
                0x0017 => "initiate-scu-switch",
                0x0018 => "finish-scu-switch",
                0x001A => "abort-receipt",
                _ => null
            };
        }
    }
}
