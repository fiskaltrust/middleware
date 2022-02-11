using System.Linq;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Queue
{
    public static class ReceiptRequestHelper
    {
        public static ReceiptRequest ConvertToV1(ifPOS.v0.ReceiptRequest data)
        {
            return new ReceiptRequest
            {
                cbArea = data.cbArea,
                cbCustomer = data.cbCustomer,
                cbPreviousReceiptReference = data.cbPreviousReceiptReference,
                cbReceiptAmount = data.cbReceiptAmount,
                cbReceiptMoment = data.cbReceiptMoment,
                cbReceiptReference = data.cbReceiptReference,
                cbSettlement = data.cbSettlement,
                cbTerminalID = data.cbTerminalID,
                cbUser = data.cbUser,
                ftCashBoxID = data.ftCashBoxID,
                ftPosSystemId = data.ftPosSystemId,
                ftQueueID = data.ftQueueID,
                ftReceiptCase = data.ftReceiptCase,
                ftReceiptCaseData = data.ftReceiptCaseData,
                cbChargeItems = data.cbChargeItems.Select(ConvertToV1).ToArray(),
                cbPayItems = data.cbPayItems.Select(ConvertToV1).ToArray(),
            };
        }

        public static ChargeItem ConvertToV1(ifPOS.v0.ChargeItem data)
        {
            return new ChargeItem
            {
                AccountNumber = data.AccountNumber,
                Amount = data.Amount,
                CostCenter = data.CostCenter,
                Description = data.Description,
                ftChargeItemCase = data.ftChargeItemCase,
                ftChargeItemCaseData = data.ftChargeItemCaseData,
                Moment = data.Moment,
                Position = data.Position,
                ProductBarcode = data.ProductBarcode,
                ProductGroup = data.ProductGroup,
                ProductNumber = data.ProductNumber,
                Quantity = data.Quantity,
                Unit = data.Unit,
                UnitPrice = data.UnitPrice,
                UnitQuantity = data.UnitQuantity,
                VATAmount = data.VATAmount,
                VATRate = data.VATRate
            };
        }

        public static PayItem ConvertToV1(ifPOS.v0.PayItem data)
        {
            return new PayItem
            {
                AccountNumber = data.AccountNumber,
                Amount = data.Amount,
                CostCenter = data.CostCenter,
                Description = data.Description,
                ftPayItemCase = data.ftPayItemCase,
                ftPayItemCaseData = data.ftPayItemCaseData,
                Moment = data.Moment,
                Position = data.Position,
                MoneyGroup = data.MoneyGroup,
                MoneyNumber = data.MoneyNumber,
                Quantity = data.Quantity
            };
        }

        public static ifPOS.v0.ReceiptResponse ConvertToV0(ReceiptResponse data)
        {
            return new ifPOS.v0.ReceiptResponse
            {
                ftCashBoxID = data.ftCashBoxID,
                cbReceiptReference = data.cbReceiptReference,
                cbTerminalID = data.cbTerminalID,
                ftCashBoxIdentification = data.ftCashBoxIdentification,
                ftChargeLines = data.ftChargeLines,
                ftPayLines = data.ftPayLines,
                ftQueueID = data.ftQueueID,
                ftQueueItemID = data.ftQueueItemID,
                ftQueueRow = data.ftQueueRow,
                ftReceiptFooter = data.ftReceiptFooter,
                ftReceiptHeader = data.ftReceiptHeader,
                ftReceiptIdentification = data.ftReceiptIdentification,
                ftReceiptMoment = data.ftReceiptMoment,
                ftState = data.ftState,
                ftStateData = data.ftStateData,
                ftChargeItems = data.ftChargeItems?.Select(ConvertToV0).ToArray(),
                ftPayItems = data.ftPayItems?.Select(ConvertToV0).ToArray(),
                ftSignatures = data.ftSignatures?.Select(ConvertToV0).ToArray(),
            };
        }

        public static ifPOS.v0.SignaturItem ConvertToV0(SignaturItem data)
        {
            return new ifPOS.v0.SignaturItem
            {
                Caption = data.Caption,
                Data = data.Data,
                ftSignatureFormat = data.ftSignatureFormat,
                ftSignatureType = data.ftSignatureType
            };
        }

        public static ifPOS.v0.ChargeItem ConvertToV0(ChargeItem data)
        {
            return new ifPOS.v0.ChargeItem
            {
                AccountNumber = data.AccountNumber,
                Amount = data.Amount,
                CostCenter = data.CostCenter,
                Description = data.Description,
                ftChargeItemCase = data.ftChargeItemCase,
                ftChargeItemCaseData = data.ftChargeItemCaseData,
                Moment = data.Moment,
                Position = data.Position,
                ProductBarcode = data.ProductBarcode,
                ProductGroup = data.ProductGroup,
                ProductNumber = data.ProductNumber,
                Quantity = data.Quantity,
                Unit = data.Unit,
                UnitPrice = data.UnitPrice,
                UnitQuantity = data.UnitQuantity,
                VATAmount = data.VATAmount,
                VATRate = data.VATRate
            };
        }

        public static ifPOS.v0.PayItem ConvertToV0(PayItem data)
        {
            return new ifPOS.v0.PayItem
            {
                AccountNumber = data.AccountNumber,
                Amount = data.Amount,
                CostCenter = data.CostCenter,
                Description = data.Description,
                ftPayItemCase = data.ftPayItemCase,
                ftPayItemCaseData = data.ftPayItemCaseData,
                Moment = data.Moment,
                Position = data.Position,
                MoneyGroup = data.MoneyGroup,
                MoneyNumber = data.MoneyNumber,
                Quantity = data.Quantity
            };
        }

        public static string GetRequestVersion(ReceiptRequest data)
        {
            if (data.GetType() == typeof(ifPOS.v0.ReceiptRequest))
            {
                //TODO change on version update
                return "v0";
            }
            else
            {
                var type = data.GetType().ToString();
                var start = type.IndexOf(".ifPOS.") + 7;
                var end = type.LastIndexOf('.');
                if (start >= 7 && end >= 0)
                {
                    return type.Substring(start, end - start);
                }
                else
                {
                    return $"unknown:{type}";
                }
            }
        }

        public static string GetCountry(ReceiptRequest data)
        {
            return (0xFFFF000000000000 & (ulong) data.ftReceiptCase) switch
            {
                0x4445000000000000 => "DE",
                0x4652000000000000 => "FR",
                _ => "AT",
            };
        }
    }
}
