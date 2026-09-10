using System;
using System.Linq;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    /// <summary>
    /// #514: a multi-use voucher redeemed as a negative charge item (0x..48) used to become a printRecItem with a
    /// negative unit price, which the printer rejects. It is now a "sconto a pagare" payment (type 6, index 1),
    /// the same command pay item 0x06 produces, in sale, refund and void documents alike.
    /// </summary>
    public class MultiUseVoucherRedeemTests
    {
        private const long SaleReceipt = 0x4954_2000_0000_0001;
        private const long RefundReceipt = 0x4954_2000_0100_0001;
        private const long VoidReceipt = 0x4954_2000_0004_0001;
        private const long GroupingFlag = 0x0000_0000_0800_0000;
        private const long RefundItem = 0x0000_0000_0002_0000;
        private const long VoidItem = 0x0000_0000_0001_0000;

        private static readonly EpsonRTPrinterSCUConfiguration _configuration = new();
        private static readonly DateTime _referenceDate = new(2026, 9, 8);

        private static ChargeItem Food(decimal amount, long flags = 0) => new()
        {
            Position = 100,
            Quantity = 1,
            Description = "Food",
            Amount = amount,
            VATRate = 10m,
            ftChargeItemCase = 0x4954_2000_0000_0001 | flags
        };

        private static ChargeItem Voucher(decimal amount, long flags = 0, int position = 200) => new()
        {
            Position = position,
            Quantity = 1,
            Description = "Gutschein",
            Amount = amount,
            ftChargeItemCase = 0x4954_2000_0000_0048 | flags
        };

        private static ChargeItem Coperto(int position) => new()
        {
            Position = position,
            Quantity = 1,
            Description = "Coperto",
            Amount = 2m,
            VATRate = 22m,
            ftChargeItemCase = 0x4954_2000_0000_0003
        };

        private static PayItem Cash(decimal amount, long flags = 0) => new()
        {
            Quantity = 1,
            Description = "Contanti",
            Amount = amount,
            ftPayItemCase = 0x4954_2000_0000_0001 | flags
        };

        private static ReceiptRequest CreateReceipt(long receiptCase, ChargeItem[] chargeItems, params PayItem[] payItems) => new()
        {
            ftReceiptCase = receiptCase,
            cbReceiptReference = "514",
            cbReceiptMoment = new DateTime(2026, 9, 8, 12, 0, 0),
            cbChargeItems = chargeItems,
            cbPayItems = payItems,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount)
        };

        private static FiscalReceipt Sale(ReceiptRequest request) => EpsonCommandFactory.CreateInvoiceRequestContent(_configuration, request);
        private static FiscalReceipt Refund(ReceiptRequest request) => EpsonCommandFactory.CreateRefundRequestContent(_configuration, request, 12, 97, _referenceDate, "99IEB077964");
        private static FiscalReceipt Void(ReceiptRequest request) => EpsonCommandFactory.CreateVoidRequestContent(_configuration, request, 12, 97, _referenceDate, "99IEB077964");

        private static void ShouldBeVoucherPayment(TotalAndMessage total, decimal amount)
        {
            total.PrintRecTotal.Should().NotBeNull();
            total.PrintRecTotal!.PaymentType.Should().Be(6);
            total.PrintRecTotal.Index.Should().Be(1);
            total.PrintRecTotal.Payment.Should().Be(amount);
            total.PrintRecTotal.Description.Should().Be("Gutschein");
        }

        private static void ShouldBeCashPayment(TotalAndMessage total, decimal amount)
        {
            total.PrintRecTotal.Should().NotBeNull();
            total.PrintRecTotal!.PaymentType.Should().Be(0);
            total.PrintRecTotal.Index.Should().Be(0);
            total.PrintRecTotal.Payment.Should().Be(amount);
        }

        [Fact]
        public void Sale_RedeemAsChargeItem_IsAScontoAPagarePaymentBeforeTheCash()
        {
            var request = CreateReceipt(SaleReceipt, new[] { Food(100m), Voucher(-20m) }, Cash(80m));

            var content = Sale(request);

            content.ItemAndMessages.Where(x => x.PrintRecItem != null).Should().ContainSingle().Which.PrintRecItem!.Description.Should().Be("Food");
            content.ItemAndMessages.Should().OnlyContain(x => x.PrintRecVoidItem == null && x.PrintRecItemAdjustment == null);
            content.RecTotalAndMessages.Should().HaveCount(2);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 20m);
            ShouldBeCashPayment(content.RecTotalAndMessages[1], 80m);

            var xml = SoapSerializer.Serialize(content);
            xml.Should().NotContain("unitPrice=\"-").And.NotContain("quantity=\"-");
            xml.Should().Contain("paymentType=\"6\"");
        }

        [Fact]
        public void Sale_GroupingRequest_RedeemIsNeitherAVoidItemNorAnAdjustment()
        {
            var request = CreateReceipt(SaleReceipt | GroupingFlag, new[] { Food(100m), Voucher(-20m), Voucher(-10m, position: 101) }, Cash(70m));

            var content = Sale(request);

            content.ItemAndMessages.Where(x => x.PrintRecItem != null).Should().ContainSingle().Which.PrintRecItem!.Description.Should().Be("Food");
            content.ItemAndMessages.Should().OnlyContain(x => x.PrintRecVoidItem == null && x.PrintRecItemAdjustment == null);
            content.RecTotalAndMessages.Should().HaveCount(3);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 20m);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[1], 10m);
            ShouldBeCashPayment(content.RecTotalAndMessages[2], 70m);
        }

        [Fact]
        public void Sale_VoucherCoversTheWholeReceipt_NeedsNoZeroCashFallback()
        {
            var request = CreateReceipt(SaleReceipt, new[] { Food(100m), Voucher(-100m) });

            var content = Sale(request);

            content.RecTotalAndMessages.Should().ContainSingle();
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 100m);
        }

        [Fact]
        public void Refund_MirrorsTheSale_VoucherIsAScontoAPagarePayment()
        {
            var request = CreateReceipt(RefundReceipt, new[] { Food(-100m, RefundItem), Voucher(20m, RefundItem) }, Cash(-80m, RefundItem));

            var content = Refund(request);

            content.PrintRecRefund.Should().ContainSingle().Which.Description.Should().Be("Food");
            content.RecTotalAndMessages.Should().HaveCount(2);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 20m);
            ShouldBeCashPayment(content.RecTotalAndMessages[1], 80m);
        }

        [Fact]
        public void Void_MirrorsTheSale_VoucherIsAScontoAPagarePayment()
        {
            var request = CreateReceipt(VoidReceipt, new[] { Food(-100m, VoidItem), Voucher(20m, VoidItem) }, Cash(-80m, VoidItem));

            var content = Void(request);

            content.PrintRecVoid.Should().ContainSingle().Which.Description.Should().Be("Food");
            content.RecTotalAndMessages.Should().HaveCount(2);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 20m);
            ShouldBeCashPayment(content.RecTotalAndMessages[1], 80m);
        }

        /// <summary>The sale of the voucher itself (positive line) is unchanged: an item on the NS department 11.</summary>
        [Fact]
        public void Sale_VoucherSaleLine_IsStillAnItemOnDepartment11()
        {
            var request = CreateReceipt(SaleReceipt, new[] { Voucher(100m) }, Cash(100m));

            var content = Sale(request);

            var item = content.ItemAndMessages.Should().ContainSingle().Which.PrintRecItem;
            item.Should().NotBeNull();
            item!.Department.Should().Be(11);
            item.UnitPrice.Should().Be(100m);
            content.RecTotalAndMessages.Should().ContainSingle();
            ShouldBeCashPayment(content.RecTotalAndMessages[0], 100m);
        }

        /// <summary>Refunding a voucher sale used to put the line on department -1 (GetVatGroup of 0x48).</summary>
        [Fact]
        public void Refund_OfAVoucherSale_IsARefundLineOnDepartment11()
        {
            var request = CreateReceipt(RefundReceipt, new[] { Voucher(-100m, RefundItem) }, Cash(-100m, RefundItem));

            var content = Refund(request);

            var line = content.PrintRecRefund.Should().ContainSingle().Which;
            line.Department.Should().Be(11);
            line.Amount.Should().Be(100m);
            content.RecTotalAndMessages.Should().ContainSingle();
            ShouldBeCashPayment(content.RecTotalAndMessages[0], 100m);
        }

        /// <summary>The redeem filter runs before the grouping, so a group can lose its head: its members must still be printed.</summary>
        [Fact]
        public void Sale_GroupingRequest_VoucherAsGroupHead_PrintsTheChildrenAsItems()
        {
            var request = CreateReceipt(SaleReceipt | GroupingFlag, new[] { Food(100m), Voucher(-20m), Coperto(201) }, Cash(82m));

            var content = Sale(request);

            content.ItemAndMessages.Where(x => x.PrintRecItem != null).Select(x => x.PrintRecItem!.Description).Should().Equal("Food", "Coperto");
            content.ItemAndMessages.Should().OnlyContain(x => x.PrintRecVoidItem == null && x.PrintRecItemAdjustment == null);
            content.RecTotalAndMessages.Should().HaveCount(2);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 20m);
            ShouldBeCashPayment(content.RecTotalAndMessages[1], 82m);
        }

        /// <summary>Without pay items the rest is paid in cash: payment 0 means "the whole amount still due" on the Epson.</summary>
        [Fact]
        public void Sale_NoPayItems_VoucherBelowTheTotal_PaysTheRestInCash()
        {
            var request = CreateReceipt(SaleReceipt, new[] { Food(100m), Voucher(-20m) });

            var content = Sale(request);

            content.RecTotalAndMessages.Should().HaveCount(2);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 20m);
            ShouldBeCashPayment(content.RecTotalAndMessages[1], 0m);
        }

        [Fact]
        public void Sale_NoPayItems_NoVoucher_StillPaysInCash()
        {
            var request = CreateReceipt(SaleReceipt, new[] { Food(100m) });

            var content = Sale(request);

            content.RecTotalAndMessages.Should().ContainSingle();
            ShouldBeCashPayment(content.RecTotalAndMessages[0], 0m);
        }

        /// <summary>Without pay items the rest is paid in cash: payment 0 means "the whole amount still due" on the Epson.</summary>
        [Fact]
        public void Sale_NoPayItems_VoucherBelowTheTotal_PaysTheRestInCash()
        {
            var request = CreateReceipt(SaleReceipt, new[] { Food(100m), Voucher(-20m) });

            var content = Sale(request);

            content.RecTotalAndMessages.Should().HaveCount(2);
            ShouldBeVoucherPayment(content.RecTotalAndMessages[0], 20m);
            ShouldBeCashPayment(content.RecTotalAndMessages[1], 0m);
        }

        [Fact]
        public void Sale_NoPayItems_NoVoucher_StillPaysInCash()
        {
            var request = CreateReceipt(SaleReceipt, new[] { Food(100m) });

            var content = Sale(request);

            content.RecTotalAndMessages.Should().ContainSingle();
            ShouldBeCashPayment(content.RecTotalAndMessages[0], 0m);
        }
    }
}
