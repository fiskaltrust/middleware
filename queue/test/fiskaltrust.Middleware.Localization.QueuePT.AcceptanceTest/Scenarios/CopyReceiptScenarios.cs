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

public class CopyReceiptScenarios : AbstractScenarioTests
{
    #region Scenario 1: Printing a copy without a cbPreviousReceiptReference should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData((ReceiptCase) 0x0006)]
    // [InlineData((ReceiptCase) 0x0007)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario1_Printing_a_copy_without_a_cbPreviousReceiptReference_should_fail(ReceiptCase receiptCase)
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
                }
            }
            """;

        var copyReceiptCase = ReceiptCase.CopyReceiptPrintExistingReceipt0x3010.WithCountry("PT");
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt, (long) copyReceiptCase);
        copyResponse.ftState.State().Should().Be(State.Error);

        copyResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.EEEE_PreviousReceiptReference);
    }

    #endregion

    #region Scenario 2: Printing a copy with a non-existing cbPreviousReceiptReference should fail

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.ECommerce0x0004)]
    // [InlineData((ReceiptCase) 0x0006)]
    // [InlineData((ReceiptCase) 0x0007)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario2_Printing_a_copy_without_a_non_existing_cbPreviousReceiptReference_should_fail(ReceiptCase receiptCase)
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
                "cbPreviousReceiptReference": "NON_EXISTING_REFERENCE"
            }
            """;

        var copyReceiptCase = ReceiptCase.CopyReceiptPrintExistingReceipt0x3010.WithCountry("PT");
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt, (long) copyReceiptCase);
        copyResponse.ftState.State().Should().Be(State.Error);

        copyResponse.ftSignatures[0].Data.Should().Contain($"The given cbPreviousReceiptReference 'NON_EXISTING_REFERENCE' didn't match with any of the items in the Queue or the items referenced are invalid.");
    }

    #endregion

    #region Scenario 3: Printing a copy of a not supported document should fail

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
    [InlineData(ReceiptCase.ECommerce0x0004)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.ZeroReceipt0x2000)]
    [InlineData(ReceiptCase.OneReceipt0x2001)]
    [InlineData(ReceiptCase.ShiftClosing0x2010)]
    [InlineData(ReceiptCase.DailyClosing0x2011)]
    [InlineData(ReceiptCase.MonthlyClosing0x2012)]
    [InlineData(ReceiptCase.YearlyClosing0x2013)]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.ProtocolTechnicalEvent0x3001)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    [InlineData(ReceiptCase.Order0x3004)]
    [InlineData(ReceiptCase.Pay0x3005)]
    // [InlineData(ReceiptCase.InitialOperationReceipt0x4001)]
    // [InlineData(ReceiptCase.OutOfOperationReceipt0x4002)]
    [InlineData(ReceiptCase.InitSCUSwitch0x4011)]
    [InlineData(ReceiptCase.FinishSCUSwitch0x4012)]
    public async Task Scenario3_PrintingCopyOfANotSupportedDocument_ShouldFail(ReceiptCase receiptCase)
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
                "cbPreviousReceiptReference": "{{originalReceipt}}"
            }
            """.Replace("{{originalReceipt}}", originalResponse.cbReceiptReference);

        var copyReceiptCase = ReceiptCase.CopyReceiptPrintExistingReceipt0x3010.WithCountry("PT");
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt, (long) copyReceiptCase);
        copyResponse.ftState.State().Should().Be(State.Error, "Scenario1_TransactionWithoutUser_ShouldFail");

        copyResponse.ftSignatures[0].Data.Should().Contain(ErrorMessagesPT.CopyReceiptNotSupportedForType(originalRequest.ftReceiptCase.Case()));
    }

    #endregion

    #region Scenario 4: Printing a copy of an existing document should be a full copy of the original and include it in the ftStateData

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    // [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    // [InlineData((ReceiptCase) 0x0006)]
    // [InlineData((ReceiptCase) 0x0007)]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Scenario4_PrintingCopyOfAnExistingDocument_ShouldIncludeOriginalInStateData(ReceiptCase receiptCase)
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
                "cbPreviousReceiptReference": "{{originalReceipt}}"
            }
            """.Replace("{{originalReceipt}}", originalResponse.cbReceiptReference);


        var copyReceiptCase = ReceiptCase.CopyReceiptPrintExistingReceipt0x3010.WithCountry("PT");
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt, (long) copyReceiptCase);
        copyResponse.ftState.State().Should().Be(State.Success, because: Environment.NewLine + string.Join(Environment.NewLine, copyResponse.ftSignatures.Select(x => x.Data)));

        // check if ftStateData contains the original receipt response
        var stateDataJson = MiddlewareStateData.FromReceiptResponse(copyResponse);
        stateDataJson.PreviousReceiptReference.Should().HaveCount(1, "ftStateData should contain exactly one PreviousReceiptReference");
        var previousReceipt = stateDataJson.PreviousReceiptReference!.First();
        previousReceipt.Response.cbReceiptReference.Should().Be(originalResponse.cbReceiptReference, "The PreviousReceiptReference should match the original receipt reference");
    }

    #endregion
}