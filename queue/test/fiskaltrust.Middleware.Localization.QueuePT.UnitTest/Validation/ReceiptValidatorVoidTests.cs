using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Validation;

public class ReceiptValidatorVoidTests
{
    [Fact]
    public async Task Validate_VoidWorkingDocument_WithInvoicedReference_ShouldFail()
    {
        var now = DateTime.UtcNow;
        var workingRef = "WD-001";

        var originalRequest = CreateWorkingDocumentRequest(workingRef, now);
        var originalResponse = CreateReceiptResponse("origin#WD 2024/0001", now);

        var voidRequest = CreateVoidWorkingDocumentRequest(workingRef, now, originalRequest.cbChargeItems ?? new List<ChargeItem>());
        var voidResponse = CreateReceiptResponse("void#WD 2024/0002", now);
        voidResponse.ftStateData = new MiddlewareStateData
        {
            PreviousReceiptReference = new List<Receipt>
            {
                new Receipt { Request = originalRequest, Response = originalResponse }
            }
        };

        var repository = new Mock<IMiddlewareQueueItemRepository>();
        var queueItems = new List<ftQueueItem>
        {
            CreateInvoiceQueueItem(workingRef, now, "FT 2024/0001")
        };

        repository.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(queueItems[0]);
        repository.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(queueItems.ToAsyncEnumerable());

        var validator = new ReceiptValidator(voidRequest, voidResponse, new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));

        var results = await validator.ValidateAndCollectAsync(new ReceiptValidationContext
        {
            IsRefund = false,
            GeneratesSignature = false,
            IsHandwritten = false,
            NumberSeries = null
        });

        results.AllErrors.Should().ContainSingle(e => e.Code == "EEEE_WorkingDocumentAlreadyInvoiced");
    }

    [Fact]
    public async Task Validate_VoidWorkingDocument_WithoutInvoiceReference_ShouldNotReturnInvoicedError()
    {
        var now = DateTime.UtcNow;
        var workingRef = "WD-002";

        var originalRequest = CreateWorkingDocumentRequest(workingRef, now);
        var originalResponse = CreateReceiptResponse("origin#WD 2024/0002", now);

        var voidRequest = CreateVoidWorkingDocumentRequest(workingRef, now, originalRequest.cbChargeItems ?? new List<ChargeItem>());
        var voidResponse = CreateReceiptResponse("void#WD 2024/0003", now);
        voidResponse.ftStateData = new MiddlewareStateData
        {
            PreviousReceiptReference = new List<Receipt>
            {
                new Receipt { Request = originalRequest, Response = originalResponse }
            }
        };

        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());
        repository.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(Enumerable.Empty<ftQueueItem>().ToAsyncEnumerable());

        var validator = new ReceiptValidator(voidRequest, voidResponse, new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));

        var results = await validator.ValidateAndCollectAsync(new ReceiptValidationContext
        {
            IsRefund = false,
            GeneratesSignature = false,
            IsHandwritten = false,
            NumberSeries = null
        });

        results.AllErrors.Should().NotContain(e => e.Code == "EEEE_WorkingDocumentAlreadyInvoiced");
    }

    private static ReceiptRequest CreateWorkingDocumentRequest(string receiptReference, DateTime moment)
    {
        return new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x0007).WithCountry("PT"),
            cbReceiptReference = receiptReference,
            cbTerminalID = "TERM-001",
            cbUser = "user",
            cbReceiptMoment = moment,
            Currency = Currency.EUR,
            cbChargeItems = new List<ChargeItem>
            {
                CreateChargeItem(moment)
            },
            cbPayItems = new List<PayItem>()
        };
    }

    private static ReceiptRequest CreateVoidWorkingDocumentRequest(string previousReceiptReference, DateTime moment, IEnumerable<ChargeItem> originalChargeItems)
    {
        return new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) ((long) ((ReceiptCase) 0x0007).WithCountry("PT") | (long) ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = previousReceiptReference,
            cbReceiptReference = $"VOID-{previousReceiptReference}",
            cbTerminalID = "TERM-001",
            cbUser = "user",
            cbReceiptMoment = moment,
            Currency = Currency.EUR,
            cbChargeItems = originalChargeItems.Select(CloneChargeItem).ToList(),
            cbPayItems = new List<PayItem>()
        };
    }

    private static ReceiptResponse CreateReceiptResponse(string receiptIdentification, DateTime moment)
    {
        return new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = receiptIdentification,
            ftReceiptMoment = moment,
            ftState = (State) 0x5054_0000_0000_0000
        };
    }

    private static ChargeItem CreateChargeItem(DateTime moment)
    {
        var amount = 10m;
        var vatRate = 23m;
        var vatAmount = amount - (amount / (1 + vatRate / 100m));
        return new ChargeItem
        {
            Position = 1,
            ProductNumber = "ITEM-001",
            Description = "Item",
            Quantity = 1m,
            Unit = "UN",
            UnitPrice = amount,
            Amount = amount,
            VATRate = vatRate,
            VATAmount = vatAmount,
            ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0001,
            Moment = moment
        };
    }

    private static ChargeItem CloneChargeItem(ChargeItem source)
    {
        return new ChargeItem
        {
            Position = source.Position,
            ProductNumber = source.ProductNumber,
            Description = source.Description,
            Quantity = source.Quantity,
            Unit = source.Unit,
            UnitPrice = source.UnitPrice,
            Amount = source.Amount,
            VATRate = source.VATRate,
            VATAmount = source.VATAmount,
            ftChargeItemCase = source.ftChargeItemCase,
            Moment = source.Moment
        };
    }

    private static ftQueueItem CreateInvoiceQueueItem(string previousReceiptReference, DateTime moment, string invoiceIdentifier)
    {
        var invoiceRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"),
            cbPreviousReceiptReference = previousReceiptReference,
            cbReceiptReference = "INV-001",
            cbTerminalID = "TERM-001",
            cbUser = "user",
            cbReceiptMoment = moment,
            Currency = Currency.EUR,
            cbChargeItems = new List<ChargeItem>
            {
                CreateChargeItem(moment)
            },
            cbPayItems = new List<PayItem>()
        };

        var invoiceResponse = new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = $"queue#{invoiceIdentifier}",
            ftReceiptMoment = moment,
            ftState = (State) 0x5054_0000_0000_0000
        };

        return new ftQueueItem
        {
            cbReceiptReference = invoiceRequest.cbReceiptReference,
            cbTerminalID = invoiceRequest.cbTerminalID,
            request = JsonSerializer.Serialize(invoiceRequest),
            response = JsonSerializer.Serialize(invoiceResponse)
        };
    }
}

