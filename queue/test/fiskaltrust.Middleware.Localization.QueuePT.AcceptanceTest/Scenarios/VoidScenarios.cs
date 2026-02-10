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

public class VoidScenarios : AbstractScenarioTests
{
    #region Scenario 1: Transactions with void with no reference should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario1_TransactionWithVoidWithNoReference_ShouldFail(ReceiptCase receiptCase)
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

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        response.ftState.State().Should().Be(State.Error);

        response.ftSignatures[0].Data.Should().EndWith("Validation error [EEEE_PreviousReceiptReference]: EEEE_cbPreviousReceiptReference is mandatory and must be set for this receipt. (Field: cbPreviousReceiptReference, Index: )");
        // also check the signaturedata if the returned error is included


    }

    #endregion

    #region Scenario 2: Transactions with void with missing reference should be rejected

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
    public async Task Scenario2_TransactionWithVoidWithMissingReference_ShouldFail(ReceiptCase receiptCase)
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
            originalResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, originalResponse.ftSignatures.Select(x => x.Data)));

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

            var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
            voidResponse.ftState.State().Should().Be(State.Error);

            voidResponse.ftSignatures[0].Data.Should().EndWith("The given cbPreviousReceiptReference 'FIXED' didn't match with any of the items in the Queue or the items referenced are invalid.");
        }

        #endregion

        #region Scenario 3: Transactions with void with reference to multiple receipts should be rejected

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
    public async Task Scenario3_TransactionWithVoidWithMissingReference_ShouldFail(ReceiptCase receiptCase)
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
            originalResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, originalResponse.ftSignatures.Select(x => x.Data)));
            (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
            originalResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, originalResponse.ftSignatures.Select(x => x.Data)));

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

            var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
            voidResponse.ftState.State().Should().Be(State.Error);

            voidResponse.ftSignatures[0].Data.Should().EndWith($"The given cbPreviousReceiptReference 'FIXED-scenario3' did match with more than one item in the Queue.");
        }

        #endregion

        #region Scenario 4: Transactions with void with reference should match the original

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario4_TransactionWithVoidWithReference_ShouldMatchOriginal(ReceiptCase receiptCase)
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
                        "ftPayItemCase": 5788286605450018835
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
            originalResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, originalResponse.ftSignatures.Select(x => x.Data)));

            // Arrange - Void receipt with charge items should be rejected
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
                            "ftChargeItemCase": 5788286605450018835
                        }
                    ],
                    "cbPayItems": [
                        {
                            "Amount": 10,
                            "Description": "Cash",
                            "ftPayItemCase": 5788286605450018835
                        }
                    ],
                    "cbUser": "Stefan Kert",
                    "cbCustomer": {
                        "CustomerVATId": "123456789"
                    },
                    "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}"
                }
                """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

            var (voidRequest, voidResponse) = await ProcessReceiptAsync(voidReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
            voidResponse.ftState.State().Should().Be(State.Error);
            voidResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.EEEE_VoidItemsMismatch(originalResponse.cbReceiptReference));
        }

        #endregion

        #region Scenario 5: Transactions with Void with multiple references should be rejected

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
    public async Task Scenario6_TransactionsWithVoidWithMultipleReferences_ShouldFail(ReceiptCase receiptCase)
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

        var (voidRequest, voidResponse) = await ProcessReceiptAsync(copyReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        voidResponse.ftState.State().Should().Be(State.Error);

        voidResponse.ftSignatures[0].Data.Should().EndWith("Voiding a receipt is only supported with single references.");
    }

    #endregion

    #region Scenario 7: Void items must have opposite signs

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    public async Task Scenario7_VoidWithSameSignAmountsAndQuantities_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": 1,
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

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
        originalResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, originalResponse.ftSignatures.Select(x => x.Data)));

        var invalidVoidReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 2,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
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

        var (_, invalidVoidResponse) = await ProcessReceiptAsync(invalidVoidReceipt, (long) receiptCase.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        invalidVoidResponse.ftState.State().Should().Be(State.Error);
        invalidVoidResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.EEEE_VoidItemsMismatch(originalResponse.cbReceiptReference));
    }

    #endregion

    #region Scenario 8: Void invoice with discount should succeed

    [Theory]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    public async Task Scenario8_VoidInvoiceWithDiscount_ShouldSucceed(ReceiptCase receiptCase)
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

        var voidReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbPreviousReceiptReference": "{{cbPreviousReceiptReference}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -10,
                        "Description": "Line item 1",
                        "Amount": -120,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450018833
                    },
                    {
                        "Quantity": -1,
                        "Description": "Discount Line item 1",
                        "Amount": 20,
                        "VATRate": 6,
                        "ftChargeItemCase": 5788286605450280977
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
    }

    #endregion

    #region Scenario 9: Refunded receipt cannot be voided

    [Fact]
    public async Task Scenario9_TransactionWithVoidForAlreadyRefundedReceipt_ShouldFail()
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
                        "Quantity": 1,
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

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        originalResponse.ftState.State().Should().Be(State.Success);

        var refundReceipt = """
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

        var (_, refundResponse) = await ProcessReceiptAsync(refundReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund));
        refundResponse.ftState.State().Should().Be(State.Success);

        var voidReceipt = """
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

        var (_, voidResponse) = await ProcessReceiptAsync(voidReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        voidResponse.ftState.State().Should().Be(State.Error);
        voidResponse.ftSignatures[0].Data.Should().Contain("EEEE_CannotVoidRefundedDocument");
    }

    #endregion

    #region Scenario 10: Partially refunded receipt cannot be voided

    [Fact]
    public async Task Scenario10_TransactionWithVoidForPartiallyRefundedReceipt_ShouldFail()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": 2,
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

        var (_, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
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
                        "Quantity": -1,
                        "Amount": -10,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450149907
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -10,
                        "Description": "Cash",
                        "ftPayItemCase": 5788286605450149889
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """.Replace("{{cbPreviousReceiptReference}}", originalResponse.cbReceiptReference);

        var (_, partialRefundResponse) = await ProcessReceiptAsync(partialRefundReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"));
        partialRefundResponse.ftState.State().Should().Be(State.Success);

        var voidReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -2,
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

        var (_, voidResponse) = await ProcessReceiptAsync(voidReceipt, (long) ReceiptCase.InvoiceB2C0x1001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void));
        voidResponse.ftState.State().Should().Be(State.Error);
        voidResponse.ftSignatures[0].Data.Should().Contain("EEEE_CannotVoidPartiallyRefundedDocument");
    }

    #endregion
}
