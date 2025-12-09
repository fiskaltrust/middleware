using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

public static class ReceiptExamples
{
    private static readonly Guid _cashBoxId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private static readonly Guid _posSystemId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    public static ReceiptRequest GetPosReceiptWithCash()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"POS-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 2.0m,
                    Amount = 42.0m,
                    UnitPrice = 21.0m,
                    VATRate = 21.0m,
                    VATAmount = 7.24m,
                    Description = "Product A",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment
                },
                new ChargeItem
                {
                    Quantity = 1.0m,
                    Amount = 10.0m,
                    VATRate = 10.0m,
                    VATAmount = 0.91m,
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Description = "Product B",
                    Moment = currentMoment
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment,
                    Amount = 52.0m
                }
            },
            ftReceiptCase = (ReceiptCase)0x4753_0000_0000_0001
        };
    }

    public static ReceiptRequest GetPosReceiptWithCard()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"POS-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1.0m,
                    Amount = 100.0m,
                    UnitPrice = 100.0m,
                    VATRate = 21.0m,
                    VATAmount = 17.36m,
                    Description = "Service",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Card",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0002,
                    Moment = currentMoment,
                    Amount = 100.0m
                }
            },
            ftReceiptCase = (ReceiptCase)0x4753_0000_0000_0001
        };
    }

    public static ReceiptRequest GetVoidReceipt()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"VOID-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = -1.0m,
                    Amount = -50.0m,
                    UnitPrice = 50.0m,
                    VATRate = 21.0m,
                    VATAmount = -8.68m,
                    Description = "Voided Item",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash Return",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment,
                    Amount = -50.0m
                }
            },
            ftReceiptCase = (ReceiptCase)0x4753_0000_0004_0001 // Void flag
        };
    }

    public static ReceiptRequest GetRefundReceipt()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"REFUND-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = -1.0m,
                    Amount = -30.0m,
                    UnitPrice = 30.0m,
                    VATRate = 21.0m,
                    VATAmount = -5.21m,
                    Description = "Refunded Item",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0010_0001,
                    Moment = currentMoment
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash Refund",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment,
                    Amount = -30.0m
                }
            },
            ftReceiptCase = (ReceiptCase)0x4753_0000_0010_0001 // Refund flag
        };
    }

    public static ReceiptRequest GetTrainingReceipt()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"TRAIN-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1.0m,
                    Amount = 10.0m,
                    UnitPrice = 10.0m,
                    VATRate = 21.0m,
                    VATAmount = 1.74m,
                    Description = "Training Item",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0001,
                    Moment = currentMoment,
                    Amount = 10.0m
                }
            },
            ftReceiptCase = (ReceiptCase)0x4753_0000_0002_0001 // Training flag
        };
    }

    public static ReceiptRequest GetZeroReceipt()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"ZERO-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = (ReceiptCase)0x4753_0000_0000_2000
        };
    }

    public static ReceiptRequest GetDailyClosing()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"DAILY-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = (ReceiptCase)0x4753_0000_0000_2011
        };
    }

    public static ReceiptRequest GetMonthlyClosing()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"MONTHLY-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = (ReceiptCase)0x4753_0000_0000_2012
        };
    }

    public static ReceiptRequest GetYearlyClosing()
    {
        var currentMoment = DateTime.UtcNow;
        return new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = _posSystemId,
            cbTerminalID = "TERM001",
            cbReceiptReference = $"YEARLY-{Guid.NewGuid().ToString()[..8]}",
            cbUser = "TestUser",
            cbReceiptMoment = currentMoment,
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = (ReceiptCase)0x4753_0000_0000_2013
        };
    }
}
