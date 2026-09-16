using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>Key material and receipts the FR SCU tests share.</summary>
public static class FRTestData
{
    public const string Siret = "12345678901234";

    /// <summary>A freshly generated secp256r1 key, exported the way the SCUs expect it.</summary>
    public static (string privateKeyBase64, ECDsa key) CreateKey()
    {
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (Convert.ToBase64String(key.ExportPkcs8PrivateKey()), key);
    }

    public static ReceiptRequest CashSaleRequest() => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        cbTerminalID = "1",
        cbReceiptReference = "receipt-1",
        cbReceiptMoment = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
        Currency = Currency.EUR,
        ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
        cbChargeItems =
        [
            new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
            new ChargeItem { Amount = 5.50m, VATAmount = 0.29m, VATRate = 5.5m, Quantity = 1, Description = "Pain", ftChargeItemCase = ChargeItemCase.DiscountedVatRate1.WithCountry("FR") },
        ],
        cbPayItems =
        [
            new PayItem { Amount = 17.50m, Description = "Especes", ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") },
        ],
    };

    public static ReceiptResponse Response() => new()
    {
        ftCashBoxIdentification = "cashbox-fr",
        ftQueueID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        ftQueueItemID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        ftQueueRow = 1,
        ftReceiptIdentification = "T1",
        ftReceiptMoment = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
        ftState = (State) 0x4652_2000_0000_0000,
        ftSignatures = new List<SignatureItem>(),
    };
}
