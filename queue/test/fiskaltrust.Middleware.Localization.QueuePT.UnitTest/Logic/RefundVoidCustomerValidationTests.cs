using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Logic;

public class RefundVoidCustomerValidationTests
{
    private const string OriginalReference = "INV-001";
    private static readonly Guid CashBoxId = Guid.Parse("5c4a97c9-954a-46f2-bc3c-583244f5fc61");

    [Fact]
    public void CompareReceiptRequest_WithDifferentCustomerJson_ShouldFail()
    {
        var originalCustomer = new { CustomerVATId = "123456789", CustomerCountry = "PT" };
        var refundCustomer = new { CustomerVATId = "999999999", CustomerCountry = "PT" };
        var originalRequest = CreateBaseReceipt(originalCustomer);
        var refundRequest = CreateBaseReceipt(refundCustomer);

        var (flowControl, value) = RefundValidator.CompareReceiptRequest(OriginalReference, refundRequest, originalRequest);

        flowControl.Should().BeFalse();
        value.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch(OriginalReference, "cbCustomer") + ". Different fields: CustomerVATId");
    }

    [Fact]
    public void CompareReceiptRequest_WithDifferentCustomerTypes_ShouldFail()
    {
        var originalRequest = CreateBaseReceipt("123456789");
        var refundRequest = CreateBaseReceipt(new { CustomerVATId = "123456789" });

        var (flowControl, value) = RefundValidator.CompareReceiptRequest(OriginalReference, refundRequest, originalRequest);

        flowControl.Should().BeFalse();
        value.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch(OriginalReference, "cbCustomer") + ". Different fields: cbCustomer is null on one side");
    }

    [Fact]
    public void CompareReceiptRequest_WithMatchingCustomerJson_ShouldPass()
    {
        var originalCustomer = new { CustomerVATId = "123456789", CustomerCountry = "PT" };
        var refundCustomer = JsonSerializer.Deserialize<JsonElement>("{\"CustomerVATId\":\"123456789\",\"CustomerCountry\":\"PT\"}");
        var originalRequest = CreateBaseReceipt(originalCustomer);
        var refundRequest = CreateBaseReceipt(refundCustomer);

        var (flowControl, value) = RefundValidator.CompareReceiptRequest(OriginalReference, refundRequest, originalRequest);

        flowControl.Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public async Task ValidateVoidAsync_WithCustomerMismatch_ShouldFail()
    {
        var originalRequest = CreateBaseReceipt(new { CustomerVATId = "123456789", CustomerCountry = "PT" });
        var voidRequest = CreateBaseReceipt(new { CustomerVATId = "987654321", CustomerCountry = "PT" });

        var validator = new VoidValidator(new AsyncLazy<IMiddlewareQueueItemRepository>(() =>
            Task.FromResult(new Mock<IMiddlewareQueueItemRepository>().Object)));

        var result = await validator.ValidateVoidAsync(voidRequest, originalRequest, OriginalReference);

        result.Should().Be(ErrorMessagesPT.EEEE_VoidItemsMismatch(OriginalReference));
    }

    private static ReceiptRequest CreateBaseReceipt(object? customer)
    {
        return new ReceiptRequest
        {
            ftCashBoxID = CashBoxId,
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            ftReceiptCaseData = "case-data",
            cbArea = "Area-01",
            cbSettlement = "Settlement-01",
            cbCustomer = customer,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 1,
                    Amount = 10m,
                    VATRate = 23m,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate,
                    Description = "Item",
                    Position = 1
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Quantity = 1,
                    Amount = 10m,
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                    Description = "Cash",
                    Position = 1
                }
            ]
        };
    }
}
