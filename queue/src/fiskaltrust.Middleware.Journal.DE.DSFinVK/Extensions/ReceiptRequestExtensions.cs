using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions
{
    public static class ReceiptRequestExtensions
    {
        public static string GetBonType(this ReceiptRequest request)
        {
            if ((request.ftReceiptCase & 0x0000000000020000) == 0x0000000000020000)
            {
                return DSFinVKConstants.BonTypes.OtherActionTraining;
            }
            
            return (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => DSFinVKConstants.BonTypes.Receipt,
                0x0001 => DSFinVKConstants.BonTypes.Receipt,
                0x0002 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0003 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0004 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0005 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0006 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0007 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0008 => request.cbChargeItems?.Count() > 0 ? DSFinVKConstants.BonTypes.OtherActionOrder : DSFinVKConstants.BonTypes.OtherActionElse,
                0x0009 => request.cbPayItems?.Count() > 0 ? DSFinVKConstants.BonTypes.Receipt : DSFinVKConstants.BonTypes.OtherActionOrder,
                0x000A => request.cbPayItems?.Count() > 0 ? DSFinVKConstants.BonTypes.Receipt : DSFinVKConstants.BonTypes.OtherActionOrder,
                0x000B => DSFinVKConstants.BonTypes.OtherActionFailed,
                0x000C => DSFinVKConstants.BonTypes.Receipt,
                0x000D => DSFinVKConstants.BonTypes.Receipt,
                0x000E => DSFinVKConstants.BonTypes.OtherActionInvoice,
                0x000F => DSFinVKConstants.BonTypes.OtherActionTransfer,
                0x0010 => (request.cbPayItems ?? Array.Empty<PayItem>()).Any() ? DSFinVKConstants.BonTypes.OtherActionOrder : DSFinVKConstants.BonTypes.OtherActionElse,
                0x0011 => DSFinVKConstants.BonTypes.Receipt,
                0x0012 => DSFinVKConstants.BonTypes.OtherActionConsumption,
                0x0013 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0014 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0015 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0016 => DSFinVKConstants.BonTypes.OtherActionVoided,
                0x0017 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x0018 => DSFinVKConstants.BonTypes.OtherActionElse,
                0x001A => DSFinVKConstants.BonTypes.OtherActionFailed,
                _ => DSFinVKConstants.BonTypes.Receipt
            };
        }

        public static string GetTseProcessType(this ReceiptRequest request)
        {
            return (request.ftReceiptCase & 0xFFFF) switch
            {
                0x0000 => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x0001 => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x0002 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0003 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0004 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0005 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0006 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0007 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0008 => DSFinVKConstants.ProcessTypes.Empty,
                0x0009 => DSFinVKConstants.ProcessTypes.Empty,
                0x000A => DSFinVKConstants.ProcessTypes.Empty,
                0x000B => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x000C => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x000D => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x000E => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x000F => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x0010 => (request.cbPayItems ?? Array.Empty<PayItem>()).Any() ? DSFinVKConstants.ProcessTypes.KassenbelegV1 : DSFinVKConstants.ProcessTypes.BestellungV1,
                0x0011 => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x0012 => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x0013 => (request.cbChargeItems ?? Array.Empty<ChargeItem>()).Any() && (request.cbPayItems ?? Array.Empty<PayItem>()).Any() ? DSFinVKConstants.ProcessTypes.KassenbelegV1 : DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0014 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0015 => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x0016 => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                0x0017 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x0018 => DSFinVKConstants.ProcessTypes.SonstigerVorgang,
                0x001A => DSFinVKConstants.ProcessTypes.KassenbelegV1,
                _ => DSFinVKConstants.ProcessTypes.KassenbelegV1
            };
        }

        /// <summary>
        /// DSFinV-K Anhang B: all Vorgangstypen starting with "AV" (except AVTraining) may only use the Zahlart "Keine".
        /// </summary>
        public static bool IsPaymentTypeNoneOnly(this ReceiptRequest request)
        {
            var bonType = request.GetBonType();
            return bonType.StartsWith("AV", StringComparison.Ordinal) && bonType != DSFinVKConstants.BonTypes.OtherActionTraining;
        }

        /// <summary>
        /// DSFinV-K 2.4 Anhang B/I: an aborted transaction (AbortReceipt 0x001A -> AVBelegabbruch) is signed as
        /// "AVBelegabbruch^0.00_0.00_0.00_0.00_0.00^", so Bonkopf and Bonkopf_USt are 0.00. The entered positions were never
        /// signed (StartTransaction has empty processData), so they are exported with P_STORNO = 1 (Tz. 4.2.3).
        /// </summary>
        public static bool IsAbortedTransaction(this ReceiptRequest request) => (request.ftReceiptCase & 0xFFFF) == 0x001A;

        public static bool IsReceiptProcessType(this ReceiptRequest request) => request.GetTseProcessType() == DSFinVKConstants.ProcessTypes.KassenbelegV1;

        public static DateTime GetReceiptActionStartMoment(this ReceiptRequest request)
        {
            static DateTime GetMinDateTime(IEnumerable<DateTime?> dts)
            {
                return dts == null || !dts.Any()
                    ? DateTime.UtcNow
                    : dts.Select(x => x ?? DateTime.UtcNow).Min();
            }

            return (new DateTime[] {
                GetMinDateTime(request.cbChargeItems?.Select(x => x.Moment)),
                GetMinDateTime(request.cbPayItems?.Select(x => x.Moment)),
                request.cbReceiptMoment
            }).Min();
        }
    }
}
