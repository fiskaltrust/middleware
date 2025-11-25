using System.Reflection.PortableExecutable;
using System.Text.Json;
using Azure;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

public class RefundScenarios : AbstractScenarioTests
{
    #region Scenario 1: Transactions with Refund with no reference should be rejected

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
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        response.ftState.State().Should().Be(State.Error);

        response.ftSignatures[0].Data.Should().EndWith("Validation error [EEEE_PreviousReceiptReference]: EEEE_cbPreviousReceiptReference is mandatory and must be set for this receipt. (Field: cbPreviousReceiptReference, Index: )");
        // also check the signaturedata if the returned error is included
    }

    #endregion

    #region Scenario 2: Transactions with Refund with missing reference should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
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
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
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
                "cbChargeItems": [],
                "cbPayItems": [],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "FIXED"
            }
            """;

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith("The given cbPreviousReceiptReference 'FIXED' didn't match with any of the items in the Queue.");
    }

    #endregion

    #region Scenario 3: Transactions with Refund with reference to multiple receipts should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
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
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
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
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [],
                "cbPayItems": [],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "FIXED-scenario3"
            }
            """;

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith($"The given cbPreviousReceiptReference 'FIXED-scenario3' did match with more than one item in the Queue.");
    }

    #endregion

    #region Scenario 4: Transactions with Refund with reference should match the original

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
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
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
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
                        "Amount": 10,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith($"EEEE_Full refund does not match the original invoice '{originalResponse.cbReceiptReference}'. All articles from the original invoice must be properly refunded with matching quantities and amounts. (Field: , Index: )");
    }

    #endregion

    #region Scenario 5: Transactions with Refund for already refunded receipt should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
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
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
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
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        refundResponse.ftState.State().Should().Be(State.Success);


        (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        refundResponse.ftState.State().Should().Be(State.Error);
        refundResponse.ftSignatures[0].Data.Should().EndWith($"EEEE_A refund for receipt '{originalResponse.cbReceiptReference}' already exists. Multiple refunds for the same receipt are not allowed. (Field: cbPreviousReceiptReference, Index: )");
    }

    #endregion

    #region Scenario 6: Transactions with Refund with multiple references should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
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
                "cbChargeItems": [],
                "cbPayItems": [],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": ["FIXED", "Test"]
            }
            """;

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith("Refunding a receipt is only supported with single references.");
    }

    #endregion

    #region Scenario 7: Transactions with Refund with missmatch in customer

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario7_TransactionWithRefundWithCustomerMismatch_ShouldFail(ReceiptCase receiptCase)
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
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
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
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": -1,
                        "Amount": -20,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789",
                    "CustomerName": "Different Customer"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith($"EEEE_Full refund does not match the original invoice '{originalResponse.cbReceiptReference}'. All articles from the original invoice must be properly refunded with matching quantities and amounts. (Field: , Index: )");
    }
    
    #endregion

    #region Scenario 8: Transactions with Refund for already voided receipt should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario8_TransactionWithRefundForAlreadyVoidedReceipt_ShouldFail(ReceiptCase receiptCase)
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
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
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
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        refundResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, refundResponse.ftSignatures.Select(x => x.Data)));

        (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        refundResponse.ftState.State().Should().Be(State.Error);
        refundResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.EEEE_HasBeenVoidedAlready(originalResponse.cbReceiptReference));
    }

    #endregion
}
