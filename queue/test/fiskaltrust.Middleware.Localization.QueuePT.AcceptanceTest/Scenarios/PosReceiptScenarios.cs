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

public class PosReceiptScenarios : AbstractScenarioTests
{
    private readonly Func<string, Task<string>> _signProcessor;
    private readonly Guid _queueId;
    private readonly Guid _cashBoxId;

    public PosReceiptScenarios()
    {
        _queueId = Guid.NewGuid();
        _cashBoxId = Guid.NewGuid();

        var mockSscd = new MockPTSSCD();

        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox
                {
                    ftCashBoxId = _cashBoxId,
                    TimeStamp = DateTime.UtcNow.Ticks
                }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue>
            {
                new ftQueue
                {
                    ftQueueId = _queueId,
                    ftCashBoxId = _cashBoxId,
                    StartMoment = DateTime.UtcNow
                }
            }) },
            { "init_ftQueuePT", JsonSerializer.Serialize(new List<ftQueuePT>
            {
                new ftQueuePT
                {
                    ftQueuePTId = _queueId,
                    IssuerTIN = "123456789"
                }
            }) },
            { "init_ftSignaturCreationUnitPT", JsonSerializer.Serialize(new List<ftSignaturCreationUnitPT>()) }
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(_queueId, configuration, loggerFactory);
        var bootstrapper = new QueuePTBootstrapper(_queueId, loggerFactory, configuration, mockSscd, storageProvider);
        _signProcessor = bootstrapper.RegisterForSign();
    }

    private async Task<(ReceiptRequest request, ReceiptResponse response)> ProcessReceiptAsync(string rawJson, long? ftReceiptCase = null)
    {
        var preparedJson = rawJson.Replace("{{$guid}}", Guid.NewGuid().ToString())
            .Replace("{{$isoTimestamp}}", DateTime.UtcNow.ToString("o"))
            .Replace("{{cashboxid}}", _cashBoxId.ToString());

        if (ftReceiptCase.HasValue)
        {
            preparedJson = preparedJson.Replace("{{ftReceiptCase}}", ftReceiptCase.Value.ToString());
        }

        var request = JsonSerializer.Deserialize<ReceiptRequest>(preparedJson)!;
        var responseJson = await _signProcessor(preparedJson);
        var response = JsonSerializer.Deserialize<ReceiptResponse>(responseJson)!;

        return (request, response);
    }


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

    #region Scenario 11: Printing a copy of a non existing document should fail

    [Fact]
    public async Task Scenario11_PrintingCopyOfANonExistingDocument_ShouldFail()
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

        var posReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) posReceiptCase);

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
                "cbPreviousReceiptReference": "FIXED_VALUE"
            }
            """;

        var copyReceiptCase = ReceiptCase.CopyReceiptPrintExistingReceipt0x3010.WithCountry("PT");
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt, (long) copyReceiptCase);
        copyResponse.ftState.State().Should().Be(State.Error, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 12: Printing a copy of a not supported document should fail

    [Fact]
    public async Task Scenario12_PrintingCopyOfANotSupportedDocument_ShouldFail()
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

        var orderReceiptCase = ReceiptCase.Order0x3004.WithCountry("PT");
        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) orderReceiptCase);

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
    }

    #endregion

    #region Scenario 13: Printing a copy of an existing document should be a full copy of the original and include it in the ftStateData

    [Fact]
    public async Task Scenario13_PrintingCopyOfAnExistingDocument_ShouldIncludeOriginalInStateData()
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

        var posReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) posReceiptCase);

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
        copyResponse.ftState.Should().Be((State) 0x5054_2000_0000_0000, "Scenario1_TransactionWithoutUser_ShouldFail");

        // check if ftStateData contains the original receipt response
        var stateDataJson = MiddlewareStateData.FromReceiptResponse(copyResponse);
        stateDataJson.PreviousReceiptReference.Should().HaveCount(1, "ftStateData should contain exactly one PreviousReceiptReference");
        var previousReceipt = stateDataJson.PreviousReceiptReference!.First();
        previousReceipt.Response.cbReceiptReference.Should().Be(originalResponse.cbReceiptReference, "The PreviousReceiptReference should match the original receipt reference");
    }

    #endregion


    #region Scenario 16: Transactions with void with missing reference should be rejected

    [Fact]
    public async Task Scenario16_TransactionWithVoid_ForDocument_ShouldWork()
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

        var posReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) posReceiptCase);

        // Arrange
        var voidReceipt = """       
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


        var voidReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void);
        var (voidRequest, voidResponse) = await ProcessReceiptAsync(voidReceipt, (long) voidReceiptCase);
        voidResponse.ftState.Should().Be((State) 0x5054_2000_0000_0000, "Scenario1_TransactionWithoutUser_ShouldFail");

        // check if ftStateData contains the original receipt response
        var stateDataJson = MiddlewareStateData.FromReceiptResponse(voidResponse);
        stateDataJson.PreviousReceiptReference.Should().HaveCount(1, "ftStateData should contain exactly one PreviousReceiptReference");
        var previousReceipt = stateDataJson.PreviousReceiptReference!.First();
        previousReceipt.Response.cbReceiptReference.Should().Be(originalResponse.cbReceiptReference, "The PreviousReceiptReference should match the original receipt reference");
    }

    #endregion

    #region Scenario 17: Transactions with refund with no reference should be rejected

    [Fact]
    public async Task Scenario17_TransactionWithRefundWithNoReference_ShouldFail()
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

        var receiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund);

        var (request, response) = await ProcessReceiptAsync(json, (long) receiptCase);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 18: Transactions with refund with missing reference should be rejected

    [Fact]
    public async Task Scenario18_TransactionWithRefundWithMissingReference_ShouldFail()
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

        var posReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) posReceiptCase);

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


        var copyReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund);
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt, (long) copyReceiptCase);
        copyResponse.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 19: Transactions with full refund with missmatch between original and refund receipt should be rejected

    [Fact]
    public async Task Scenario19_TransactionWithRefund_ForDocument_ShouldWork()
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

        var posReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) posReceiptCase);

        // Arrange
        var voidReceipt = """       
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": {{ftReceiptCase}},
                "cbChargeItems": [
                    {
                        "Quantity": -1,
                        "Amount": -10,
                        "Description": "Test",
                        "VATRate": 23,
                        "ftChargeItemCase": 3
                    }
                ],
                "cbPayItems": [
                    {
                        "Amount": -10,
                        "Description": "Cash"
                    }
                ],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{originalReceipt}}"
            }
            """.Replace("{{originalReceipt}}", originalResponse.cbReceiptReference);


        var refundReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund);
        var (refundRequest, refundResponse) = await ProcessReceiptAsync(voidReceipt, (long) refundReceiptCase);

        refundResponse.ftState.State().Should().Be(State.Error);

        // check if ftStateData contains the original receipt response
        var stateDataJson = MiddlewareStateData.FromReceiptResponse(refundResponse);
        stateDataJson.PreviousReceiptReference.Should().HaveCount(1, "ftStateData should contain exactly one PreviousReceiptReference");
        var previousReceipt = stateDataJson.PreviousReceiptReference!.First();
        previousReceipt.Response.cbReceiptReference.Should().Be(originalResponse.cbReceiptReference, "The PreviousReceiptReference should match the original receipt reference");
    }

    #endregion

    #region Scenario 20: Transactions with full refund with match between original and refund receipt should be good

    [Fact]
    public async Task Scenario20_TransactionWithFullRefund_ShouldWork()
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

        var posReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt, (long) posReceiptCase);

        // Arrange
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
                },
                "cbPreviousReceiptReference": "{{originalReceipt}}"
            }
            """.Replace("{{originalReceipt}}", originalResponse.cbReceiptReference);


        var refundReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund);
        var (refundRequest, refundResponse) = await ProcessReceiptAsync(voidReceipt, (long) refundReceiptCase);

        refundResponse.ftState.State().Should().Be(State.Success);

        // check if ftStateData contains the original receipt response
        var stateDataJson = MiddlewareStateData.FromReceiptResponse(refundResponse);
        stateDataJson.PreviousReceiptReference.Should().HaveCount(1, "ftStateData should contain exactly one PreviousReceiptReference");
        var previousReceipt = stateDataJson.PreviousReceiptReference!.First();
        previousReceipt.Response.cbReceiptReference.Should().Be(originalResponse.cbReceiptReference, "The PreviousReceiptReference should match the original receipt reference");
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