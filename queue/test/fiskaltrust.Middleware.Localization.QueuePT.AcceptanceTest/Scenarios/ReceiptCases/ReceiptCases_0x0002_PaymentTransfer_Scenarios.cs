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
                        "Description": "Cash"
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
                        "Amount": 30,
                        "Description": "Test",
                        "VATRate": 0,
                        "ftChargeItemCase": 5788286605450018968
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 30,
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
}