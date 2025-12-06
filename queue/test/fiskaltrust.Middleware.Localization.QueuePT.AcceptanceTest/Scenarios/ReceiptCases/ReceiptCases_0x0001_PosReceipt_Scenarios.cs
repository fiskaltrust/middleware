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

public class ReceiptCases_0x0001_PosReceipt_Scenarios : AbstractScenarioTests
{
    #region Scenario 9: Transactions without payment are not allowed

    [Fact]
    public async Task Scenario9_TransactionWithoutPayment_ShouldFail()
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
                "cbPayItems": [],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var receiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 10: Transactions with negative payment are not allowed

    [Fact]
    public async Task Scenario10_TransactionWithNegativePayment_ShouldFail()
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
                        "Amount": -20,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var receiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 21: Transactions with net amount > 1000 € should fail

    [Fact]
    public async Task Scenario21_TransactionWithNetAmountGreaterThan1000_ShouldFail()
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
                        "Amount": 1300,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": 1300,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                }
            }
            """;

        var posReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (request, response) = await ProcessReceiptAsync(originalReceipt, (long) posReceiptCase);
        response.ftState.State().Should().Be(State.Error);
    }

    #endregion
}