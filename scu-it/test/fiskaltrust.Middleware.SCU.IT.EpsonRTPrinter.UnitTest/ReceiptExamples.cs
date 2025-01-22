using System;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public static class ReceiptExamples
    {
        public static ReceiptRequest GetInitialOperation()
        {
            var current_moment = DateTime.UtcNow;
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "INIT",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [],
    "cbPayItems": [],
    "ftReceiptCase": {{0x4954200000004001}},
    "cbUser": "Admin"
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetOutOOperation()
        {
            var current_moment = DateTime.UtcNow;
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "OutOfOperation",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [],
    "cbPayItems": [],
    "ftReceiptCase": {{0x4954200000004002}},
    "cbUser": "Admin"
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetZeroReceipt()
        {
            var current_moment = DateTime.UtcNow;
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "Zero",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [],
    "cbPayItems": [],
    "ftReceiptCase": {{0x4954200000002000}},
    "cbUser": "Admin"
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetDailyClosing()
        {
            var current_moment = DateTime.UtcNow;
            var receipt = $$"""
{
    "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
    "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
    "cbTerminalID": "00010001",
    "cbReceiptReference": "Daily-Closing",
    "cbReceiptMoment": "{{current_moment}}",
    "cbChargeItems": [],
    "cbPayItems": [],
    "ftReceiptCase": {{0x4954200000002011}},
    "cbUser": "Admin"
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest GetTakeAway_Delivery_Cash()
        {
            var current_moment = DateTime.UtcNow.ToString("s");
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
            "ftChargeItemCase": 5283883447184556040,
            "Description": "TakeAway - Delivery - Item VAT NI",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447184531464,
            "Description": "TakeAway - Delivery - Item VAT NS",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447184527368,
            "Description": "TakeAway - Delivery - Item VAT ES",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447184535560,
            "Description": "TakeAway - Delivery - Item VAT RM",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447184539656,
            "Description": "TakeAway - Delivery - Item VAT AL",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 1,
            "Amount": 10,
            "VATRate": 0,
            "VATAmount": 0,
            "ftChargeItemCase": 5283883447184543752,
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
            var current_moment = DateTime.UtcNow;
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
            var current_moment = DateTime.UtcNow;
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
            var current_moment = DateTime.UtcNow;
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
            var current_moment = DateTime.UtcNow;
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
    "cbCustomer": "{\"CustomerVATId\": \"IT01606720215\"}",
    "ftReceiptCase": 5283883447184523265
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }

        public static ReceiptRequest FoodBeverage_CashAndVoucher()
        {
            var current_moment = DateTime.UtcNow;
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

        public static ReceiptRequest NonFiscal()
        {
            var current_moment = DateTime.UtcNow;
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
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883447184523265,
            "ProductBarcode": "4015000073615",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883451479490561,
            "ProductBarcode": "12345678",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883455774457857,
            "ProductBarcode": "123456789012",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883460069425153,
            "ProductBarcode": "012345654565",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883464364392449,
            "ProductBarcode": "CODE39VALUE",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883468659359745,
            "ProductBarcode": "CODE93VALUE",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883472954327041,
            "ProductBarcode": "{BCODE128VALUE",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883477249294337,
            "ProductBarcode": "A12345B",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883481544261633,
            "ProductBarcode": "1234567890",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883485839228929,
            "ProductBarcode": "1234567890",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883490134196225,
            "ProductBarcode": "https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc/italy/reference-tables/ftreceiptcase",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883494429163521,
            "ProductBarcode": "CUSTOMTYPE74",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883498724130817,
            "ProductBarcode": "0123456789012",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883503019098113,
            "ProductBarcode": "0123456789012",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883507314065409,
            "ProductBarcode": "1123456789012",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883511609032705,
            "ProductBarcode": "0123456789ABCDabcd",
            "Description": "Food/Beverage - Item VAT 10%",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883515904000001,
            "ftChargeItemCaseData": "VEhJUyBJUyBBIFRFU1QgSU1BR0UNCk1BREUgQlkgVEVYVA==",
            "Description": "BMP",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883584623476737,
            "ftChargeItemCaseData": "G0AbMygbYQEddjAADQAyAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHAAAAAAAAAAAAAAAAcoAAAAAAAAAAAAAAAIB4AAAAAAAAAAAAAACaEAAAAAAAAAAAAAAGLYgAAAAAAAAAAAAAAmTIAAAAAAAAAAAAAARBJAAAAAAAAAAAAAACgKYAAAAAAAAAAAAABMDSAAAAAAAAAAAAAARMEAAAAAAAAAAAAAAKRAQAAAAAAAAAAAAABKDkAAAAAAAAAAAAAAJOjADiACAACAAAAAABMsoB5wAwAAwAAAAAAQkQAYYAcAAMYAAAAYGAMAGAADAADGAAAAGA6sAD8h4xHwz5JjDz4B1AA/4+Y5+c+eYz8+AAAAGHMDMBjOHmMwOAAAABhjA2AYxhhjGBgAAAAYY+Zg+cYYYx8YAAAAGGDjYdjGGGMHGAAAABhgM3OcxjhnAxgwAAAYc+cxmccYfzsecAAAGGPiGfjHmH0/HiAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAARAAAAAAAAAAAAACIAEQAAAAAAAAAAAAAiABAEAAAAAAAAAAAAJ48RHAAAAAAAAAAAACIBEQIAAAAAAAAAAAAiAREBAAAAAAAAAAAAIgUREgAAAAAAAAAAACIZETIAAAAAAAAAAAAiERMjAAAAAAAAAAAAIRMREgAAAAAAAAAAAACEEAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAChshABtFARshECAgICAgICAgICAgICAgICAgICAgICAgTUYgICAgICAgICAgICAgICAgICAgICAgIAobIQAgICAgICAgICAgICAgICBQaWF6emEgRHVvbW8gMjAvQyAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgMjAxMDAgTWlsYW5vICAgICAgICAgICAgICAgICAgChtFASAgICAgICAgICAgICBET0NVTUVOVE8gQ09NTUVSQ0lBTEUgICAgICAgICAgICAgIAobIQAbRQEgICAgICAgICAgICBkaSB2ZW5kaXRhIG8gcHJlc3RhemlvbmUgICAgICAgICAgICAKGyEAChtFAURFU0NSSVpJT05FICAgICAgICAgICAgICAgICAgIElWQSAgICAgIFByZXp6byjVKQobIQBGb29kL0JldmVyYWdlIC0gSXRlbSBWQVQgMTAlICAgICAgIDEwJSAgICAgICAgMTAKNywwMApGb29kL0JldmVyYWdlIC0gSXRlbSBWQVQgMTAlICAgICAgIDEwJSAgICAgICAgMTAKNywwMAoKG0UBGyEQVE9UQUxFIENPTVBMRVNTSVZPICAgICAgICAgICAgICAgICAgICAgICAgMjE0LDAwChshABtFARshEGRpIGN1aSBJVkEgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAxOSw0NQobIQAKUGFnYW1lbnRvIGNvbnRhbnRlICAgICAgICAgICAgICAgICAgICAgICAgMjE0LDAwCkltcG9ydG8gcGFnYXRvICAgICAgICAgICAgICAgICAgICAgICAgICAgIDIxNCwwMAoKICAgICAgICAgICAgICAgMzEtMTItMjAyNCAxMjo0NQ0gICAgICAgICAgICAgICAgCiAgICAgICAgICAgIERPQ1VNRU5UTyBOLiAwMDE0LTAwMDENICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgIENhc3NhIEZNIFRlc3QNICAgICAgICAgICAgICAgICAKCgoKCh1WABshAA==",
            "Description": "Raster",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [],
    "ftReceiptCase": 5283883447184535552
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }
        public static ReceiptRequest NonFiscal2()
        {
            var current_moment = DateTime.UtcNow;
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
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883515904000001,
            "ftChargeItemCaseData": "VEhJUyBJUyBBIFRFU1QgSU1BR0UNCk1BREUgQlkgVEVYVA==",
            "Description": "BMP",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883515904000001,
            "ftChargeItemCaseData": "Qk3ePwAAAAAAAJYAAAB8AAAAWgAAAC0AAAABACAAAwAAAAAAAADXDQAA1w0AAAAAAAAAAAAAAAD/AAD/AAD/AAAAAAAA/yBuaVcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/wAAAAD/AAAAAP8A/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////v7+/+jo6P7d3d3+///////////19fX/xsbG/rGxsf7l5eX+/////+jo6P60tLT+wcHB/tvb2/7g4OD/+Pj4//v7+/7T09P+9/f3//39/f/j4+P+4+Pj///////8/Pz/z9DQ/rGxsf7Pz8/+4ODg/uTk5P7//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7y8vPyUlJT///////////++vr7+eHh4/q2urv3i4uL++Pj4/4OEhP6Wl5f+tra2/nV1df6IiIj/8PDw//Ly8v5kZGT+6urq//r6+v+tra3+ra2t///////Nzc3+dnZ2/rCwsP6lpaX+XFxc/rGxsf3//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7m5ufyPj4////////////+mpqb/q6ur/vf39///////7Ozs/15fX/7k5OT+/////8bGxv2IiYn+7u7u//Ly8v5ZWVn+6enp//r6+v+pqan+qqqq//////+rq6v+o6Oj/vX19f/4+Pj/mpqa/6ioqPz//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7q6uvyRkZH///////////+np6f/rq6u/vr6+v//////9/f3/4KCgv6MjIz9u7y8+4uLi/yGhob+8PDw//Ly8v5eXl7+6enp//r6+v+qqqr+q6ur///////Nzc3+bm9v/qioqPy2trb7b29v/q6urvz//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7q6uvyRkZH///////////+oqKj/rq6u/vv7+////////////+7u7v7AwMD+u7u7/JGRkfyGh4f+7+/v//Ly8v5eXl7+6enp//r6+v+qqqr+q6ur///////+/v7/2dra/rm5uf24uLj8dXV1/qysrPz//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7q6uvyRkZH///////////6pqan/r6+v/vv7+/7//////////////////////////87Ozv6IiIj+7u7u//Ly8v5eXl7+6enp//r6+v+qqqr+qqqq////////////////////////////np6e/qmpqf3//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7q6uvyUlJT//////9vb2/x0dHT+gYGB/bm5ufvm5ub9/Pz8/9HR0f7BwcH+yMjI/omJif6mpqb++Pj4//Hx8f5eXl7+6enp//r6+v+nqKj+p6en///////s7Oz+xMTE/sbGxv7AwMD+aGlp/tHR0f7//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7q6uvyUlZX//////9XV1f1oaGj+d3h4/a+vr/zh4eH9/Pz8/87Ozv6qqqr+pqam/ri4uP7t7e3///////Dw8P5eXl7+6enp//39/f/f39/93t7e/f/////t7e3+urq6/qeoqP2oqKj+y8vL/vv7+////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/7i4uPyLi4v///////////6rq6v+r6+v/v39/f7////////////////+/v7//Pz8//////////////////Dw8P5dXV3+6enp//39/f/f4OD939/f/f////////////////z8/P/9/f3//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////f39/8XFxf2kpKT////////////s7Oz+6+vr/v7+/v////////////////////////////////////////////Dw8P5OT0/+6Ojo//r6+v+qqqr+qaqq//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////j4+P/19fX///////////////////////////////////////////////////////////////////////v7+//Z2dn+9vb2//7+/v/r6ur/6urq///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////+/v7//f39//7+/v/////////////////+/v7//f39//7+/v////////////z8/P/6+vr/+vr6//////////////////7+/v/9/f3+/v7+/////////////v7+//39/f/9/f3////////////6+vr//f39///////+/v7//v7+///////+/v7//f39//7+/v/////////////////9/f3/+fn5//z8/P///////v7+//39/f/+/v7///////////////////////v7+//7+/v//////////////////f39/////////////v7+//r6+v/5+fn//f39///////////////////////9/f3/+fn5//z8/P////////////7+/v/9/f3//v7+//////////////////////////////////////////////////////////////////////////////////7+/v/j4+P9urq6/dXV1f35+fn///////7+/v/Ozs79wMDA/d7e3v37+/v/2NjY/rm5uf6pqan+ra2t/s7Ozv739/f//f39/9LS0v2/v7/839/f/v/////x8fH+ycnJ/bu7u/3r6+v++vr6/8zMzP6qqqr+wcHB/tjY2P7Gxsb9z8/P/fr6+v/i4uL+v7+//c3Nzf38/Pz//////+vr6/69vb3+qamp/ru7u/74+Pj/4ODg/L6+vv3S0tL8/f39/v//////////4uLi/rGxsf2ysrL9zMzM/ubm5v/Ozs7+y8vL/fb29v/p6en+wsLC/q6urv6oqKj+vr6+/ujo6P7//////////+zs7P6+vr7+qKio/ri4uP719fX//Pz8/83Nzf7BwcH94+Pj/v////////////////////////////////////////////////////////////////////////////////z8/P64uLj7AAAA/oeHh/7x8fH+//////z8/P5mZmb9AAAA/6ioqPvz8/P+cXFx/QAAAP8vLy/+AAAA/wAAAP+9vb3++Pj4/3d3d/4AAAD+ra2t//////+2trb+AAAA/zw8PP7r6+v+1tbW/gAAAP8AAAD/NDQ0/igoKP4AAAD/cHBw/vT09P+1tbX/AAAA/mNjY//6+vr/+Pj4/pycnP4AAAD/AAAA/yQkJP7y8vL+r6+v/AAAAP92dnb9+fn5/f/////39/f+g4OD/gAAAP8AAAD/AAAA/kdHR/4AAAD+ZWVl/+zs7P+1tbX9AAAA/hsbG/4VFRX/AAAA/3t7e/709PT++vr6/6Kiov4AAAD/AAAA/wAAAP7s7Oz++Pj4/1tbW/8AAAD+s7Oz//////////////////////////////////////////////////////////////////////////////////39/f69vb38AAAA/5CQkP/y8vL///////39/f51dXX+BwcH/6ysrPv9/f3+1NTU/srKyv7f39/+mpqa/gMDA/96enr++Pj4/4SEhP8AAAD/urq6/+3t7f5sbGz/AAAA/8TExP7+/v7/qKio/QAAAP9/f3/+29vb/ZmZmf4AAAD/fX19/vX19f+5ubn/AAAA/3R0dP/8/Pz/7Ozs/nx8fP4AAAD/np6e/dPT0/78/Pz+srKy/AAAAP+Dg4P++fn5/v/////n5+f+aGho/wAAAP+EhIT+mpqa/k1NTf4AAAD/c3Nz/+rq6v/l5eX+yMjI/tjY2P7Nzc39Hh4e/kRERP/b29v+8fHx/oODg/0AAAD/m5ub/tHR0f78/Pz++fn5/3R0dP4JCQn/vLy8//////////////////////////////////////////////////////////////////////////////////39/f69vb38AAAA/5CQkP7y8vL///////39/f51dXX+CAgI/6urq/v///////////f39/7c3Nz9ioqK/gAAAP92dnb++Pj4/4SEhP8AAAD/sLCw/7a2tv4AAAD/fX19/vLy8v7/////tLS0/QAAAP9hYWH/s7Oz/4SEhP4AAAD/fX19/vX19f+5ubn/AAAA/3R0dP/8/Pz/7e3t/n5+fv4AAAD/zs7O/P//////////sbGx/AAAAP+Dg4P++fn5/v/////l5eX/aGho/wAAAP/U1NT+9/f3/r+/v/wAAAD/cnJy/+bm5v///////////ubm5v69vb3+ExMT/0NDQ//Z2dn+8vLy/YSEhP0AAAD/ycnJ/v///////////f39/97e3v7V1dX+7Ozs/v////////////////////////////////////////////////////////////////////////////////39/f69vb38AAAA/5CQkP7y8vL///////39/f51dXX+BgYG/6urq/v////+09PT/nR0dP5DQ0P+AAAA/wAAAP7AwMD++fn5/4ODg/8hISH/kpKS/n9/f/4AAAD/wsLC/v7+/v//////6+vr/omJif5DQ0P/MTEx/yUlJf8AAAD/fX19/vX19f+5ubn/AAAA/3R0dP/8/Pz/7e3t/n5+fv4AAAD/ysrK/P////7/////sbGx/AAAAP+Hh4f+/Pz8/v/////l5eX/aGho/wAAAP/b29v//////9jY2P0AAAD/cnJy/+np6f/u7u7/mJiY/1hYWP8AAAD/AAAA/4WFhf319fX+8PDw/YSEhP0AAAD/xcXF/v7+/v////////////////////////////////////////////////////////////////////////////////////////////////////////////7+/v7AwMD8AAAA/5OTk//09PT///////////54eHj+BgYG/62trfv4+Pj+k5OT/gAAAP94eHj+wMDA/uXl5f77+/v/+fn5/4ODg/8AAAD/qqqq/6mpqf4AAAD/k5OT/vf39/////////////b29v/w8PD/8vLy/rCwsP0AAAD+fHx8/vX19f+5ubn/AAAA/3R0dP/7+/v/7u7u/oGBgf4AAAD/zc3N/P////7/////sbGx/AAAAP9kZGT+4ODg/f/////m5ub/aGho/wAAAP/Y2Nj//////9XV1f8AAAD/c3Nz/+3t7f/ExMT+AAAA/zw8PP6tra3+19fX/vT09P//////8PDw/YeHh/0AAAD/ycnJ/v///////////////////////////////////////////////////////////////////////////////////////////////////////////v7+/+Tk5P6enp78AAAA/3l5ef7R0dH+3d3d/9jY2P5iYmL+BgYG/66urvv19fX+h4eH/gAAAP+5ubn+8PDw/uXl5f3w8PD++fn5/4ODg/8AAAD/u7u7/+Li4v5aWlr+AAAA/tTU1P7/////+Pj4/9nZ2f7o6Oj+5+fn/ZiYmP4AAAD/hISE/vb29v+5ubn/AAAA/3d3d//29vb/0dHR/2RkZP4AAAD/qKio/dnZ2f/8/Pz/srKy/AAAAP8AAAD/Ojo6/7i4uP3g4OD+b29v/wAAAP/Y2Nj//////9XV1f8AAAD/c3Nz/+7u7v+9vb3+AAAA/3p6ev3q6ur+6+vr/ubm5v76+vr+0tLS/mlpaf4AAAD/paWl/tjY2P/4+Pj//////////////////////////////////fv5/u7izf707eD+///+////////////////////////////////////////////+fn5/4eHh/0AAAD/BAQE/wkJCf8VFRX/GBgY/xYWFv8DAwP/EBAQ/qurq/v8/Pz+qKio/gAAAP8AAAD/GBgY/wAAAP+3t7f9+fn5/4ODg/8AAAD/tbW1//////+hoaH+AAAA/3l5ef739/f/4ODg/gAAAP4KCgr/AAAA/gAAAP8AAAD/tbW1/vz8/P+4uLj/AAAA/319ff/l5eX/ZmZm/wAAAP8DAwP/CwsL/wAAAP/o6Oj+s7Oz/QAAAP8wMDD/AAAA/5SUlP7Z2dn+cXFx/wAAAP/Y2Nj//////9TU1P8AAAD/cHBw/+vr6//R0dH+RkZG/gAAAP8cHBz/AAAA/3Jycv7i4uL+cXFx/wAAAP8DAwP/DAwM/wAAAP/h4eH+///////////////////////////+/f3+5M+n/s6iF//Rpxn/5tOw/vXu4/7w5NH+6Na2/vTt4f///////////////////////Pz8/7u7u/xlZWX9AAAA/1hYWP6Xl5f8np6e/Zubm/2fn5/9np6e/c/Pz/v/////7Ozs/qampv5/f3/+fn5+/oaGhv7Ozs79+fn5/4ODg/8AAAD/srKy///////j4+P+oaGh/JSUlPzr6+v96urq/p6env6Hh4f+fX19/oSEhP6zs7P98/Pz//////+3t7f/AAAA/3p6ev/u7u7+paWl/Tc3N/4AAAD/eHh4/Zqamvzu7u7+0dHR/JWVlfzLy8v8ysrK/sLCwv7p6en+rq6u/ZKSkvzn5+f+/////+Xl5f6QkJD8rq6u/fDw8P/9/f3/wsLC/YmJif58fHz+goKC/qysrP7s7Oz+qqqq/To6Ov4AAAD/dnZ2/piYmPzo6Oj9///////////7+PT/9Ovd/vLo2P7jzaL+0KUe/9ClDv/RqDX/0KQq/9ClAP/OoQD/zJoA/9y/h/769/H///////////////////////////7AwMD8AAAA/4yMjP7u7u79/////v////7j4+P+2dnZ/uvr6/7////////////////+/v7+/f39/v////7/////+fn5/4ODg/8AAAD/srKy//////////////////////7////////////////////+/f39/v////7///////////////+3t7f/AAAA/3R0dP/9/f3/7+/v/nh4eP4AAAD/y8vL/P////7//////////v////7///////////////////////////////7////////////////////+/////v////////////////////79/f3+/////v////7/////8fHx/X9/f/0AAAD/xsbG/v////7///////////37+f/jzaX+zp8A/s6fAP/QpCH/2Ldt/93Chv/gxpH/38WP/9y/gf/XtGP/0KUU/9GmHv/v5ND+///////////////////////////Jycn+AAAA/0RERP+QkJD+0dHR/vz8/P9tbW3+AAAA/6ampv3/////////////////////////////////////+fn5/3t7e/8AAAD/r6+v//////////////////////////////////////////////////////////////////////+zs7P/AAAA/21tbf/6+vr/9fX1/re3t/5+fn7+19fX/f//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////9fX1/ru7u/2AgID+1dXV/v7+/v////////////v48//ZuHD+zZ0A/9WwUP/hyZj/5M2i/9/Ejv/bu3r/27x9/9/GlP/kzqL/38WP/9GnJf/Zt27/7uLM/vr38f7////////////////s7Oz+jo6O/ikpKf8AAAD/np6e/vn5+f6CgoL+Ojo6/6+vr/3/////////////////////////////////////+vr6/52dnf4+Pj7+s7Oz///////////////////////////////////////////////////////////////////////FxcX+U1NT/nx8fP/4+Pj///////7+/v729vb++fn5/v////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7+/v/29vb++fn5/v////////////////n17v7UrEP+1KxJ/+POof/ew47/0aYW/82eAP/PogD/z6IA/8+hAP/TrED/4cmc/+HJmf/QpRr/z6MA/93Bh/769vD+////////////////8PDw/9jY2P7T09P+5ubm/v39/f/k5OT/29vb/+zs7P7//////////////////////////////////////v7+/+3t7f/d3d3/6+vr///////////////////////////////////////////////////////////////////////09PT/4ODg/uLi4v/9/f3/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////+/n1/uPMof7QpSD/4MeT/9/Fkf/PogD/0agj/9WwU//SqBn/0KUA/86hAP/PogD/0ack/+LLnv/cwIP/z6MA/86hAP717uH+///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////8+vb+38WR/s+iAP/Ws2L/482h/9SsPv/NngD/38WQ//Dm0//UrUb/1K5I/97Civ/Urkr/zJwA/9azZf/iy5z/06tL/9ayWv77+PP////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////38un+0KYA/s+kAP/bvXn/4ciX/8+jAP/NnQD/69zD//Lo1//RpkH/69zC//n06//jzKH/5c6n/9q6dP/hyJf/1rJl/9u7ef79/Pr////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////+/fz/3sSO/tCjCP/cwIL/3sOO/82eAP/bvHn/+fXu/+jXuP/RqDr/+PLp/+PNo//XtHD/7d/G/9q6df/fxZL/2bhs/9KoF/717eD+////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////9e7i/tGoC/7cvn3/38WR/8uaAP/jzaP//fv4//Ts3//s3sX//fv5/97Div/PogD/0acZ/9ClAP/iypr/2bhs/86gAP/dwYf+////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////+PXu/tKqLv7ZuG3/4suc/82dAP/dwYz/+PTs/+HIl//t4Mn//Pn1/+jVs//QpAD/0KQA/9OsQP/jzJ7/17Ni/82fAP/ZuXD+////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////8OTR/s6gAP/Ur1P/482f/9ezY//SqTb/6tq+/+XQqf/dwIT/4sqb/9CkAP/RpwD/z6EA/9y/hP/hyJX/0acn/9i3av/z6tz+////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7eDI/s2eAP/OoAD/2719/+TOo//Tqz3/zJwA/9CkAP/PowD/z6IA/9CkAP/PogD/17Vl/+TPpf/Xs2H/1a9M/vbv4/7/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////+vfy/ufVtP7YtW3+0KQB/97Ci//kzqP/2rp4/9KpM//QpQD/0KUA/9OtRf/cwIX/5M+l/9u8eP/NnQD/4MeZ/v37+f/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////17+P+3cGH/s+hAP/au3f/4cqZ/+PMnf/iypv/4suc/+LMnv/gyJT/1rNh/82fAP/KlwD/5M+r/v38+v//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////8OTR/s+jAP/OoAD/0ac4/9WvX//Yt2r/2bhr/9ayYf/Rpij/061D/tu9f/7fxZP+9e3i////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////+vfx/9/Ejv7SqSv+271+/ty+hP7QpgD/zqEA/86gAP/cvoD+9/Hp/vv59f/8+vf///7+//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////z59f759u/+/Pv4//38+v/y6Nn+2Lhs/dq7dfz38un+////////////////////////////////",
            "Description": "BMP",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883584623476737,
            "ftChargeItemCaseData": "G0AbMygbYQEddjAADQAyAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHAAAAAAAAAAAAAAAAcoAAAAAAAAAAAAAAAIB4AAAAAAAAAAAAAACaEAAAAAAAAAAAAAAGLYgAAAAAAAAAAAAAAmTIAAAAAAAAAAAAAARBJAAAAAAAAAAAAAACgKYAAAAAAAAAAAAABMDSAAAAAAAAAAAAAARMEAAAAAAAAAAAAAAKRAQAAAAAAAAAAAAABKDkAAAAAAAAAAAAAAJOjADiACAACAAAAAABMsoB5wAwAAwAAAAAAQkQAYYAcAAMYAAAAYGAMAGAADAADGAAAAGA6sAD8h4xHwz5JjDz4B1AA/4+Y5+c+eYz8+AAAAGHMDMBjOHmMwOAAAABhjA2AYxhhjGBgAAAAYY+Zg+cYYYx8YAAAAGGDjYdjGGGMHGAAAABhgM3OcxjhnAxgwAAAYc+cxmccYfzsecAAAGGPiGfjHmH0/HiAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAARAAAAAAAAAAAAACIAEQAAAAAAAAAAAAAiABAEAAAAAAAAAAAAJ48RHAAAAAAAAAAAACIBEQIAAAAAAAAAAAAiAREBAAAAAAAAAAAAIgUREgAAAAAAAAAAACIZETIAAAAAAAAAAAAiERMjAAAAAAAAAAAAIRMREgAAAAAAAAAAAACEEAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAChshABtFARshECAgICAgICAgICAgICAgICAgICAgICAgTUYgICAgICAgICAgICAgICAgICAgICAgIAobIQAgICAgICAgICAgICAgICBQaWF6emEgRHVvbW8gMjAvQyAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgMjAxMDAgTWlsYW5vICAgICAgICAgICAgICAgICAgChtFASAgICAgICAgICAgICBET0NVTUVOVE8gQ09NTUVSQ0lBTEUgICAgICAgICAgICAgIAobIQAbRQEgICAgICAgICAgICBkaSB2ZW5kaXRhIG8gcHJlc3RhemlvbmUgICAgICAgICAgICAKGyEAChtFAURFU0NSSVpJT05FICAgICAgICAgICAgICAgICAgIElWQSAgICAgIFByZXp6byjVKQobIQBGb29kL0JldmVyYWdlIC0gSXRlbSBWQVQgMTAlICAgICAgIDEwJSAgICAgICAgMTAKNywwMApGb29kL0JldmVyYWdlIC0gSXRlbSBWQVQgMTAlICAgICAgIDEwJSAgICAgICAgMTAKNywwMAoKG0UBGyEQVE9UQUxFIENPTVBMRVNTSVZPICAgICAgICAgICAgICAgICAgICAgICAgMjE0LDAwChshABtFARshEGRpIGN1aSBJVkEgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAxOSw0NQobIQAKUGFnYW1lbnRvIGNvbnRhbnRlICAgICAgICAgICAgICAgICAgICAgICAgMjE0LDAwCkltcG9ydG8gcGFnYXRvICAgICAgICAgICAgICAgICAgICAgICAgICAgIDIxNCwwMAoKICAgICAgICAgICAgICAgMzEtMTItMjAyNCAxMjo0NQ0gICAgICAgICAgICAgICAgCiAgICAgICAgICAgIERPQ1VNRU5UTyBOLiAwMDE0LTAwMDENICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgIENhc3NhIEZNIFRlc3QNICAgICAgICAgICAgICAgICAKCgoKCh1WABshAA==",
            "Description": "Raster",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883490134196225,
            "ProductBarcode": "https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc/italy/reference-tables/ftreceiptcase",
            "Description": "Link",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883511609032705,
            "ProductBarcode": "0123456789ABCDabcd",
            "Description": "CUSTOMTYPE78",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [],
    "ftReceiptCase": 5283883447184535552
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }
        public static ReceiptRequest NonFiscal3()
        {
            var current_moment = DateTime.UtcNow;
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
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883515904000001,
            "ftChargeItemCaseData": "VEhJUyBJUyBBIFRFU1QgSU1BR0UNCk1BREUgQlkgVEVYVA==",
            "Description": "BMP",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883584623476737,
            "ftChargeItemCaseData": "G0AbMygbYQEddjAADQAyAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHAAAAAAAAAAAAAAAAcoAAAAAAAAAAAAAAAIB4AAAAAAAAAAAAAACaEAAAAAAAAAAAAAAGLYgAAAAAAAAAAAAAAmTIAAAAAAAAAAAAAARBJAAAAAAAAAAAAAACgKYAAAAAAAAAAAAABMDSAAAAAAAAAAAAAARMEAAAAAAAAAAAAAAKRAQAAAAAAAAAAAAABKDkAAAAAAAAAAAAAAJOjADiACAACAAAAAABMsoB5wAwAAwAAAAAAQkQAYYAcAAMYAAAAYGAMAGAADAADGAAAAGA6sAD8h4xHwz5JjDz4B1AA/4+Y5+c+eYz8+AAAAGHMDMBjOHmMwOAAAABhjA2AYxhhjGBgAAAAYY+Zg+cYYYx8YAAAAGGDjYdjGGGMHGAAAABhgM3OcxjhnAxgwAAAYc+cxmccYfzsecAAAGGPiGfjHmH0/HiAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAARAAAAAAAAAAAAACIAEQAAAAAAAAAAAAAiABAEAAAAAAAAAAAAJ48RHAAAAAAAAAAAACIBEQIAAAAAAAAAAAAiAREBAAAAAAAAAAAAIgUREgAAAAAAAAAAACIZETIAAAAAAAAAAAAiERMjAAAAAAAAAAAAIRMREgAAAAAAAAAAAACEEAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAChshABtFARshECAgICAgICAgICAgICAgICAgICAgICAgTUYgICAgICAgICAgICAgICAgICAgICAgIAobIQAgICAgICAgICAgICAgICBQaWF6emEgRHVvbW8gMjAvQyAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgMjAxMDAgTWlsYW5vICAgICAgICAgICAgICAgICAgChtFASAgICAgICAgICAgICBET0NVTUVOVE8gQ09NTUVSQ0lBTEUgICAgICAgICAgICAgIAobIQAbRQEgICAgICAgICAgICBkaSB2ZW5kaXRhIG8gcHJlc3RhemlvbmUgICAgICAgICAgICAKGyEAChtFAURFU0NSSVpJT05FICAgICAgICAgICAgICAgICAgIElWQSAgICAgIFByZXp6byjVKQobIQBGb29kL0JldmVyYWdlIC0gSXRlbSBWQVQgMTAlICAgICAgIDEwJSAgICAgICAgMTAKNywwMApGb29kL0JldmVyYWdlIC0gSXRlbSBWQVQgMTAlICAgICAgIDEwJSAgICAgICAgMTAKNywwMAoKG0UBGyEQVE9UQUxFIENPTVBMRVNTSVZPICAgICAgICAgICAgICAgICAgICAgICAgMjE0LDAwChshABtFARshEGRpIGN1aSBJVkEgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAxOSw0NQobIQAKUGFnYW1lbnRvIGNvbnRhbnRlICAgICAgICAgICAgICAgICAgICAgICAgMjE0LDAwCkltcG9ydG8gcGFnYXRvICAgICAgICAgICAgICAgICAgICAgICAgICAgIDIxNCwwMAoKICAgICAgICAgICAgICAgMzEtMTItMjAyNCAxMjo0NQ0gICAgICAgICAgICAgICAgCiAgICAgICAgICAgIERPQ1VNRU5UTyBOLiAwMDE0LTAwMDENICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgIENhc3NhIEZNIFRlc3QNICAgICAgICAgICAgICAgICAKCgoKCh1WABshAA==",
            "Description": "Raster",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883490134196225,
            "ProductBarcode": "https://docs.fiskaltrust.cloud/docs/poscreators/middleware-doc/italy/reference-tables/ftreceiptcase",
            "Description": "Link",
            "Moment": "{{current_moment}}"
        },
        {
            "Quantity": 0,
            "Amount": 107,
            "VATRate": 10,
            "ftChargeItemCase": 5283883511609032705,
            "ProductBarcode": "0123456789ABCDabcd",
            "Description": "CUSTOMTYPE78",
            "Moment": "{{current_moment}}"
        }
    ],
    "cbPayItems": [],
    "ftReceiptCase": 5283883447184535552
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }
        public static ReceiptRequest NonFiscalReceipt()
        {
            var current_moment = DateTime.UtcNow;
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
    "ftReceiptCase": 5283883447184535552
}
""";
            return JsonConvert.DeserializeObject<ReceiptRequest>(receipt);
        }
    }
}