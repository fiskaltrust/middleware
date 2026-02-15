using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// Focused tests to validate error messages and conditions for delivery note cancellation
/// </summary>
public class CancelDeliveryNoteValidationTests
{
    private const string TEST_MARK = "400001959513705";

    #region VAT ID Validation Tests

    [Fact]
    public async Task CancelDeliveryNote_ShouldShowVatIdError_WhenVatIdIsNull()
    {
        // Arrange
        var scu = CreateSCU(vatId: null);
        var request = CreateCancelRequest();
        var previousReceipt = CreatePreviousReceipt(TEST_MARK);

        // Act
        var result = await scu.ProcessReceiptAsync(request, new List<(ReceiptRequest, ReceiptResponse)> { previousReceipt });

        // Assert - Error is stored in ftSignatures with caption "FAILURE"
        result.ReceiptResponse.ftState.Should().Be(State.Error);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "FAILURE");
        
        var failureSignature = result.ReceiptResponse.ftSignatures.First(s => s.Caption == "FAILURE");
        failureSignature.Data.Should().Be(
            "The VATId is not setup correctly for this Queue. Please check the master data configuration in fiskaltrust.Portal.");
    }

    [Fact]
    public async Task CancelDeliveryNote_ShouldShowVatIdError_WhenVatIdIsEmpty()
    {
        // Arrange
        var scu = CreateSCU(vatId: "");
        var request = CreateCancelRequest();
        var previousReceipt = CreatePreviousReceipt(TEST_MARK);

        // Act
        var result = await scu.ProcessReceiptAsync(request, new List<(ReceiptRequest, ReceiptResponse)> { previousReceipt });

        // Assert
        result.ReceiptResponse.ftState.Should().Be(State.Error);
        var failureSignature = result.ReceiptResponse.ftSignatures.First(s => s.Caption == "FAILURE");
        failureSignature.Data.Should().Be(
            "The VATId is not setup correctly for this Queue. Please check the master data configuration in fiskaltrust.Portal.");
    }
    #endregion

    #region Mark Validation Tests

    [Fact]
    public async Task CancelDeliveryNote_ShouldShowMarkError_WhenMarkIsNull()
    {
        // Arrange
        var scu = CreateSCU(vatId: "EL123456789");
        var request = CreateCancelRequest();
        var previousReceipt = CreatePreviousReceipt(mark: null); // NO MARK

        // Act
        var result = await scu.ProcessReceiptAsync(request, new List<(ReceiptRequest, ReceiptResponse)> { previousReceipt });

        // Assert - Exact error message validation
        result.ReceiptResponse.ftState.Should().Be(State.Error);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "FAILURE");
        
        var failureSignature = result.ReceiptResponse.ftSignatures.First(s => s.Caption == "FAILURE");
        failureSignature.Data.Should().Be(
            "Cannot void delivery note: The mark of the delivery note to cancel is missing. Please provide the mark in the cbPreviousReceiptReference.");
    }

    [Fact]
    public async Task CancelDeliveryNote_ShouldShowMarkError_WhenMarkIsEmpty()
    {
        // Arrange
        var scu = CreateSCU(vatId: "EL123456789");
        var request = CreateCancelRequest();
        var previousReceipt = CreatePreviousReceipt(mark: ""); // EMPTY MARK

        // Act
        var result = await scu.ProcessReceiptAsync(request, new List<(ReceiptRequest, ReceiptResponse)> { previousReceipt });

        // Assert
        result.ReceiptResponse.ftState.Should().Be(State.Error);
        var failureSignature = result.ReceiptResponse.ftSignatures.First(s => s.Caption == "FAILURE");
        failureSignature.Data.Should().Be(
            "Cannot void delivery note: The mark of the delivery note to cancel is missing. Please provide the mark in the cbPreviousReceiptReference.");
    }

    [Fact]
    public async Task CancelDeliveryNote_ShouldShowMarkError_WhenSignaturesArrayIsEmpty()
    {
        // Arrange
        var scu = CreateSCU(vatId: "EL123456789");
        var request = CreateCancelRequest();
        var previousReceipt = CreatePreviousReceipt(mark: null, includeSignatures: false); // NO SIGNATURES AT ALL

        // Act
        var result = await scu.ProcessReceiptAsync(request, new List<(ReceiptRequest, ReceiptResponse)> { previousReceipt });

        // Assert
        result.ReceiptResponse.ftState.Should().Be(State.Error);
        var failureSignature = result.ReceiptResponse.ftSignatures.First(s => s.Caption == "FAILURE");
        failureSignature.Data.Should().Contain("mark of the delivery note to cancel is missing");
    }

    #endregion

    #region Helper Methods

    private MyDataSCU CreateSCU(string? vatId)
    {
        var masterData = new MasterDataConfiguration
        {
            Account = new AccountMasterData { VatId = vatId }
        };
        return new MyDataSCU("user", "key", "https://test.api", "https://receipt", sandbox: false, masterData);
    }
    private ProcessRequest CreateCancelRequest(
        bool includeVoidFlag = true,
        bool includeTransportFlag = true,
        ReceiptCase receiptCase = ReceiptCase.DeliveryNote0x0005)
    {
        var ftReceiptCase = (ReceiptCase)0x4752_0000_0000_0000;
        ftReceiptCase = ftReceiptCase.WithCase(receiptCase);
        if (includeVoidFlag) ftReceiptCase = ftReceiptCase.WithFlag(ReceiptCaseFlags.Void);
        if (includeTransportFlag) ftReceiptCase = ftReceiptCase.WithFlag(ReceiptCaseFlagsGR.HasTransportInformation);

        return new ProcessRequest
        {
            ReceiptRequest = new ReceiptRequest
            {
                cbTerminalID = "T1",
                cbReceiptReference = "CANCEL-123",
                cbReceiptMoment = DateTime.UtcNow,
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = ftReceiptCase,
                cbChargeItems = new List<ChargeItem> { new ChargeItem { Quantity = -1, Amount = 0 } },
                cbPayItems = new List<PayItem>()
            },
            ReceiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                cbReceiptReference = "CANCEL-123",
                ftReceiptIdentification = "ft#"
            }
        };
    }

    private (ReceiptRequest, ReceiptResponse) CreatePreviousReceipt(string? mark, bool includeSignatures = true)
    {
        var ftReceiptCase = ((ReceiptCase) 0x4752_0000_0000_0000)
            .WithCase(ReceiptCase.DeliveryNote0x0005)
            .WithFlag(ReceiptCaseFlagsGR.HasTransportInformation);

        var request = new ReceiptRequest
        {
            cbTerminalID = "T1",
            cbReceiptReference = "ORIG-123",
            cbReceiptMoment = DateTime.UtcNow.AddHours(-1),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ftReceiptCase,
            cbChargeItems = new List<ChargeItem> { new ChargeItem { Quantity = 1, Amount = 0 } },
            cbPayItems = new List<PayItem>()
        };

        var signatures = new List<SignatureItem>();
        if (includeSignatures && !string.IsNullOrEmpty(mark))
        {
            signatures.Add(new SignatureItem
            {
                Caption = "invoiceMark",
                Data = mark,
                ftSignatureFormat = (SignatureFormat) ((long) SignatureFormat.Text),
                ftSignatureType = (SignatureType) ((long) GRConstants.BASE_STATE | (long) SignatureTypesGR.MyDataInfo)
            });
        }

        var response = new ReceiptResponse
        {
            cbReceiptReference = "ORIG-123",
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftSignatures = includeSignatures ? signatures : new List<SignatureItem>()
        };

        return (request, response);
    }

    #endregion
}