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

public class PosReceiptScenarios
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

    private async Task<(ReceiptRequest request, ReceiptResponse response)> ProcessReceiptAsync(string rawJson)
    {
        var preparedJson = rawJson.Replace("{{$guid}}", Guid.NewGuid().ToString())
            .Replace("{{$isoTimestamp}}", DateTime.UtcNow.ToString("o"))
            .Replace("{{cashboxid}}", _cashBoxId.ToString());

        var request = JsonSerializer.Deserialize<ReceiptRequest>(preparedJson)!;
        var responseJson = await _signProcessor(preparedJson);
        var response = JsonSerializer.Deserialize<ReceiptResponse>(responseJson)!;
        
        return (request, response);
    }

    #region Scenario 1: Transactions without a cbUser should be rejected

    [Fact]
    public async Task Scenario1_TransactionWithoutUser_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 2: Transactions without a cbUser with length of bellow 3 characters should be rejected

    [Fact]
    public async Task Scenario2_TransactionWithoutUserWithShortLength_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 3: Transactions with article description of below 3 characters should be rejected

    [Fact]
    public async Task Scenario3_TransactionWithShortArticleDescription_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 4: Transactions with negative amount on a usual sales should be rejected

    [Fact]
    public async Task Scenario4_TransactionWithNegativeAmount_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 5: Transactions with negative quantity on a usual sales should be rejected

    [Fact]
    public async Task Scenario5_TransactionWithNegativeQuantity_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 6: Transactions with illegal VAT Rate should be rejected

    [Fact]
    public async Task Scenario6_TransactionWithIllegalVATRate_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 7: Transactions with 0 VATRate but no exempt reasons specified should be rejected

    [Fact]
    public async Task Scenario7_TransactionWithZeroVATRateAndNoExemptReasons_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
                "cbChargeItems": [
                    {
                        "Quantity": 1,
                        "Amount": 20,
                        "Description": "Test",
                        "VATRate": 0,
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

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

    #region Scenario 8: Transactions with invalid NIF should be rejected

    [Fact]
    public async Task Scenario8_TransactionWithInvalidNIF_ShouldFail()
    {
        // Arrange
        var json = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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
                    "CustomerVATId": "123456779"
                }
            }
            """;

        var (request, response) = await ProcessReceiptAsync(json);

        response.ftState.Should().Be((State) 0x5054_2000_EEEE_EEEE, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion

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
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

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
                "ftReceiptCase": 1,
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

        var (request, response) = await ProcessReceiptAsync(json);

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
                "ftReceiptCase": 1,
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

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt);

        // Arrange
        var copyReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 5788286605450018816,
                "cbChargeItems": [],
                "cbPayItems": [],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "FIXED_VALUE"
            }
            """;
        /* 0x5054200000003010 */
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt);
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
                "ftReceiptCase": 5788286605450018816,
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

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt);

        // Arrange
        var copyReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 5788286605450018816,
                "cbChargeItems": [],
                "cbPayItems": [],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{originalReceipt}}"
            }
            """.Replace("{{originalReceipt}}", originalResponse.cbReceiptReference);
        /* 0x5054200000003010 */
        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt);
        copyResponse.ftState.State().Should().Be(State.Error, "Scenario1_TransactionWithoutUser_ShouldFail");
    }

    #endregion


    #region Scenario 12: Printing a copy of an existing document should be a full copy of the original and include it in the ftStateData

    [Fact]
    public async Task Scenario12_PrintingCopyOfAnExistingDocument_ShouldIncludeOriginalInStateData()
    {
        var originalReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 1,
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

        var (originalRequest, originalResponse) = await ProcessReceiptAsync(originalReceipt);

        // Arrange
        var copyReceipt = """
            {
                "cbReceiptReference": "{{$guid}}",
                "cbReceiptMoment": "{{$isoTimestamp}}",
                "ftCashBoxID": "{{cashboxid}}",
                "ftReceiptCase": 5788286605450018816,
                "cbChargeItems": [],
                "cbPayItems": [],
                "cbUser": "Stefan Kert",
                "cbCustomer": {
                    "CustomerVATId": "123456789"
                },
                "cbPreviousReceiptReference": "{{originalReceipt}}"
            }
            """.Replace("{{originalReceipt}}", originalResponse.cbReceiptReference);

        var (copyRequest, copyResponse) = await ProcessReceiptAsync(copyReceipt);
        copyResponse.ftState.Should().Be((State) 0x5054_2000_0000_0000, "Scenario1_TransactionWithoutUser_ShouldFail");

        // check if ftStateData contains the original receipt response
        var stateDataJson = MiddlewareStateData.FromReceiptResponse(copyResponse);
        stateDataJson.PreviousReceiptReference.Should().HaveCount(1, "ftStateData should contain exactly one PreviousReceiptReference");
        var previousReceipt = stateDataJson.PreviousReceiptReference!.First();
        previousReceipt.Response.cbReceiptReference.Should().Be(originalResponse.cbReceiptReference, "The PreviousReceiptReference should match the original receipt reference");
    }

    #endregion
}