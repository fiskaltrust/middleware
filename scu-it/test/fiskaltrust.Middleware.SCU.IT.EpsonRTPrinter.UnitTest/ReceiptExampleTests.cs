using System;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class ReceiptExampleTests
    {
        [Fact]
        public void Test1()
        {
            var receipt = $$"""
{
  "ftCashBoxID": "4038ca6d-fe63-46d0-95d6-a1fce0f98258",
  "ftQueueID": "910347dc-a5fc-44bf-9ef0-2ec5fda824ca",
  "ftPosSystemId": "e0c014d5-44de-4eec-886f-02dde5ec2d3a",
  "cbTerminalID": "1",
  "cbReceiptReference": "f64afd38-91be-44fc-8625-2fba33c22004",
  "cbReceiptMoment": "2024-10-02T07:01:46.993Z",
  "cbChargeItems": [
    {
      "Position": 100,
      "Quantity": 0.3330,
      "Description": "Americano",
      "Amount": 0.99900000000000000000000000,
      "VATRate": 22.0000,
      "ftChargeItemCase": 5283883447184523283,
      "ftChargeItemCaseData": "",
      "VATAmount": 0.1801475409836065573770491803,
      "CostCenter": "4",
      "ProductGroup": "Warme Getränke",
      "ProductNumber": "1019",
      "ProductBarcode": "",
      "Unit": "Stk",
      "Moment": "2024-10-02T07:01:36.867Z"
    },
    {
      "Position": 200,
      "Quantity": 1.0000,
      "Description": "Espresso",
      "Amount": 1.60000000000000000000000000,
      "VATRate": 22.0000,
      "ftChargeItemCase": 5283883447184523283,
      "ftChargeItemCaseData": "",
      "VATAmount": 0.2885245901639344262295081967,
      "CostCenter": "4",
      "ProductGroup": "Warme Getränke",
      "ProductNumber": "1001",
      "ProductBarcode": "",
      "Unit": "Stk",
      "Moment": "2024-09-20T09:17:59.667Z"
    },
    {
      "Position": 300,
      "Quantity": 0.3330,
      "Description": "Glühmix",
      "Amount": 1.16550000000000000000000000,
      "VATRate": 22.0000,
      "ftChargeItemCase": 5283883447184523283,
      "ftChargeItemCaseData": "",
      "VATAmount": 0.210172131147540983606557377,
      "CostCenter": "4",
      "ProductGroup": "Warme Getränke",
      "ProductNumber": "1015",
      "ProductBarcode": "",
      "Unit": "Stk",
      "Moment": "2024-09-17T08:52:01.367Z"
    },
    {
      "Position": 400,
      "Quantity": 0.3330,
      "Description": "Glühwein",
      "Amount": 1.16550000000000000000000000,
      "VATRate": 22.0000,
      "ftChargeItemCase": 5283883447184523283,
      "ftChargeItemCaseData": "",
      "VATAmount": 0.210172131147540983606557377,
      "CostCenter": "4",
      "ProductGroup": "Warme Getränke",
      "ProductNumber": "1016",
      "ProductBarcode": "",
      "Unit": "Stk",
      "Moment": "2024-10-02T07:01:37.177Z"
    }
  ],
  "cbPayItems": [
    {
      "Quantity": 1.0,
      "Description": "Bar",
      "Amount": 4.9300,
      "ftPayItemCase": 5283883447184523265,
      "ftPayItemCaseData": "",
      "CostCenter": "4",
      "MoneyGroup": "1",
      "MoneyNumber": ""
    }
  ],
  "ftReceiptCase": 5283883447318740993,
  "cbReceiptAmount": 4.93,
  "cbUser": "Chef",
  "cbArea": "50"
}
""";
            var receiptDataDeserialized = JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
            var content = EpsonCommandFactory.CreateInvoiceRequestContent(new EpsonRTPrinterSCUConfiguration { }, receiptDataDeserialized);
            var sumChargeItems = content.ItemAndMessages.Select(x => x.PrintRecItem).Sum(x => x.Quantity * x.UnitPrice);
            
            var xml = SoapSerializer.Serialize(content);
            Console.WriteLine(xml);
        }
    }
}