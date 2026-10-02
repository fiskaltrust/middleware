using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using V1 = fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.SCU;

public class ITSSCDContractConverterTests
{
    /// <summary>
    /// A request the way the v2 queue receives it: deserialized from JSON, so the structured members are JSON elements.
    /// </summary>
    private static ReceiptRequest DeserializedRequest() => JsonSerializer.Deserialize<ReceiptRequest>("""
        {
          "ftCashBoxID": "4a0d8f34-c06c-4467-91e5-f25257f8bb77",
          "ftPosSystemId": "2d5f3f7a-7b6b-4b0f-9d3a-3a1c1b5d9a10",
          "cbTerminalID": "1",
          "cbReceiptReference": "0001-0002",
          "cbReceiptMoment": "2026-06-29T10:09:00Z",
          "cbReceiptAmount": 221.0,
          "cbUser": "user1234",
          "cbCustomer": { "CustomerName": "Mario Rossi", "CustomerTaxId": "RSSMRA85T10A562S" },
          "ftReceiptCaseData": { "servizi_lotteriadegliscontrini_gov_it": { "codicelotteria": "DT1MV66K" } },
          "cbPreviousReceiptReference": "0001-0001",
          "cbChargeItems": [
            { "Position": 1, "Quantity": 2.0, "Amount": 221, "UnitPrice": 110.5, "VATRate": 22, "VATAmount": 39.85, "Description": "Item VAT 22%", "ftChargeItemCase": 5283883447186620435, "ftChargeItemCaseData": { "foo": "bar" }, "ProductBarcode": "8001234567890", "Moment": "2026-06-29T10:09:00Z" }
          ],
          "cbPayItems": [
            { "Position": 1, "Quantity": 1, "Amount": 221, "Description": "Cash", "ftPayItemCase": 5283883447184523265, "Moment": "2026-06-29T10:09:00Z" }
          ],
          "ftReceiptCase": 5283883447184523265
        }
        """)!;

    [Fact]
    public void ToV1_Request_MapsTheCasesTheItemsAndTheStructuredMembers()
    {
        var request = DeserializedRequest();

        var v1 = ITSSCDContractConverter.ToV1(request);

        using var scope = new AssertionScope();
        v1.ftCashBoxID.Should().Be("4a0d8f34-c06c-4467-91e5-f25257f8bb77");
        v1.ftPosSystemId.Should().Be("2d5f3f7a-7b6b-4b0f-9d3a-3a1c1b5d9a10");
        v1.cbTerminalID.Should().Be("1");
        v1.cbReceiptReference.Should().Be("0001-0002");
        v1.cbReceiptMoment.Should().Be(request.cbReceiptMoment);
        v1.cbReceiptAmount.Should().Be(221.0m);
        v1.ftReceiptCase.Should().Be(0x4954_2000_0000_0001);
        v1.cbUser.Should().Be("user1234");
        v1.cbPreviousReceiptReference.Should().Be("0001-0001");
        v1.cbCustomer.Should().Be("{ \"CustomerName\": \"Mario Rossi\", \"CustomerTaxId\": \"RSSMRA85T10A562S\" }", "the SCUs parse cbCustomer as JSON");
        v1.ftReceiptCaseData.Should().Be("{ \"servizi_lotteriadegliscontrini_gov_it\": { \"codicelotteria\": \"DT1MV66K\" } }");

        v1.cbChargeItems.Should().HaveCount(1);
        v1.cbChargeItems[0].Position.Should().Be(1);
        v1.cbChargeItems[0].Quantity.Should().Be(2.0m);
        v1.cbChargeItems[0].Amount.Should().Be(221m);
        v1.cbChargeItems[0].UnitPrice.Should().Be(110.5m);
        v1.cbChargeItems[0].VATRate.Should().Be(22m);
        v1.cbChargeItems[0].VATAmount.Should().Be(39.85m);
        v1.cbChargeItems[0].Description.Should().Be("Item VAT 22%");
        v1.cbChargeItems[0].ftChargeItemCase.Should().Be(0x4954_2000_0020_0013);
        v1.cbChargeItems[0].ftChargeItemCaseData.Should().Be("{ \"foo\": \"bar\" }");
        v1.cbChargeItems[0].ProductBarcode.Should().Be("8001234567890");
        v1.cbChargeItems[0].Moment.Should().Be(request.cbChargeItems[0].Moment);

        v1.cbPayItems.Should().HaveCount(1);
        v1.cbPayItems[0].Amount.Should().Be(221m);
        v1.cbPayItems[0].Quantity.Should().Be(1m);
        v1.cbPayItems[0].Description.Should().Be("Cash");
        v1.cbPayItems[0].ftPayItemCase.Should().Be(0x4954_2000_0000_0001);
    }

