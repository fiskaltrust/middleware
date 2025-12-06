using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;

public static class ReceiptExamples
{
    public static ReceiptRequest InitialOperation(Guid cashBoxId)
    {
        return new ReceiptRequest
        {
            ftCashBoxID = cashBoxId,
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_4001,
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = [],
            cbPayItems = []
        };
    }

    public static ReceiptRequest ExamplePosReceipt(Guid cashBoxId)
    {
        var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100m,
                    Description = "DebitCard",
                    ftPayItemCase = (PayItemCase) (0x4752_2000_0000_0000 | (long) PayItemCase.DebitCardPayment),
                    ftPayItemCaseData = new PayItemCaseDataCloudApi
                    {
                        Provider = new PayItemCaseProviderVivaWallet
                        {
                            Action = "Sale",
                            Protocol = "VivaWallet",
                            ProtocolVersion = "1.0",
                            ProtocolRequest = new VivaWalletPayment
                            {
                                amount = 100 * 100,
                                cashRegisterId = "",
                                currencyCode = "EUR",
                                merchantReference = Guid.NewGuid().ToString(),
                                sessionId = "John015",
                                terminalId = "123456",
                                aadeProviderSignatureData = "4680AFE5D58088BF8C55F57A5B5DBB15936B51DE;;20241015153111;4600;9;1;10;16007793",
                                aadeProviderSignature = "MEUCIQCnUrakY9pemgdXIsYvbOahoBBadDa9DPaRS9ZtTTra8gIgIUp9LPaH/E+LRwTGJWeL+MZl5j5PtFcM+chiXTqeed4="
                            },
                            ProtocolResponse = new VivaPaymentSession
                            {
                                aadeTransactionId = "116430909552789552789"
                            }
                        }
                    }
                }
            };
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 100m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [new ChargeItem {
                    Amount = 100m,
                    Quantity = 1,
                    VATRate = 0m,
                    VATAmount = 0m,
                    Description = "Line Item 1",
                    Position = 1,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0018
                }],
            cbPayItems = payItems,
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001
        };
        return receiptRequest;
    }

    public static ReceiptRequest Example_SalesInvoice_1_1(Guid cashBoxId)
    {
        var chargeItems = new List<ChargeItem> {
                    CreateGoodNormalVATRateItem(description: "Product 1", amount: 89.20m, quantity: 1),
                    CreateGoodNormalVATRateItem(description: "Product 2", amount: 23.43m, quantity: 1),
                    CreateGoodDiscountedVATRateItem(description: "Merchandise Product 1", amount: 12.30m, quantity: 1),
                    CreateGoodDiscountedVATRateItem(description: "Merchandise Product 2", amount: 113.43m, quantity: 1),
                };

        var i = 1;
        foreach (var chargeItem in chargeItems)
        {
            chargeItem.Position = i++;
            // Set fraction
            chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
            chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
        }

        var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 92.39m,
                    Description = "VAT withholding (-20%)",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0099
                },
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount) -  92.39m,
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
                }
            };

        i = 1;
        foreach (var payItem in payItems)
        {
            payItem.Position = i++;
            // Set fraction
            payItem.Amount = decimal.Round(payItem.Amount, 2, MidpointRounding.AwayFromZero);
        }

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems = payItems,
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",

            }
        };
        return receiptRequest;
    }


    public static ReceiptRequest Example_SalesInvoice_1_1_nowithholding(Guid cashBoxId)
    {
        var chargeItems = new List<ChargeItem> {
                    CreateGoodNormalVATRateItem(description: "Product 1", amount: 89.20m, quantity: 1),
                    CreateGoodNormalVATRateItem(description: "Product 2", amount: 23.43m, quantity: 1),
                    CreateGoodDiscountedVATRateItem(description: "Merchandise Product 1", amount: 12.30m, quantity: 1),
                    CreateGoodDiscountedVATRateItem(description: "Merchandise Product 2", amount: 113.43m, quantity: 1),
                };

        var i = 1;
        foreach (var chargeItem in chargeItems)
        {
            chargeItem.Position = i++;
            // Set fraction
            chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
            chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
        }

        var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
                }
            };

        i = 1;
        foreach (var payItem in payItems)
        {
            payItem.Position = i++;
            // Set fraction
            payItem.Amount = decimal.Round(payItem.Amount, 2, MidpointRounding.AwayFromZero);
        }

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems = payItems,
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",

            }
        };
        return receiptRequest;
    }


    public static ReceiptRequest Example_RetailSales(Guid cashBoxId)
    {
        var chargeItems = new List<ChargeItem>
            {
                CreateGoodNormalVATRateItem(description: "Merchandise Product 1", amount: 1.3m, quantity: 1),
                CreateGoodNormalVATRateItem(description: "Merchandise Product 2", amount: 1.0m, quantity: 1),
                CreateGoodNormalVATRateItem(description: "Merchandise Product 3", amount: 1.2m, quantity: 1),
                CreateGoodDiscountedVATRateItem(description: "Merchandise Product Discounted 1", amount: 0.5m, quantity: 1),
                CreateGoodDiscountedVATRateItem(description: "Merchandise Product Discounted 2", amount: 0.6m, quantity: 1)
            };
        var i = 1;
        foreach (var chargeItem in chargeItems)
        {
            chargeItem.Position = i++;
            // Set fraction
            chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
            chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
        }
        var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Card",
                    ftPayItemCase = (PayItemCase) (0x4752_2000_0000_0000 | (long) PayItemCase.DebitCardPayment),
                    ftPayItemCaseData = new PayItemCaseDataCloudApi
                    {
                        Provider = new PayItemCaseProviderVivaWallet
                        {
                            Action = "Sale",
                            Protocol = "VivaWallet",
                            ProtocolVersion = "1.0",
                            ProtocolRequest = new VivaWalletPayment
                            {
                                amount = (int) chargeItems.Sum(x => x.Amount) * 100,
                                cashRegisterId = "",
                                currencyCode = "EUR",
                                merchantReference = Guid.NewGuid().ToString(),
                                sessionId = "John015",
                                terminalId = "123456",
                                aadeProviderSignatureData = "4680AFE5D58088BF8C55F57A5B5DBB15936B51DE;;20241015153111;4600;9;1;10;16007793",
                                aadeProviderSignature = "MEUCIQCnUrakY9pemgdXIsYvbOahoBBadDa9DPaRS9ZtTTra8gIgIUp9LPaH/E+LRwTGJWeL+MZl5j5PtFcM+chiXTqeed4="
                            },
                            ProtocolResponse = new VivaPaymentSession
                            {
                                aadeTransactionId = "116430909552789552789"
                            }
                        }
                    }
                }
            };
        return new ReceiptRequest
        {
            Currency = Currency.EUR,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems = payItems,
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "1",
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001 // posreceipt
        };
    }

    public static ReceiptRequest Example_RetailSales_100(Guid cashBoxId)
    {
        var chargeItems = new List<ChargeItem>
            {
                CreateGoodNormalVATRateItem(description: "Merchandise Product 1", amount: 100m, quantity: 1)
            };
        var i = 1;
        foreach (var chargeItem in chargeItems)
        {
            chargeItem.Position = i++;
            chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
            chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
        }
        var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Card",
                    ftPayItemCase = (PayItemCase) (0x4752_2000_0000_0000 | (long) PayItemCase.DebitCardPayment),
                    ftPayItemCaseData = new PayItemCaseDataCloudApi
                    {
                        Provider = new PayItemCaseProviderVivaWallet
                        {
                            Action = "Sale",
                            Protocol = "VivaWallet",
                            ProtocolVersion = "1.0",
                            ProtocolRequest = new VivaWalletPayment
                            {
                                amount = (int) chargeItems.Sum(x => x.Amount) * 100,
                                cashRegisterId = "",
                                currencyCode = "EUR",
                                merchantReference = Guid.NewGuid().ToString(),
                                sessionId = "e34072ea-067c-46ca-afca-52fecbbcba7f",
                                terminalId = "16009303",
                                aadeProviderSignatureData = "fb35d169-42a3-4064-ad7c-ac92e3c0fe30;;20241105125841;10000;10000;2400;10000;16009303",
                                aadeProviderSignature = "MEUCIQCnUrakY9pemgdXIsYvbOahoBBadDa9DPaRS9ZtTTra8gIgIUp9LPaH/E+LRwTGJWeL+MZl5j5PtFcM+chiXTqeed4="
                            },
                            ProtocolResponse = new VivaPaymentSession
                            {
                                aadeTransactionId = "116431015555865555865"
                            }
                        }
                    }
                }
            };
        return new ReceiptRequest
        {
            Currency = Currency.EUR,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems = payItems,
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "1",
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001 // posreceipt
        };
    }

    public static ReceiptRequest Example_SalesInvoice_8_6(Guid cashBoxId)
    {
        var chargeItems = new List<ChargeItem> {
                    CreateGoodNormalVATRateItem(description: "Product 1", amount: 89.20m, quantity: 1),
                    CreateGoodNormalVATRateItem(description: "Product 2", amount: 23.43m, quantity: 1)
                };

        foreach (var chargeItem in chargeItems)
        {
            // Set fraction
            chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
        }
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbArea = "22",
            Currency = Currency.EUR,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems = [],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_3004,
        };
        return receiptRequest;
    }


    public static ReceiptRequest Example_RetailSales_100App2APp(Guid cashBoxId)
    {
        var chargeItems = new List<ChargeItem>  
            {
                CreateGoodNormalVATRateItem(description: "Merchandise Product 1", amount: 100m, quantity: 1)
            };
        var i = 1;
        foreach (var chargeItem in chargeItems)
        {
            chargeItem.Position = i++;
            chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
            chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
        }
        var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Card",
                    ftPayItemCase = (PayItemCase) (0x4752_2000_0000_0000 | (long) PayItemCase.DebitCardPayment),
                    ftPayItemCaseData = new PayItemCaseDataApp2App
                    {
                        Provider = new PayItemCaseProviderVivaWalletApp2APp
                        {
                            Action = "Sale",
                            Protocol = "use_auto",
                            ProtocolVersion = "1.0",
                            ProtocolRequest = "vivapayclient://pay/v1?appId=eu.fiskaltrust.instore.app&action=sale&clientTransactionId=29d6e88a-432a-44bd-8390-20af43d48df0&amount=100&callback=instoreapp%3a%2f%2fresult&show_receipt=false&show_transaction_result=false&show_rating=false&aadeProviderId=999&aadeProviderSignatureData=29d6e88a-432a-44bd-8390-20af43d48df0%3b%3b20241116101605%3b100%3b100%3b2400%3b100%3b16009303&aadeProviderSignature=MEQCIBaEvb4fHFPn2omFEExMcKmPyz62mhGqX5TsB8AjDJRpAiAYWgnzvwpNzSXr98QTQJ%2bThMfvYZe48MtA5Eed0QfEow%3d%3d",
                            ProtocolResponse = "instoreapp://result?aid=A0000000041010&status=success&message=Transaction successful&action=sale&clientTransactionId=29d6e88a-432a-44bd-8390-20af43d48df0&amount=100&rrn=432122575537&verificationMethod=CONTACTLESS - NO CVM&cardType=Debit Mastercard&accountNumber=535686******1364&referenceNumber=575537&authorisationCode=575537&tid=16009303&orderCode=4321235753009303&transactionDate=2024-11-17T00:16:11.4043324+02:00&transactionId=ca0532f1-75e6-465f-a11e-ebb065a87a91&paymentMethod=CARD_PRESENT&shortOrderCode=4321235753&aadeTransactionId=116432122575537575537"
                        }
                    }
                }
            };
        return new ReceiptRequest
        {
            Currency = Currency.EUR,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems = payItems,
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "1",
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001 // posreceipt
        };
    }

    public static ReceiptRequest ExampleCashSales(Guid cashBoxId)
    {
        return new ReceiptRequest
        {
            ftCashBoxID = cashBoxId,
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0000,
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
                        [
                            new ChargeItem
                    {
                        Position = 1,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.2m,
                        Amount = 6.2m,
                        VATRate = 24m,
                        Quantity = 1,
                        Description = "ChargeItem1"
                    },
                    new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.2m,
                        Amount = 6.2m,
                        VATRate = 24m,
                        Quantity = 1,
                        Description = "ChargeItem2"
                    }
                        ],
            cbPayItems =
                        [
                            new PayItem
                    {
                        ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                        Amount = 12.4m,
                        Description = "Cash"
                    }
                        ]
        };
    }


    public static ChargeItem CreateServiceNormalVATRateItem(string description, decimal amount, decimal quantity)
    {
        var vatRate = 24m;
        return new ChargeItem
        {
            Amount = amount,
            VATRate = vatRate,
            VATAmount = amount / (100M + vatRate) * vatRate,
            ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0023,
            Quantity = quantity,
            Description = description
        };
    }

    public static ChargeItem CreateServiceDiscountedVATRateItem(string description, decimal amount, decimal quantity)
    {
        var vatRate = 13m;
        return new ChargeItem
        {
            Amount = amount,
            VATRate = vatRate,
            VATAmount = amount / (100M + vatRate) * vatRate,
            ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0021,
            Quantity = quantity,
            Description = description
        };
    }

    public static PayItem CreateWithHoldingPayItem(string description, decimal amount)
    {
        var percent = 20m;
        return new PayItem
        {
            Amount = amount * (percent / 100),
            ftPayItemCase = (PayItemCase) 0x4752_2000_0000_099,
            Quantity = 1,
            Description = description
        };
    }

    public static ChargeItem CreateGoodNormalVATRateItem(string description, decimal amount, decimal quantity)
    {
        var vatRate = 24m;
        return new ChargeItem
        {
            Amount = amount,
            VATRate = vatRate,
            VATAmount = amount / (100M + vatRate) * vatRate,
            ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
            Quantity = quantity,
            Description = description
        };
    }

    public static ChargeItem CreateGoodDiscountedVATRateItem(string description, decimal amount, decimal quantity)
    {
        var vatRate = 13m;
        return new ChargeItem
        {
            Amount = amount,
            VATRate = vatRate,
            VATAmount = amount / (100M + vatRate) * vatRate,
            ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0011,
            Quantity = quantity,
            Description = description
        };
    }

}