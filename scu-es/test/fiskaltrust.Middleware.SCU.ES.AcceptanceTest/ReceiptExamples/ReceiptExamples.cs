using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

public static class ReceiptExamples
{
    public static ReceiptRequest GetPosReceiptWithCash(ReceiptCase receiptCase)
    {
        return new ReceiptRequest
        {
            cbReceiptReference = $"POS-{Guid.NewGuid().ToString()[..8]}",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 2.0m,
                    Amount = 42.0m,
                    VATRate = 21.0m,
                    VATAmount = 7.24m,
                    Description = "Product A",
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001
                },
                new ChargeItem
                {
                    Quantity = 1.0m,
                    Amount = 10.0m,
                    VATRate = 10.0m,
                    VATAmount = 0.91m,
                    ftChargeItemCase = (ChargeItemCase)0x4753_0000_0000_0001,
                    Description = "Product B"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase)0x4753_0000_0000_0001,
                    Amount = 52.0m
                }
            ],
            ftReceiptCase = receiptCase.WithCountry("ES").WithVersion(2)
        };
    }
}
