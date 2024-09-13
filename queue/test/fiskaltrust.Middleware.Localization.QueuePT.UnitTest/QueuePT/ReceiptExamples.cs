﻿using fiskaltrust.ifPOS.v1;
using System.Text.RegularExpressions;
using System;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT;

public static class ReceiptExamples
{
    public static ReceiptRequest NUNO_BASIC_RECEIPT = new ReceiptRequest
    {
        cbChargeItems =
        [
            new ChargeItem
            {
                Position = 1,
                ProductNumber = "SUPCERTIFIC",
                Description = "Suporte Certifica  o Software",
                Quantity = 1.000000m,
                Unit = "UN",
                UnitPrice = 400.00m,
                Moment = new DateTime(2024, 06, 27, 11, 37, 18),
                Amount = 400.00m,
                VATRate = 0.00m,
                VATAmount = 0.00m,
                ftChargeItemCase = 0x5054_2000_0000_0008
            }
        ],
        ftReceiptCase = 0x5054_2000_0000_0001
    };

    public static ReceiptRequest CASH_SALES_RECEIPT = new ReceiptRequest
    {
        ftCashBoxID = Guid.NewGuid().ToString(),
        ftPosSystemId = Guid.Empty.ToString(),
        cbTerminalID = "00010001",
        cbReceiptReference = "0001-0002",
        cbUser = "user",
        cbReceiptMoment = DateTime.UtcNow,
        cbChargeItems =
        [
            new ChargeItem
            {
                Position = 1,
                ProductGroup = "Drinks",
                ProductNumber = Guid.NewGuid().ToString(),
                Unit = "l",
                Quantity = 1.0m,
                Amount = 3.20m,
                UnitPrice = 3.20m,
                VATRate = 6m,
                VATAmount = 3.20m - (3.20m / (1 + 0.06m)),
                ftChargeItemCase = 0x5054_2000_0000_0001,
                Description = "Beer",
                Moment = DateTime.UtcNow
            }
        ],
        cbPayItems =
        [
            new PayItem
            {
                Quantity = 1,
                Description = "Cash",
                ftPayItemCase = 0x5054_2000_0000_0001,
                Moment = DateTime.UtcNow,
                Amount = 3.20m,
            }
        ],
        ftReceiptCase = 0x5054_2000_0000_0001
    };

    public static ReceiptRequest DEBIT_SALES_RECEIPT = new ReceiptRequest
    {
        ftCashBoxID = Guid.NewGuid().ToString(),
        ftPosSystemId = Guid.Empty.ToString(),
        cbTerminalID = "00010001",
        cbReceiptReference = "0001-0002",
        cbUser = "user",
        cbReceiptMoment = DateTime.UtcNow,
        cbChargeItems =
        [
            new ChargeItem
            {
                Position = 1,
                ProductGroup = "Drinks",
                ProductNumber = Guid.NewGuid().ToString(),
                Unit = "l",
                Quantity = 1.0m,
                Amount = 3.20m,
                UnitPrice = 3.20m,
                VATRate = 6m,
                VATAmount = 3.20m - (3.20m / (1 + 0.06m)),
                ftChargeItemCase = 0x5054_2000_0000_0001,
                Description = "Beer",
                Moment = DateTime.UtcNow
            }
        ],
        cbPayItems =
        [
            new PayItem
            {
                Quantity = 1,
                Description = "Card",
                ftPayItemCase = 0x5054_2000_0000_0004,
                Moment = DateTime.UtcNow,
                Amount = 3.20m,
            }
        ],
        ftReceiptCase = 0x5054_2000_0000_0001
    };

    public static ReceiptRequest MULTIPLE_PAYITEMS_SALES_RECEIPT = new ReceiptRequest
    {
        ftCashBoxID = Guid.NewGuid().ToString(),
        ftPosSystemId = Guid.Empty.ToString(),
        cbTerminalID = "00010001",
        cbReceiptReference = "0001-0002",
        cbUser = "user",
        cbReceiptMoment = DateTime.UtcNow,
        cbChargeItems =
       [
           new ChargeItem
            {
                Position = 1,
                ProductGroup = "Drinks",
                ProductNumber = Guid.NewGuid().ToString(),
                Unit = "l",
                Quantity = 1.0m,
                Amount = 3.20m,
                UnitPrice = 3.20m,
                VATRate = 6m,
                VATAmount = 3.20m - (3.20m / (1 + 0.06m)),
                ftChargeItemCase = 0x5054_2000_0000_0001,
                Description = "Beer",
                Moment = DateTime.UtcNow
            }
       ],
        cbPayItems =
       [
           new PayItem
            {
                Quantity = 1,
                Description = "Cash",
                ftPayItemCase = 0x5054_2000_0000_0001,
                Moment = DateTime.UtcNow,
                Amount = 1.00m,
            },
           new PayItem
            {
                Quantity = 1,
                Description = "Card",
                ftPayItemCase = 0x5054_2000_0000_0004,
                Moment = DateTime.UtcNow,
                Amount = 2.20m,
            }
       ],
        ftReceiptCase = 0x5054_2000_0000_0001
    };
}