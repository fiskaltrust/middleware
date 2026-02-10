using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Models;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

public class PartialRefundScenarios : AbstractScenarioTests
{
    #region Scenario 1: Transactions with ParitalRefundScenarios with no reference should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario1_TransactionWithRefundWithNoReference_ShouldFail(ReceiptCase receiptCase)
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT"));
        response.ftState.State().Should().Be(State.Error);

        response.ftSignatures[0].Data.Should().EndWith("Validation error [EEEE_PreviousReceiptReference]: EEEE_cbPreviousReceiptReference is mandatory and must be set for this receipt. (Field: cbPreviousReceiptReference, Index: )");
        // also check the signaturedata if the returned error is included
    }

    #endregion

    #region Scenario 2: Transactions with Refund with missing reference should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario2_TransactionWithRefundWithMissingReference_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));

        // Arrange
        var copyReceipt = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "FIXED"
            }
            """;

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith("The given cbPreviousReceiptReference 'FIXED' didn't match with any of the items in the Queue or the items referenced are invalid.");
    }

    #endregion

    #region Scenario 3: Transactions with Refund with reference to multiple receipts should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario3_TransactionWithRefundWithMissingReference_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "FIXED-scenario3",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
        (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));

        // Arrange
        var copyReceipt = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "FIXED-scenario3"
            }
            """;

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith($"The given cbPreviousReceiptReference 'FIXED-scenario3' did match with more than one item in the Queue.");
    }

    #endregion

    #region Scenario 4: Transactions with Refund with reference should match the original

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.PaymentTransfer0x0002)] => PaymentTransfer receipts cannot be refunded
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario4_TransactionWithRefundWithReference_ShouldMatchOriginal(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    },
                    {
                        "Quantity": 1,
                        "Amount": 30,
                        "Description": "Doing stuff",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));

        // Arrange
        var copyReceipt = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": -40,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);
        var mismatchError = voidResponse.ftSignatures[0].Data;
        (mismatchError.Contains("EEEE_Full refund does not match the original invoice") ||
         mismatchError.Contains("didn't match with any of the items in the Queue"))
            .Should().BeTrue();
    }

    #endregion

    #region Scenario 5: Transactions with Refund for already refunded receipt should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.PaymentTransfer0x0002)] => PaymentTransfer receipts cannot be refunded
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario5_TransactionWithRefundForAlreadyRefundedReceipt_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));

        //5054200000000001
        // Arrange
        var copyReceipt = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT"));
        refundResponse.ftState.State().Should().Be(State.Success);


        (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT"));
        refundResponse.ftState.State().Should().Be(State.Error);
        refundResponse.ftSignatures[0].Data.Should().Contain("[EEEE_PartialRefund]");
    }

    #endregion

    #region Scenario 6: Transactions with Refund with multiple references should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario6_TransactionsWithRefundWithMultipleReferences_ShouldFail(ReceiptCase receiptCase)
    {
        // Arrange
        var copyReceipt = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": ["FIXED", "Test"]
            }
            """;

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith("Partial refunding a receipt is only supported with single references.");
    }

    #endregion

    #region Scenario 7: Mixing refund items and none refund items in partial refund should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario7_MixingRefundItemsAndNoneRefundItemsInPartialRefund_ShouldFail(ReceiptCase receiptCase)
    {
        // Arrange
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));

        // Arrange
        var copyReceipt = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    },
                    {
                        "Quantity": 1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    },
                    {
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);
        // 5054200000000013
        var (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT"));
        refundResponse.ftSignatures[0].Data.Should().EndWith("EEEE_Partial refund contains mixed refund and non-refund items. In Portugal, it is not allowed to mix refunds with non-refunds in the same receipt. All charge items must have the refund flag set for partial refunds. (Field: , Index: )");
    }

    #endregion


    #region Scenario 8: Transactions with Partial Refund for already refunded receipt should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.PaymentTransfer0x0002)] => PaymentTransfer receipts cannot be refunded
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario8_TransactionWithRefundForAlreadyRefundedReceipt_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));

        // Arrange
        var fullRefund = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (refundRequest, refundResponse) = await ProcessReceiptAsync(fullRefund, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        refundResponse.ftState.State().Should().Be(State.Success);

        // Arrange
        var partialRefund = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (partialRefundRequest, partialRefundResponse) = await ProcessReceiptAsync(partialRefund, (long) receiptCase.WithCountry("PT"));
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().EndWith($"EEEE_A refund for receipt '{originalResponse.cbReceiptReference}' already exists. Multiple refunds for the same receipt are not allowed. (Field: cbPreviousReceiptReference, Index: )");
    }

    #endregion


    #region Scenario 9: Transactions with Partial Refund for already voided receipt should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.PaymentTransfer0x0002)] => PaymentTransfer receipts cannot be refunded
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario9_TransactionWithPartialRefundForAlreadyVoidedReceipt_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));

        // Arrange
        var fullRefund = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (refundRequest, refundResponse) = await ProcessReceiptAsync(fullRefund, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        refundResponse.ftState.State().Should().Be(State.Success);

        // Arrange
        var partialRefund = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (partialRefundRequest, partialRefundResponse) = await ProcessReceiptAsync(partialRefund, (long) receiptCase.WithCountry("PT"));
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.EEEE_HasBeenVoidedAlready(originalResponse.cbReceiptReference));
    }

    #endregion

    #region Scenario 10: Partial Refund should fail when amount and quantity exceed original

    [Fact]
    public async Task Scenario10_PartialRefundExceedingOriginalAmountAndQuantity_ShouldFail()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Description": "Line item 1",
                        "Amount": 2,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "On Credit",
                        "Amount": 2,
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success);

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": -3,
                        "Description": "Line item 1",
                        "Amount": -3,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "On Credit",
                        "Amount": -3,
                        "ftPayItemCase": 5788286605450149897
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (partialRefundRequest, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt);
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().Contain("[EEEE_PartialRefund] Total amount to be refunded for item 'Line item 1' exceeds original amount.");
    }

    #endregion

    #region Scenario 11: Third partial refund should fail when original is already fully refunded

    [Fact]
    public async Task Scenario11_ThirdPartialRefundAfterTwoSuccessfulPartialRefunds_ShouldFail()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Description": "Line item 1",
                        "Amount": 2,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "On Credit",
                        "Amount": 2,
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success, because: string.Join(", ", originalResponse.ftSignatures?.Select(s => s.Data) ?? []));

        var partialRefundTemplate = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Description": "Line item 1",
                        "Amount": -1,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "On Credit",
                        "Amount": -1,
                        "ftPayItemCase": 5788286605450149897
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse1) = await ProcessReceiptAsync(partialRefundTemplate);
        partialRefundResponse1.ftState.State().Should().Be(State.Success);

        var (_, partialRefundResponse2) = await ProcessReceiptAsync(partialRefundTemplate);
        partialRefundResponse2.ftState.State().Should().Be(State.Success);

        var (_, partialRefundResponse3) = await ProcessReceiptAsync(partialRefundTemplate);
        partialRefundResponse3.ftState.State().Should().Be(State.Error);
        partialRefundResponse3.ftSignatures[0].Data.Should().Contain("[EEEE_PartialRefund] Total amount to be refunded for item 'Line item 1' exceeds original amount.");
    }

    #endregion

    #region Scenario 12: AccountsReceivable in original cannot be refunded with other payment methods

    [Fact]
    public async Task Scenario12_PartialRefundAccountsReceivableWithCash_ShouldFail()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Description": "Line item 1",
                        "Amount": 1,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "On Credit",
                        "Amount": 1,
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success);

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Description": "Line item 1",
                        "Amount": -1,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "Cash",
                        "Amount": -1,
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt);
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().Contain("Field: PayItem OtherPayments Exceeded");
    }

    #endregion

    #region Scenario 13: Other payments in original cannot be refunded with AccountsReceivable

    [Fact]
    public async Task Scenario13_PartialRefundCashWithAccountsReceivable_ShouldFail()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Description": "Line item 1",
                        "Amount": 1,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "Cash",
                        "Amount": 1,
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success);

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Description": "Line item 1",
                        "Amount": -1,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "On Credit",
                        "Amount": -1,
                        "ftPayItemCase": 5788286605450149897
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt);
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().Contain("Field: PayItem AccountsReceivable Exceeded");
    }

    #endregion

    #region Scenario 14: Mixed payments must respect original payment type buckets

    [Fact]
    public async Task Scenario14_PartialRefundWithMixedPayments_AccountsReceivablePartExceeds_ShouldFail()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Description": "Line item 1",
                        "Amount": 30,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "Cash",
                        "Amount": 10,
                        "ftPayItemCase": 5788286605450018817
                    },
                    {
                        "Description": "On Credit",
                        "Amount": 20,
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success);

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Description": "Line item 1",
                        "Amount": -30,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "Cash",
                        "Amount": -5,
                        "ftPayItemCase": 5788286605450149889
                    },
                    {
                        "Quantity": -1,
                        "Description": "On Credit",
                        "Amount": -25,
                        "ftPayItemCase": 5788286605450149897
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt);
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().Contain("Field: PayItem AccountsReceivable Exceeded");
    }

    #endregion

    #region Scenario 15: Partial refund of discounted item should succeed with proportional amounts

    [Fact]
    public async Task Scenario15_PartialRefundDiscountedItem_ProportionalRefund_ShouldSucceed()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 10,
                        "Description": "Line item 1",
                        "Amount": 120,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    },
                    {
                        "Quantity": 1,
                        "Description": "Discount Line item 1",
                        "Amount": -20,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450280977
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "Cash",
                        "Amount": 100,
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success);

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": -5,
                        "Description": "Line item 1",
                        "Amount": -40,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    },
                    {
                        "Quantity": -1,
                        "Description": "Discount Line item 1",
                        "Amount": -10,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450412049
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "Cash",
                        "Amount": -50,
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt);
        partialRefundResponse.ftState.State().Should().Be(State.Success);
    }

    #endregion

    #region Scenario 16: Partial refund is not possible for already voided receipt

    [Theory]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    public async Task Scenario16_PartialRefundForAlreadyVoidedReceipt_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Description": "Line item 1",
                        "Amount": 100,
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "Cash",
                        "Amount": 100,
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
        originalResponse.ftState.State().Should().Be(State.Success);

        var voidReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Description": "Line item 1",
                        "Amount": -100,
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "Cash",
                        "Amount": -100,
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, voidResponse) = await ProcessReceiptAsync(voidReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        voidResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, voidResponse.ftSignatures.Select(x => x.Data)));

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Description": "Line item 1",
                        "Amount": -50,
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "Cash",
                        "Amount": -50,
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt, (long) receiptCase.WithCountry("PT"));
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.EEEE_HasBeenVoidedAlready(originalResponse.cbReceiptReference));
    }

    #endregion

    #region Scenario 17: Partial refund with wrong signs should fail

    [Fact]
    public async Task Scenario17_PartialRefundWithWrongSigns_ShouldFail()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Description": "Line item 1",
                        "Amount": 2,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "On Credit",
                        "Amount": 2,
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success);

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Description": "Line item 1",
                        "Amount": -1,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "On Credit",
                        "Amount": -1,
                        "ftPayItemCase": 5788286605450149897
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt);
        partialRefundResponse.ftState.State().Should().Be(State.Error);
        partialRefundResponse.ftSignatures[0].Data.Should().Contain("Field: ChargeItem.QuantitySign");
    }

    #endregion

    #region Scenario 18: Partial refund invoice with discount should succeed

    [Theory]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    public async Task Scenario18_PartialRefundInvoiceWithDiscount_ShouldSucceed(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 10,
                        "Description": "Line item 1",
                        "Amount": 120,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    },
                    {
                        "Quantity": 1,
                        "Description": "Discount Line item 1",
                        "Amount": -20,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450280977
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "Cash",
                        "Amount": 100,
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
        originalResponse.ftState.State().Should().Be(State.Success);

        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -5,
                        "Description": "Line item 1",
                        "Amount": -40,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    },
                    {
                        "Quantity": -1,
                        "Description": "Discount Line item 1",
                        "Amount": 10,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450412049
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "Cash",
                        "Amount": -30,
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt, (long) receiptCase.WithCountry("PT"));
        partialRefundResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, partialRefundResponse.ftSignatures.Select(x => x.Data)));
    }

    #endregion

    #region Scenario 19: Failed partial refund must not affect subsequent valid partial refund

    [Fact]
    public async Task Scenario19_FailedPartialRefundMustNotBeConsideredForStateCalculation_ShouldSucceed()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Description": "Line item 1",
                        "Amount": 2,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    }
                ],
                "cbPayItems": [
                    {
                        "Description": "On Credit",
                        "Amount": 2,
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """;

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt);
        originalResponse.ftState.State().Should().Be(State.Success);

        // Intentionally invalid signs: must fail and must not be counted.
        var failedPartialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Description": "Line item 1",
                        "Amount": -2,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "On Credit",
                        "Amount": -2,
                        "ftPayItemCase": 5788286605450149897
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, failedPartialRefundResponse) = await ProcessReceiptAsync(failedPartialRefundReceipt);
        failedPartialRefundResponse.ftState.State().Should().Be(State.Error);

        // This should succeed because previous failed partial refund must not count.
        var validPartialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Description": "Line item 1",
                        "Amount": -1,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450149905
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Description": "On Credit",
                        "Amount": -1,
                        "ftPayItemCase": 5788286605450149897
                    }
                ],
                "ftReceiptCase": 5788286605450022913,
                "cbUser": "AT1",
                "cbCustomer": {
                    "CustomerName": "Cliente AT",
                    "CustomerStreet": "Morada AT",
                    "CustomerZip": "1000-000",
                    "CustomerCity": "Lisboa",
                    "CustomerVATId": "999999990"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, validPartialRefundResponse) = await ProcessReceiptAsync(validPartialRefundReceipt);
        validPartialRefundResponse.ftState.State().Should().Be(State.Success);
    }

    #endregion
}
