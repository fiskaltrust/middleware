using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.AcceptanceTests
{
    public static class ReceiptExamples
    {
        public static ReceiptRequest GetInitialOperation()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "0x4001_InitialOperation.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetOutOOperation()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "0x4002_OutOfOperation.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetZeroReceipt()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "0x2011_ZeroReceipt.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetDeliveryNoteWithCustomerData()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "PosReceipts", "0x0005_Cash_cbCustomer.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetCashReceiptWithTip()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "PosReceipts", "0x0001_Cash_Tip.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetCashWithSubItems()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "PosReceipts", "0x0001_Cash_withSubItems.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest Cash_withSubItemReduction()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "PosReceipts", "0x0001_Cash_withSubItemReduction.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetDailyClosing()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "0x2011_DailyClosing.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest CashWithMutliUseVoucherRedeem()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "PosReceipts", "0x0001_CashWithMutliUseVoucherRedeem.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest CashWithMutliUseVoucherPurchase()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "PosReceipts", "0x0001_CashWithMutliUseVoucherPurchase.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTicketExample()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "PosReceipts", "0x0001_CashWithTicket.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetReprintReceipt()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = File.ReadAllText(Path.Combine("ReceiptRequests", "0x3010_Reprint.json")).Replace("{{current_moment}}", current_moment);
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTakeAway_Delivery_Cash()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0002",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": 2.0,
            "Amount": 221,
            "UnitPrice": 110.5,
            "VATRate": 22,
            "VATAmount": 39.85,
            "Description": "TakeAway - Delivery - Item VAT 22%",
            "ftChargeItemCase": 5283883447186620435,
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 107,
            "VATRate": 10,
            "VATAmount": 9.73,
            "ftChargeItemCase": 5283883447186620433,
            "Description": "TakeAway - Delivery - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 88,
            "VATRate": 5,
            "VATAmount": 4.19,
            "ftChargeItemCase": 5283883447186620434,
            "Description": "TakeAway - Delivery - Item VAT 5%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 90,
            "VATRate": 4,
            "VATAmount": 3.46,
            "ftChargeItemCase": 5283883447186620436,
            "Description": "TakeAway - Delivery - Item VAT 4%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186624532,
            "Description": "TakeAway - Delivery - Item VAT NI",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186628628,
            "Description": "TakeAway - Delivery - Item VAT NS",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186632724,
            "Description": "TakeAway - Delivery - Item VAT ES",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186636820,
            "Description": "TakeAway - Delivery - Item VAT RM",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186640916,
            "Description": "TakeAway - Delivery - Item VAT AL",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186653204,
            "Description": "TakeAway - Delivery - Item VAT EE",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Cash",
            "ftPayItemCase": 5283883447184523265,
            "Moment": "{{current_moment}}",
            "Amount": 566
        }
    ],
    "ftReceiptCase": 5283883447184523265
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTakeAway_Delivery_Refund()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0004",
    "cbPreviousReceiptReference": "96SRT900126,00010001;0001-0002;20230830120101",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": -2.0,
            "Amount": -221,
            "UnitPrice": 110.5,
            "VATRate": 22,
            "VATAmount": 39.85,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 22%",
            "ftChargeItemCase": 5283883447186751507,
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -107,
            "VATRate": 10,
            "VATAmount": 9.73,
            "ftChargeItemCase": 5283883447186751505,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -88,
            "VATRate": 5,
            "VATAmount": 4.19,
            "ftChargeItemCase": 5283883447186751506,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 5%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -90,
            "VATRate": 4,
            "VATAmount": 3.46,
            "ftChargeItemCase": 5283883447186751508,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 4%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186755604,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT NI",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186759700,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT NS",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186763796,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT ES",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186767892,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT RM",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186771988,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT AL",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186784276,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT EE",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Return/Refund Cash",
            "ftPayItemCase": 5283883447184654337,
            "Moment": "{{current_moment}}",
            "Amount": -566
        }
    ],
    "ftReceiptCase": 5283883447201300481
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTakeAway_Delivery_Void()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0004",
    "cbPreviousReceiptReference": "96SRT900126,00010001;0001-0002;20230830120101",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": -2.0,
            "Amount": -221,
            "UnitPrice": 110.5,
            "VATRate": 22,
            "VATAmount": 39.85,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 22%",
            "ftChargeItemCase": 5283883447186751507,
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -107,
            "VATRate": 10,
            "VATAmount": 9.73,
            "ftChargeItemCase": 5283883447186751505,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -88,
            "VATRate": 5,
            "VATAmount": 4.19,
            "ftChargeItemCase": 5283883447186751506,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 5%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -90,
            "VATRate": 4,
            "VATAmount": 3.46,
            "ftChargeItemCase": 5283883447186751508,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT 4%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186755604,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT NI",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186759700,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT NS",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186763796,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT ES",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186767892,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT RM",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186771988,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT AL",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447186784276,
            "Description": "Return/Refund - TakeAway - Delivery - Item VAT EE",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Return/Refund Cash",
            "ftPayItemCase": 5283883447184654337,
            "Moment": "{{current_moment}}",
            "Amount": -566
        }
    ],
    "ftReceiptCase": 5283883447184785409
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTakeAway_Delivery_Card()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0003",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": 2.0,
            "Amount": 221,
            "UnitPrice": 110.5,
            "VATRate": 22,
            "Description": "TakeAway - Delivery - Item VAT 22%",
            "ftChargeItemCase": 5283883447186620435,
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883447186620433,
            "Description": "TakeAway - Delivery - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 88,
            "VATRate": 5,
            "ftChargeItemCase": 5283883447186620434,
            "Description": "TakeAway - Delivery - Item VAT 5%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 90,
            "VATRate": 4,
            "ftChargeItemCase": 5283883447186620436,
            "Description": "TakeAway - Delivery - Item VAT 4%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "ftChargeItemCase": 5283883447186624532,
            "Description": "TakeAway - Delivery - Item VAT NI",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "ftChargeItemCase": 5283883447186628628,
            "Description": "TakeAway - Delivery - Item VAT NS",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "ftChargeItemCase": 5283883447186632724,
            "Description": "TakeAway - Delivery - Item VAT ES",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "ftChargeItemCase": 5283883447186636820,
            "Description": "TakeAway - Delivery - Item VAT RM",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "ftChargeItemCase": 5283883447186640916,
            "Description": "TakeAway - Delivery - Item VAT AL",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "ftChargeItemCase": 5283883447186653204,
            "Description": "TakeAway - Delivery - Item VAT EE",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Card",
            "ftPayItemCase": 5283883447184523269,
            "Moment": "{{current_moment}}",
            "Amount": 566
        }
    ],
    "cbCustomer": "{\"CustomerVATId\": \"01606720215\"}",
    "ftReceiptCase": 5283883447184523265
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTakeAway_Delivery_Card_WithCustomerIva()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0003",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": 2.0,
            "Amount": 221,
            "UnitPrice": 110.5,
            "VATRate": 22,
            "Description": "TakeAway - Delivery - Item VAT 22%",
            "ftChargeItemCase": 5283883447186620435,
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Card",
            "ftPayItemCase": 5283883447184523269,
            "Moment": "{{current_moment}}",
            "Amount": 221
        }
    ],
    "cbCustomer": "{\"CustomerVATId\": \"IT01606720215\"}",
    "ftReceiptCase": 5283883447184523265
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTakeAway_Delivery_Card_WithInvalidCustomerIva()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0003",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": 2.0,
            "Amount": 221,
            "UnitPrice": 110.5,
            "VATRate": 22,
            "Description": "TakeAway - Delivery - Item VAT 22%",
            "ftChargeItemCase": 5283883447186620435,
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Card",
            "ftPayItemCase": 5283883447184523269,
            "Moment": "{{current_moment}}",
            "Amount": 221
        }
    ],
    "cbCustomer": "{\"CustomerVATId\": \"12345\"}",
    "ftReceiptCase": 5283883447184523265
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest FoodBeverage_CashAndVoucher()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0006",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": 1,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883447184523265,
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Voucher",
            "ftPayItemCase": 5283883447184523270,
            "Moment": "{{current_moment}}",
            "Amount": 10
        },
        {
            "Quantity": 1,
            "Description": "Cash",
            "ftPayItemCase": 5283883447184523265,
            "Moment": "{{current_moment}}",
            "Amount": 97
        }
    ],
    "ftReceiptCase": 5283883447184523265
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }


        public static ReceiptRequest FoodBeverage_CashAndVoucher_Discount()
        {
            var current_moment = DateTime.UtcNow.ToString("o");
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "0001-0002",
    "cbUser": "user1234",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [
        {
            "Quantity": 1,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883447184523265,
            "VATAmount": 9.73,
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": -1,
            "Amount": -107,
            "VATRate": 10,
            "VATAmount": -9.73,
            "ftChargeItemCase": 5283883447184785409,
            "Description": "Discount/Free item - Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [
        {
            "Quantity": 1,
            "Description": "Cash",
            "ftPayItemCase": 5283883447184523265,
            "Moment": "{{current_moment}}",
            "Amount": 0
        }
    ],
    "ftReceiptCase": 5283883447184523265
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }
    }
}