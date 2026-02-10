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

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios.ReceiptCases;

public class ReceiptCases_0x0002_PaymentTransfer_Scenarios : AbstractScenarioTests
{
    #region Scenario 0: Transactions with PaymentTransfer should work

    [Fact]
    public async Task Scenari0_Positive_Base()
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
                        "Amount": 10,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
                        "Description": "On Credit",
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));

        // 0x5054_2000_0000_0098
        // 0x5054_2000_0000_0009
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
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
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

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Success);

        voidResponse.ftReceiptIdentification.Should().Contain("RG");
        // voidResponse.ftSignatures[0].Data.Should().EndWith($"EEEE_Full refund does not match the original invoice '{originalResponse.cbReceiptReference}'. All articles from the original invoice must be properly refunded with matching quantities and amounts. (Field: , Index: )");
    }

    #endregion

    #region Scenario 1: Transactions without cbPreviousReceiptReference should fail

    [Fact]
    public async Task Scenario1_TransactionsWithoutcbPreviousReceiptReferenceShouldFail()
    {
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
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
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

        var (request, response) = await ProcessReceiptAsync(json, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        response.ftState.State().Should().Be(State.Error);

        response.ftSignatures[0].Data.Should().EndWith("Validation error [EEEE_PreviousReceiptReference]: EEEE_cbPreviousReceiptReference is mandatory and must be set for this receipt. (Field: cbPreviousReceiptReference, Index: )");
        // also check the signaturedata if the returned error is included }
    }

    #endregion

    #region Scenario 2: Transactions with PaymentTransfer with missing reference should be rejected

    [Fact]
    public async Task Scenario2_TransactionWithPaymentTransferWithMissingReference_ShouldFail()
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

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));

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

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith("The given cbPreviousReceiptReference 'FIXED' didn't match with any of the items in the Queue.");
    }

    #endregion

    #region Scenario 3: Transactions with PaymentTransfer with reference to multiple receipts should be rejected

    [Fact]
    public async Task Scenario3_TransactionWithPaymentTransferWithMissingReference_ShouldFail()
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

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));
        (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));

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

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith($"The given cbPreviousReceiptReference 'FIXED-scenario3' did match with more than one item in the Queue.");
    }

    #endregion

    #region Scenario 4: Transactions with PaymentTransfer with reference should match the original

    [Fact]
    public async Task Scenario4_TransactionWithPaymentTransferWithReference_ShouldMatchOriginal()
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
                        "Amount": 10,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
                        "Description": "On Credit",
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));

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
                        "Amount": 30,
                        "Description": "Test",
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 30,
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

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().Contain($"The total amount of pay items in the payment transfer receipt must match the total amount of pay items in the original invoice receipt");
    }

    #endregion

    #region Scenario 5: Transactions with PaymentTransfer for already payed receipt should fail

    [Fact]
    public async Task Scenario5_TransactionWithPaymentTransferForAlreadyPayedReceipt_ShouldFail()
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
                        "Amount": 10,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
                        "Description": "On Credit",
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));

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
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
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

        var (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        refundResponse.ftState.State().Should().Be(State.Success);


        (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        refundResponse.ftState.State().Should().Be(State.Error);
        refundResponse.ftSignatures[0].Data.Should().EndWith($"EEEE_A refund for receipt '{originalResponse.cbReceiptReference}' already exists. Multiple refunds for the same receipt are not allowed. (Field: cbPreviousReceiptReference, Index: )");
    }

    #endregion

    #region Scenario 7: Transactions with PaymentTransfer must use Accounts Receivable for chargeitem

    [Fact]
    public async Task Scenario7_TransactionsWithPaymentTransferMustUseAccountsReceivableForChargeItem_ShouldFail()
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

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));

        // 0x5054_2000_0000_0013
        // 0x5054_2000_0000_0001

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
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
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

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        voidResponse.ftState.State().Should().Be(State.Error);
        voidResponse.ftSignatures[0].Data.Should().Contain($"EEEE_PaymentTransfer pay items require at least one accounts receivable charge item in the receipt.");
    }

    #endregion

    #region Scenario 8: Transactions with PaymentTransfer for already voided receipt should fail

    [Fact]
    public async Task Scenario8_TransactionWithPaymentTransferForAlreadyVoidedReceipt_ShouldFail()
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
                        "Amount": 10,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
                        "Description": "On Credit",
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT"));

        var voidReceipt = """       
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

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(voidReceipt, (long) ReceiptCase.InvoiceUnknown0x1000.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));

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
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 10,
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

        var (refundRequest, refundResponse) = await ProcessReceiptAsync(copyReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        refundResponse.ftState.State().Should().Be(State.Error);
        refundResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.EEEE_HasBeenVoidedAlready(originalResponse.cbReceiptReference));
    }

    #endregion

    #region Scenario 9: Invoice on credit -> Partial Refund -> Payment Transfer for full amount should fail

    [Fact]
    public async Task Scenario9_InvoiceOnCredit_PartialRefund_PaymentTransferFullAmount_ShouldFail()
    {
        // Step 1: Create an invoice on credit (not paid yet) for 100 EUR
        var invoiceReceipt = """
            {
                "cbReceiptReference": "invoice-partial-refund-001",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Amount": 100,
                        "Description": "Product A",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 100,
                        "Description": "On Credit",
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "cbUser": "Test User",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (invoiceRequest, invoiceResponse) = await ProcessReceiptAsync(invoiceReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        invoiceResponse.ftState.State().Should().Be(State.Success, because: "Invoice on credit should succeed. Errors: " + string.Join(", ", invoiceResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 2: Partially refund the invoice - return 1 item (50 EUR)
        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -50,
                        "Description": "Product A",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -50,
                        "Description": "Cash Refund",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Test User",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "invoice-partial-refund-001"
            }
            """;

        var (partialRefundRequest, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        partialRefundResponse.ftState.State().Should().Be(State.Success, because: "Partial refund should succeed. Errors: " + string.Join(", ", partialRefundResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 3: Try to pay the full original amount (100 EUR) via Payment Transfer - should fail
        // because only 50 EUR remains after partial refund
        var paymentTransferReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 100,
                        "Description": "Payment for Invoice",
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 100,
                        "Description": "Cash Payment",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "invoice-partial-refund-001"
            }
            """;

        var (paymentTransferRequest, paymentTransferResponse) = await ProcessReceiptAsync(paymentTransferReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        
        // Assert: Should fail because payment amount exceeds remaining amount
        paymentTransferResponse.ftState.State().Should().Be(State.Error, because: "Payment transfer for full amount should fail after partial refund");
        paymentTransferResponse.ftSignatures.Should().Contain(s => s.Data.Contains("EEEE_Payment transfer amount (100,00€) exceeds the remaining amount (50,00€) for invoice 'invoice-partial-refund-001'. Already refunded: 50,00€."));
    }

    #endregion

    #region Scenario 10: Invoice on credit -> Partial Refund -> Payment Transfer for remaining amount should succeed

    [Fact]
    public async Task Scenario10_InvoiceOnCredit_PartialRefund_PaymentTransferRemainingAmount_ShouldSucceed()
    {
        // Step 1: Create an invoice on credit (not paid yet) for 100 EUR
        var invoiceReceipt = """
            {
                "cbReceiptReference": "invoice-partial-refund-002",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Amount": 100,
                        "Description": "Product A",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 100,
                        "Description": "On Credit",
                        "ftPayItemCase": 5788286605450018825
                    }
                ],
                "cbUser": "Test User",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (invoiceRequest, invoiceResponse) = await ProcessReceiptAsync(invoiceReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        invoiceResponse.ftState.State().Should().Be(State.Success, because: "Invoice on credit should succeed. Errors: " + string.Join(", ", invoiceResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 2: Partially refund the invoice - return 1 item (50 EUR)
        var partialRefundReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -50,
                        "Description": "Product A",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -50,
                        "Description": "Cash Refund",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Test User",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "invoice-partial-refund-002"
            }
            """;

        var (partialRefundRequest, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        partialRefundResponse.ftState.State().Should().Be(State.Success, because: "Partial refund should succeed. Errors: " + string.Join(", ", partialRefundResponse.ftSignatures?.Select(s => s.Data) ?? []));

        // Step 3: Pay the remaining amount (50 EUR) via Payment Transfer - should succeed
        var paymentTransferReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 50,
                        "Description": "Payment for Invoice",
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 50,
                        "Description": "Cash Payment",
                        "ftPayItemCase": 5788286605450018817
                    }
                ],
                "cbUser": "Test User",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "invoice-partial-refund-002"
            }
            """;

        var (paymentTransferRequest, paymentTransferResponse) = await ProcessReceiptAsync(paymentTransferReceipt, (long) ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"));
        
        // Assert: Should succeed because payment amount matches remaining amount
        paymentTransferResponse.ftState.State().Should().Be(State.Success, because: "Payment transfer for remaining amount should succeed. Errors: " + string.Join(", ", paymentTransferResponse.ftSignatures?.Select(s => s.Data) ?? []));
        paymentTransferResponse.ftReceiptIdentification.Should().Contain("RG", "Payment transfer should generate an RG document");
    }

    #endregion
}