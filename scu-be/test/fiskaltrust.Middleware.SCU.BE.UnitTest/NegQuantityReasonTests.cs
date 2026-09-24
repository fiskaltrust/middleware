using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.BE.UnitTest;

/// <summary>
/// The FDM refuses a negative quantity that carries no reason (INVALID_REQUEST /
/// EXPECTED_NEGQUANTITYREASON), which is what made every Belgian refund come back
/// accepted-but-unsigned. Only the reason the fiskaltrust receipt model states outright is mapped;
/// an unflagged negative line must stay reason-less so the FDM rejects it instead of it being
/// booked under a guessed reason.
/// </summary>
public class NegQuantityReasonTests
{
    private const long ChargeItemNormalVat = 0x4245_2000_0000_0013;
    private const long ChargeItemNormalVatRefund = 0x4245_2000_0002_0013;
    private const long PointOfSaleReceipt = 0x4245_2000_0000_0001;
    private const long PointOfSaleReceiptRefund = 0x4245_2000_0100_0001;

    private static ZwarteDoosScuBe CreateSut() => new ZwarteDoosScuBe(
        NullLogger<ZwarteDoosScuBe>.Instance,
        NullLoggerFactory.Instance,
        new ZwarteDoosScuConfiguration
        {
            BaseUrl = "https://sdk.zwartedoos.be",
            DeviceId = "FDM00000000",
            SharedSecret = "not-used-in-this-test",
            TimeoutSeconds = 1,
            Language = Language.NL,
            VatNo = "BE0000000097",
            EstNo = "2000000042",
        });

    private static ReceiptRequest CreateReceiptRequest(long receiptCase, long chargeItemCase, decimal quantity, decimal amount) => new ReceiptRequest
    {
        ftCashBoxID = Guid.NewGuid(),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbReceiptMoment = DateTime.UtcNow,
        ftReceiptCase = (ReceiptCase) receiptCase,
        cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                ftChargeItemId = Guid.NewGuid(),
                Position = 1,
                Description = "E2E test article",
                Quantity = quantity,
                Amount = amount,
                VATRate = 21.0m,
                ftChargeItemCase = (ChargeItemCase) chargeItemCase
            }
        },
        cbPayItems = new List<PayItem>()
    };

    [Theory]
    // a charge item flagged as a return
    [InlineData(PointOfSaleReceipt, ChargeItemNormalVatRefund)]
    // a whole receipt flagged as a refund (what a total refund looks like)
    [InlineData(PointOfSaleReceiptRefund, ChargeItemNormalVat)]
    [InlineData(PointOfSaleReceiptRefund, ChargeItemNormalVatRefund)]
    public void GetTransactionInput_NegativeQuantityOnARefund_IsReportedAsREFUND(long receiptCase, long chargeItemCase)
    {
        var transaction = CreateSut().GetTransactionInput(CreateReceiptRequest(receiptCase, chargeItemCase, -1m, -10m));

        transaction.TransactionLines[0].MainProduct.NegQuantityReason.Should().Be(nameof(NegQuantityReason.REFUND));
    }

    [Fact]
    public void GetTransactionInput_NegativeQuantityWithoutARefundFlag_IsLeftWithoutAReason()
    {
        var transaction = CreateSut().GetTransactionInput(CreateReceiptRequest(PointOfSaleReceipt, ChargeItemNormalVat, -1m, -10m));

        transaction.TransactionLines[0].MainProduct.NegQuantityReason.Should().BeNull(
            because: "CORRECTION/WASTE/DAMAGE have no fiskaltrust counterpart yet and guessing one would misstate the Z report");
    }

    [Fact]
    public void GetTransactionInput_PositiveQuantity_CarriesNoReason()
    {
        var transaction = CreateSut().GetTransactionInput(CreateReceiptRequest(PointOfSaleReceipt, ChargeItemNormalVat, 1m, 10m));

        transaction.TransactionLines[0].MainProduct.NegQuantityReason.Should().BeNull();
    }

    /// <summary>
    /// A refund line keeps a POSITIVE unit price and expresses the reversal through the negative
    /// quantity and line total; a negative unit price is refused with VAT_PRICE_CHECK_FAILED.
    /// </summary>
    [Fact]
    public void GetTransactionInput_RefundLine_KeepsAPositiveUnitPrice()
    {
        var transaction = CreateSut().GetTransactionInput(CreateReceiptRequest(PointOfSaleReceiptRefund, ChargeItemNormalVatRefund, -1m, -10m));

        transaction.TransactionLines[0].MainProduct.UnitPrice.Should().Be(10m);
        transaction.TransactionLines[0].LineTotal.Should().Be(-10m);
        transaction.TransactionTotal.Should().Be(-10m);
    }
}