    [Fact]
    public void ToV1_Request_PassesStringsAndPocosThrough()
    {
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        request.cbCustomer = "{\"CustomerName\":\"already json\"}";
        request.ftReceiptCaseData = new { servizi_lotteriadegliscontrini_gov_it = new { codicelotteria = "DT1MV66K" } };
        request.cbUser = null;

        var v1 = ITSSCDContractConverter.ToV1(request);

        using var scope = new AssertionScope();
        v1.cbCustomer.Should().Be("{\"CustomerName\":\"already json\"}");
        v1.ftReceiptCaseData.Should().Be("{\"servizi_lotteriadegliscontrini_gov_it\":{\"codicelotteria\":\"DT1MV66K\"}}");
        v1.cbUser.Should().BeNull();
        v1.cbPreviousReceiptReference.Should().BeNull();
        v1.cbChargeItems.Should().BeEmpty();
        v1.cbPayItems.Should().BeEmpty();
    }

    [Fact]
    public void ToV1_Request_KeepsASingleElementGroupReference_AndDropsALargerOne()
    {
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);

        request.cbPreviousReceiptReference = new[] { "0001-0001" };
        ITSSCDContractConverter.ToV1(request).cbPreviousReceiptReference.Should().Be("0001-0001");

        request.cbPreviousReceiptReference = new[] { "0001-0001", "0001-0002" };
        ITSSCDContractConverter.ToV1(request).cbPreviousReceiptReference.Should().BeNull();
    }

    [Fact]
    public void ToV1_Response_MapsTheSignatures_AndLeavesTheStateDataBehind()
    {
        var queue = TestHelpers.CreateQueue();
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var response = TestHelpers.CreateResponse(queue, TestHelpers.CreateQueueItem(queue), request);
        response.ftSignatures.AddRange(TestHelpers.CreateRTSignatures(zNumber: 1, documentNumber: 2, new DateTime(2026, 6, 29, 10, 9, 0)));
        response.ftStateData = new MiddlewareStateData { PreviousReceiptReference = [] };
        response.ftReceiptHeader = ["header"];

        var v1 = ITSSCDContractConverter.ToV1(response);

        using var scope = new AssertionScope();
        v1.ftCashBoxID.Should().Be(request.ftCashBoxID.ToString());
        v1.ftQueueID.Should().Be(queue.ftQueueId.ToString());
        v1.ftQueueItemID.Should().Be(response.ftQueueItemID.ToString());
        v1.ftQueueRow.Should().Be(1);
        v1.ftCashBoxIdentification.Should().Be(TestHelpers.CashBoxIdentification);
        v1.ftReceiptIdentification.Should().Be("ft1#");
        v1.ftState.Should().Be(0x4954_2000_0000_0000);
        v1.ftStateData.Should().BeNull();
        v1.ftReceiptHeader.Should().Equal("header");
        v1.ftChargeItems.Should().BeEmpty();
        v1.ftSignatures.Should().HaveCount(5);
        v1.ftSignatures[1].ftSignatureType.Should().Be(0x4954_2000_0000_0011);
        v1.ftSignatures[1].ftSignatureFormat.Should().Be(1);
        v1.ftSignatures[1].Caption.Should().Be("<rt-z-number>");
        v1.ftSignatures[1].Data.Should().Be("0001");
    }

    [Fact]
    public void ToV2_Response_TakesWhatTheScuAnswered_AndRestoresTheStateData()
    {
        var queue = TestHelpers.CreateQueue();
        var request = TestHelpers.CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        var original = TestHelpers.CreateResponse(queue, TestHelpers.CreateQueueItem(queue), request);
        original.ftStateData = new MiddlewareStateData { PreviousReceiptReference = [] };
        var scuResponse = ITSSCDContractConverter.ToV1(original);
        scuResponse.ftReceiptIdentification = "ft1#0001-0002";
        scuResponse.ftSignatures =
        [
            new V1.SignaturItem { Caption = "<rt-z-number>", Data = "0001", ftSignatureFormat = 1, ftSignatureType = 0x4954_2000_0000_0011 },
            new V1.SignaturItem { Caption = "<rt-doc-number>", Data = "0002", ftSignatureFormat = 1, ftSignatureType = 0x4954_2000_0000_0012 },
        ];
        scuResponse.ftChargeItems = [new V1.ChargeItem { Position = 1, Amount = 221, ftChargeItemCase = 0x4954_2000_0020_0013, ftChargeItemCaseData = "{\"foo\":\"bar\"}" }];

        var v2 = ITSSCDContractConverter.ToV2(scuResponse, original);

        using var scope = new AssertionScope();
        v2.ftCashBoxID.Should().Be(original.ftCashBoxID);
        v2.ftQueueID.Should().Be(original.ftQueueID);
        v2.ftQueueItemID.Should().Be(original.ftQueueItemID);
        v2.ftReceiptIdentification.Should().Be("ft1#0001-0002");
        v2.State().Should().Be(TestHelpers.BaseState);
        v2.ftStateData.Should().BeSameAs(original.ftStateData);
        v2.ftSignatures.Should().HaveCount(2);
        ((ulong) v2.ftSignatures[0].ftSignatureType).Should().Be(0x4954_2000_0000_0011);
        v2.ftSignatures[0].ftSignatureFormat.Should().Be(SignatureFormat.Text);
        v2.ftSignatures[0].Data.Should().Be("0001");
        v2.ftChargeItems.Should().HaveCount(1);
        v2.ftChargeItems[0].Position.Should().Be(1);
        ((ulong) v2.ftChargeItems[0].ftChargeItemCase).Should().Be(0x4954_2000_0020_0013);
        v2.ftChargeItems[0].ftChargeItemCaseData.Should().Be("{\"foo\":\"bar\"}");
    }

    [Fact]
    public void ToV2_Response_ExposesTheScusStateDataAsJson()
    {
        var queue = TestHelpers.CreateQueue();
        var request = TestHelpers.CreateRequest(ReceiptCase.ZeroReceipt0x2000);
        var original = TestHelpers.CreateResponse(queue, TestHelpers.CreateQueueItem(queue), request);
        var scuResponse = ITSSCDContractConverter.ToV1(original);
        scuResponse.ftStateData = "{\"CashStatus\":\"ok\"}";

        var v2 = ITSSCDContractConverter.ToV2(scuResponse, original);

        v2.ftStateData.Should().BeOfType<JsonElement>().Which.GetProperty("CashStatus").GetString().Should().Be("ok");
        JsonSerializer.Serialize(v2).Should().Contain("\"ftStateData\":{\"CashStatus\":\"ok\"}");
    }

    [Fact]
    public void ToV2_Response_KeepsNonJsonStateDataAsString_AndFallsBackToTheOriginalIdentifiers()
    {
        var queue = TestHelpers.CreateQueue();
        var request = TestHelpers.CreateRequest(ReceiptCase.ZeroReceipt0x2000);
        var original = TestHelpers.CreateResponse(queue, TestHelpers.CreateQueueItem(queue), request);
        var scuResponse = new V1.ReceiptResponse
        {
            ftStateData = "not json",
            ftState = 0x4954_2000_EEEE_EEEE,
            ftSignatures = [new V1.SignaturItem { Caption = "FAILURE", Data = "boom", ftSignatureFormat = 1, ftSignatureType = 0x4954_2000_0000_3000 }],
        };

        var v2 = ITSSCDContractConverter.ToV2(scuResponse, original);

        using var scope = new AssertionScope();
        v2.ftStateData.Should().Be("not json");
        v2.ftCashBoxID.Should().Be(original.ftCashBoxID);
        v2.ftQueueID.Should().Be(original.ftQueueID);
        v2.ftQueueItemID.Should().Be(original.ftQueueItemID);
        v2.State().Should().Be(0x4954_2000_EEEE_EEEE);
        v2.ftState.IsState(State.Error).Should().BeTrue();
        v2.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE");
        v2.ftReceiptHeader.Should().BeEmpty();
    }
}
