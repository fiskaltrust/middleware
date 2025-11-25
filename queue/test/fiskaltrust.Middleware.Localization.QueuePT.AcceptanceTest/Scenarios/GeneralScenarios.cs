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

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

public class GeneralScenarios : AbstractScenarioTests
{
    #region Scenario 1: Transactions that are part of fiscalization should be rejected if no cbUser is provided

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario1_TransactionWithoutUser_ShouldFail(ReceiptCase receiptCase)
    {
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Amount": 20,
                        "Description": "tes",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
                    }
                ]
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT"));
        response.ftState.State().Should().Be(State.Error);
    }

    #endregion

    #region Scenario 2: Transactions with a cbUser with length of below 3 characters should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario2_TransactionWithoutUserWithShortLength_ShouldFail(ReceiptCase receiptCase)
    {
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Amount": 20,
                        "Description": "tes",
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
                "cbUser": "St"
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT"));
        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 3: Transactions with article description of below 3 characters should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario3_TransactionWithShortArticleDescription_ShouldFail(ReceiptCase receiptCase)
    {
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Amount": 20,
                        "Description": "te",
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
                "cbUser": "Stefan Kert"
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT"));
        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 4: Transactions with negative amount on a usual sales should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario4_TransactionWithNegativeAmount_ShouldFail(ReceiptCase receiptCase)
    {
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Amount": -20,
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
                "cbUser": "Stefan Kert"
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT"));
        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 5: Transactions with negative quantity on a usual sales should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario5_TransactionWithNegativeQuantity_ShouldFail(ReceiptCase receiptCase)
    {
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
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
                "cbUser": "Stefan Kert"
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT"));
        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 6: Transactions with illegal VAT Rate should be rejected

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario6_TransactionWithIllegalVATRate_ShouldFail(ReceiptCase receiptCase)
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
                        "VATRate": 22,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 20,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert"
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase.WithCountry("PT"));
        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 7: Transactions with discount that exceed the total of the lineitem should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario7_TransactionWithDiscountExceedingTotal_ShouldFail(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Amount": 55,
                        "Quantity": 100,
                        "Description": "Article 1",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    },
                    {
                        "Amount": -55.84,
                        "Description": "Desconto",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450280979
                    },
                    {
                        "Amount": 13.8,
                        "Description": "Line item 2",
                        "VATRate": 23,
                        "Quantity": 4,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 12.96,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;
        var (request, response) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
        response.ftState.State().Should().Be(State.Error, because: Environment.NewLine + string.Join(Environment.NewLine, response.ftSignatures.Select(x => x.Data)));
    }

    #endregion

    #region Scenario 8: Transactions with mismatch between charge + payitems

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    [InlineData(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010)]
    public async Task Scenario8_TransactionWithMismatchForChargeItems(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Amount": 55,
                        "Quantity": 100,
                        "Description": "Article 1",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
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
        var (request, response) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
        response.ftState.State().Should().Be(State.Error, because: Environment.NewLine + string.Join(Environment.NewLine, response.ftSignatures.Select(x => x.Data)));
    }

    #endregion

    #region Scenario 9: Transactions with invalid customer NIF

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario9_TransactionWithInvalidCustomerNIF(ReceiptCase receiptCase)
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Amount": 55,
                        "Quantity": 100,
                        "Description": "Article 1",
                        "VATRate": 23,
                        "ftChargeItemCase": 5788286605450018835
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 55,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456799"
                }
            }
            """;
        var (request, response) = await ProcessReceiptAsync(originalReceipt, (long) receiptCase.WithCountry("PT"));
        response.ftState.State().Should().Be(State.Error, because: Environment.NewLine + string.Join(Environment.NewLine, response.ftSignatures.Select(x => x.Data)));

        var errorMessage = "EEEE_Invalid Portuguese Tax Identification Number (NIF): '123456799'. The NIF must be a 9-digit number with a valid check digit according to the Portuguese tax authority validation algorithm.";
        response.ftSignatures[0].Data.Should().Contain(errorMessage);
    }

    #endregion
}
