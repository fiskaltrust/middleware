using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// Tests to verify Transmission Failure 2 signature creation and
/// that LoadInvoiceDocsFromQueueItems correctly reads it.
/// </summary>
public class TransmissionFailure2Tests
{
    #region AADEFactory.LoadInvoiceDocsFromQueueItems integration test

    [Fact]
    public void LoadInvoiceDocsFromQueueItems_WhenTransmissionFailure2SignaturePresent_ShouldSetTransmissionFailure2()
    {
        // This test verifies that the full chain works:
        // 1. Receipt response has "Transmission Failure_2" signature (as now written by our fix)
        // 2. LoadInvoiceDocsFromQueueItems reads it and sets transmissionFailure = 2

        // Arrange
        var factory = new AADEFactory(
            new MasterDataConfiguration
            {
                Account = new AccountMasterData { VatId = "112545020" }
            },
            "https://test.receipts.example.com");

        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Position = 1,
                    Amount = 10.00m,
                    VATRate = 24,
                    VATAmount = 1.94m,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0003,
                    Quantity = 1,
                    Description = "Test Item"
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 10.00m,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                    Quantity = 1,
                    Description = "Cash"
                }
            }
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft1#",
            ftCashBoxIdentification = "TEST",
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem
                {
                    Caption = "invoiceMark",
                    Data = "123456789",
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
                },
                new SignatureItem
                {
                    Caption = "Transmission Failure_2",
                    Data = "Απώλεια Διασύνδεσης Παρόχου - ΑΑΔΕ",
                    ftSignatureFormat = SignatureFormat.Text,
                    ftSignatureType = SignatureTypeGR.MyDataInfo.As<SignatureType>()
                }
            }
        };

        var queueItem = new ftQueueItem
        {
            request = System.Text.Json.JsonSerializer.Serialize(receiptRequest),
            response = System.Text.Json.JsonSerializer.Serialize(receiptResponse)
        };

        // Act
        var invoicesDoc = factory.LoadInvoiceDocsFromQueueItems(new List<ftQueueItem> { queueItem });

        // Assert - the invoice in the doc should have transmissionFailure = 2
        invoicesDoc.Should().NotBeNull();
        invoicesDoc.invoice.Should().HaveCount(1);
        invoicesDoc.invoice[0].transmissionFailureSpecified.Should().BeTrue();
        invoicesDoc.invoice[0].transmissionFailure.Should().Be(2);
    }

    #endregion

    #region SignatureItemFactoryGR unit tests

    [Fact]
    public void AddTransmissionFailure2Signature_ShouldAddCorrectSignature()
    {
        // Arrange
        var request = new ProcessRequest
        {
            ReceiptRequest = new ReceiptRequest
            {
                cbTerminalID = "T1",
                cbReceiptReference = "REF-1",
                cbReceiptMoment = DateTime.UtcNow,
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001,
                cbChargeItems = new List<ChargeItem>(),
                cbPayItems = new List<PayItem>()
            },
            ReceiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                cbReceiptReference = "REF-1",
                ftReceiptIdentification = "ft1#"
            }
        };

        // Act
        SignatureItemFactoryGR.AddTransmissionFailure2Signature(request);

        // Assert
        request.ReceiptResponse.ftSignatures.Should().ContainSingle();
        var sig = request.ReceiptResponse.ftSignatures.First();
        sig.Caption.Should().Be("Transmission Failure_2");
        sig.Data.Should().Be("Απώλεια Διασύνδεσης Παρόχου - ΑΑΔΕ");
        sig.ftSignatureFormat.Should().Be(SignatureFormat.Text);
        sig.ftSignatureType.Should().Be(SignatureTypeGR.MyDataInfo.As<SignatureType>());
    }

    #endregion
}