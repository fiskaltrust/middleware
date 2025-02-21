using System;
using System.Linq;

namespace fiskaltrust.Middleware.Interface.Client.Extensions
{
    public static class ModelConversionExtensions
    {
        private static T[] ConvertArray<U, T>(U[] from, Func<U, T> converter)
        {
            if (from == null) return null;

            return from.Select(item => converter(item)).ToArray();
        }

        public static ifPOS.v0.ReceiptRequest Into(this ifPOS.v1.ReceiptRequest from)
        {
            return new ifPOS.v0.ReceiptRequest()
            {
                ftCashBoxID = from.ftCashBoxID,
                ftQueueID = from.ftQueueID,
                ftPosSystemId = from.ftPosSystemId,
                cbTerminalID = from.cbTerminalID,
                cbReceiptReference = from.cbReceiptReference,
                cbReceiptMoment = from.cbReceiptMoment,
                cbChargeItems = ConvertArray(from.cbChargeItems, i => i.Into()),
                cbPayItems = ConvertArray(from.cbPayItems, i => i.Into()),
                ftReceiptCase = from.ftReceiptCase,
                ftReceiptCaseData = from.ftReceiptCaseData,
                cbReceiptAmount = from.cbReceiptAmount,
                cbUser = from.cbUser,
                cbArea = from.cbArea,
                cbCustomer = from.cbCustomer,
                cbSettlement = from.cbSettlement,
                cbPreviousReceiptReference = from.cbPreviousReceiptReference,
            };
        }

        public static ifPOS.v0.ChargeItem Into(this ifPOS.v1.ChargeItem from)
        {
            return new ifPOS.v0.ChargeItem()
            {
                Position = from.Position,
                Quantity = from.Quantity,
                Description = from.Description,
                Amount = from.Amount,
                VATRate = from.VATRate,
                ftChargeItemCase = from.ftChargeItemCase,
                ftChargeItemCaseData = from.ftChargeItemCaseData,
                VATAmount = from.VATAmount,
                AccountNumber = from.AccountNumber,
                CostCenter = from.CostCenter,
                ProductGroup = from.ProductGroup,
                ProductNumber = from.ProductNumber,
                ProductBarcode = from.ProductBarcode,
                Unit = from.Unit,
                UnitQuantity = from.UnitQuantity,
                UnitPrice = from.UnitPrice,
                Moment = from.Moment,
            };
        }

        public static ifPOS.v0.PayItem Into(this ifPOS.v1.PayItem from)
        {
            return new ifPOS.v0.PayItem()
            {
                Position = from.Position,
                Quantity = from.Quantity,
                Description = from.Description,
                Amount = from.Amount,
                ftPayItemCase = from.ftPayItemCase,
                ftPayItemCaseData = from.ftPayItemCaseData,
                AccountNumber = from.AccountNumber,
                CostCenter = from.CostCenter,
                MoneyGroup = from.MoneyGroup,
                MoneyNumber = from.MoneyNumber,
                Moment = from.Moment,
            };
        }

        public static ifPOS.v1.ReceiptResponse Into(this ifPOS.v0.ReceiptResponse from)
        {
            return new ifPOS.v1.ReceiptResponse()
            {
                ftCashBoxID = from.ftCashBoxID,
                ftQueueID = from.ftQueueID,
                ftQueueItemID = from.ftQueueItemID,
                ftQueueRow = from.ftQueueRow,
                cbTerminalID = from.cbTerminalID,
                cbReceiptReference = from.cbReceiptReference,
                ftCashBoxIdentification = from.ftCashBoxIdentification,
                ftReceiptIdentification = from.ftReceiptIdentification,
                ftReceiptMoment = from.ftReceiptMoment,
                ftReceiptHeader = from.ftReceiptHeader,
                ftChargeItems = ConvertArray(from.ftChargeItems, i => i.Into()),
                ftChargeLines = from.ftChargeLines,
                ftPayItems = ConvertArray(from.ftPayItems, i => i.Into()),
                ftPayLines = from.ftPayLines,
                ftSignatures = ConvertArray(from.ftSignatures, i => i.Into()),
                ftReceiptFooter = from.ftReceiptFooter,
                ftState = from.ftState,
                ftStateData = from.ftStateData,
            };
        }

        public static ifPOS.v1.ChargeItem Into(this ifPOS.v0.ChargeItem from)
        {
            return new ifPOS.v1.ChargeItem
            {
                Position = from.Position,
                Quantity = from.Quantity,
                Description = from.Description,
                Amount = from.Amount,
                VATRate = from.VATRate,
                ftChargeItemCase = from.ftChargeItemCase,
                ftChargeItemCaseData = from.ftChargeItemCaseData,
                VATAmount = from.VATAmount,
                AccountNumber = from.AccountNumber,
                CostCenter = from.CostCenter,
                ProductGroup = from.ProductGroup,
                ProductNumber = from.ProductNumber,
                ProductBarcode = from.ProductBarcode,
                Unit = from.Unit,
                UnitQuantity = from.UnitQuantity,
                UnitPrice = from.UnitPrice,
                Moment = from.Moment,
            };
        }

        public static ifPOS.v1.PayItem Into(this ifPOS.v0.PayItem from)
        {
            return new ifPOS.v1.PayItem()
            {
                Position = from.Position,
                Quantity = from.Quantity,
                Description = from.Description,
                Amount = from.Amount,
                ftPayItemCase = from.ftPayItemCase,
                ftPayItemCaseData = from.ftPayItemCaseData,
                AccountNumber = from.AccountNumber,
                CostCenter = from.CostCenter,
                MoneyGroup = from.MoneyGroup,
                MoneyNumber = from.MoneyNumber,
                Moment = from.Moment,
            };
        }

        public static ifPOS.v1.SignaturItem Into(this ifPOS.v0.SignaturItem from)
        {
            return new ifPOS.v1.SignaturItem()
            {
                ftSignatureFormat = from.ftSignatureFormat,
                ftSignatureType = from.ftSignatureType,
                Caption = from.Caption,
                Data = from.Data,
            };
        }
    }
}
