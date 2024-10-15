using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Constants;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.Localization.QueueDE.Transactions
{
    public class DSFinVKTransactionPayloadFactory : ITransactionPayloadFactory
    {
        private readonly ILogger<DSFinVKTransactionPayloadFactory> _logger;

        public DSFinVKTransactionPayloadFactory(ILogger<DSFinVKTransactionPayloadFactory> logger)
        {
            _logger = logger;
        }

        public (string processType, string payload) CreateReceiptPayload(ReceiptRequest receiptRequest)
        {
            _logger.LogTrace("DSFinVKTransactionPayloadFactory.CreateReceiptPayload [enter].");
            var processType = receiptRequest.GetTseProcessType();
            string payload;
            if (processType == DSFinVKConstants.PROCESS_TYPE_EMPTY)
            {
                payload = string.Empty;
            }
            else if (processType == DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1)
            {
                var transactionType = receiptRequest.GetReceiptTransactionType();
                var taxes = receiptRequest.GetReceiptTaxes();
                var payments = GetReceiptPayments(receiptRequest);
                payload = $"{transactionType}^{taxes}^{payments}";
            }
            else if (processType == DSFinVKConstants.PROCESS_TYPE_BESTELLUNG_V1)
            {
                var payloadStringBuilder = new StringBuilder();
                foreach (var item in receiptRequest.cbChargeItems ?? Enumerable.Empty<ChargeItem>())
                {
                    if (item.Quantity == 0 && item.Amount != 0)
                    {
                        throw new ArgumentException("The quantity property of charge item entries must not be 0.00, unless the amount is 0.00 as well.");
                    }

                    var amountPerItem = item.Quantity != 0
                        ? item.Amount / item.Quantity
                        : 0;
                    
                    var itemquantity = item.Amount < 0  && item.Quantity > 0 ? item.Quantity * -1 : item.Quantity;
                    payloadStringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.##########}", itemquantity);
                    payloadStringBuilder.Append(";\"");
                    payloadStringBuilder.Append(item.Description.Replace("\"", "\"\""));
                    payloadStringBuilder.Append("\";");
                    payloadStringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.00}", Math.Abs(amountPerItem));

                    if (item != receiptRequest.cbChargeItems.Last())
                    {
                        payloadStringBuilder.Append(DSFinVKConstants.Delimiters.ORDER_LINE);
                    }
                }
                payload = payloadStringBuilder.ToString();
            }
            else if (processType == DSFinVKConstants.PROCESS_TYPE_SONSTIGER_VORGANG)
            {
                payload = receiptRequest.GetDescription();
            }
            else
            {
                throw new NotImplementedException($"The given processType {processType} is not yet supported.");
            }
            _logger.LogTrace("DSFinVKTransactionPayloadFactory.CreateReceiptPayload [exit].");
            return (processType, payload);
        }

        public (string processType, string payload) CreateAutomaticallyCanceledReceiptPayload()
        {
            _logger.LogTrace("DSFinVKTransactionPayloadFactory.CreateAutomaticallyCanceledReceiptPayload [enter].");
            // Create an empty request to re-use the logic for constructing taxes and payments
            var emptyReceiptRequest = new ReceiptRequest { cbChargeItems = Array.Empty<ChargeItem>(), cbPayItems = Array.Empty<PayItem>() };

            var taxes = emptyReceiptRequest.GetReceiptTaxes();
            var payments = GetReceiptPayments(emptyReceiptRequest);
            var payload = $"{DSFinVKConstants.BON_TYP_OTHERACTION_FAILED}^{taxes}^{payments}";

            _logger.LogTrace("DSFinVKTransactionPayloadFactory.CreateAutomaticallyCanceledReceiptPayload [exit].");
            return (DSFinVKConstants.PROCESS_TYPE_KASSENBELEG_V1, payload);
        }

        private string GetReceiptPayments(ReceiptRequest request)
        {
            const string currencyCodeKey = "CurrencyCode";
            const string foreignCurrecyAmountKey = "ForeignCurrencyAmount";

            var paymentDict = new ConcurrentDictionary<string, decimal>();

            foreach (var item in request.cbPayItems ?? Array.Empty<PayItem>())
            {
                switch (item.ftPayItemCase & 0xFFFF)
                {
                    case 0x000A:
                    case 0x000D:
                    case 0x000E:
                    case 0x000F:
                    case 0x0010:
                    case 0x0011:
                    case 0x0012:
                    case 0x0013:
                    case 0x0014:
                    case 0x0015:
                    case 0x0016:
                    case 0x0017:
                    {
                        // not included in payments.
                        break;
                    }
                    case 0x0002:
                    case 0x000C:
                    {
                        if (string.IsNullOrEmpty(item.ftPayItemCaseData))
                        {
                            throw new ArgumentException($"ftPayItemCaseData must not be empty when using ftPayItemCase {item.ftPayItemCase:x}.");
                        }

                        var payItemCaseData = JObject.Parse(item.ftPayItemCaseData);
                        if (!payItemCaseData.ContainsKey(foreignCurrecyAmountKey) || !payItemCaseData.ContainsKey(currencyCodeKey))
                        {
                            throw new ArgumentException($"ftPayItemCaseData must contain a JSON object with '{foreignCurrecyAmountKey}' and '{currencyCodeKey}' when using ftPayItemCase {item.ftPayItemCase:x}.");
                        }

                        var foreignAmount = payItemCaseData.Value<decimal>(foreignCurrecyAmountKey);
                        foreignAmount = CalculationHelper.ReviseAmountOnNegativeQuantity(item.Quantity, foreignAmount);
                        paymentDict.AddOrUpdate($"{DSFinVKConstants.PROCESS_DATA_PAYMENT_CASH_TEXT}:{payItemCaseData.Value<string>(currencyCodeKey)}", foreignAmount, (k, v) => v + foreignAmount);
                        break;
                    }
                    case 0x0003:
                    case 0x0004:
                    case 0x0005:
                    case 0x0006:
                    case 0x0007:
                    case 0x0008:
                    case 0x0009:
                    {
                        var currency = string.Empty;
                        var amount = item.Amount;
                        if (!string.IsNullOrEmpty(item.ftPayItemCaseData))
                        {
                            var payItemCaseData = JObject.Parse(item.ftPayItemCaseData);
                            if (payItemCaseData.ContainsKey(foreignCurrecyAmountKey) && payItemCaseData.ContainsKey(currencyCodeKey) && payItemCaseData.Value<string>(currencyCodeKey).ToLower() != "eur")
                            {
                                // read foreign currency code and amount
                                currency = payItemCaseData.Value<string>(currencyCodeKey);
                                amount = payItemCaseData.Value<decimal>(foreignCurrecyAmountKey);
                            }
                        }
                        amount = CalculationHelper.ReviseAmountOnNegativeQuantity(item.Quantity, amount);
                        paymentDict.AddOrUpdate(currency.Length == 0 ? DSFinVKConstants.PROCESS_DATA_PAYMENT_NON_CASH_TEXT : $"{DSFinVKConstants.PROCESS_DATA_PAYMENT_NON_CASH_TEXT}:{currency}", amount, (k, v) => v + amount);
                        break;
                    }
                    case 0x0000:
                    case 0x0001:
                    case 0x000B:
                    default:
                    {
                        var amount = CalculationHelper.ReviseAmountOnNegativeQuantity(item.Quantity, item.Amount);
                        paymentDict.AddOrUpdate(DSFinVKConstants.PROCESS_DATA_PAYMENT_CASH_TEXT, amount, (k, v) => v + amount);
                        break;
                    }
                }
            }
            var paymentList = paymentDict.Where(i => i.Value != 0.0m).OrderBy(i => i.Key).Select(i => string.Format(CultureInfo.InvariantCulture, "{0:0.00}", i.Value)+ ":"+ i.Key);
            return string.Join("_", paymentList);
        }
    }
}
