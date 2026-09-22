using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Queue.PostFiscalization;
using FluentAssertions;
using Xunit;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest.PostFiscalization
{
    public class PostFiscalizationMapperTests
    {
        private const long ItReceiptCase = 0x4954_0000_0000_0001L;

        [Fact]
        public void ToV2_Request_MapsIdsAndCases_AndEmbedsJsonStrings()
        {
            var cashBoxId = Guid.NewGuid();
            var request = new ReceiptRequest
            {
                ftCashBoxID = cashBoxId.ToString(),
                ftQueueID = string.Empty,
                ftPosSystemId = "not-a-guid",
                cbTerminalID = "T1",
                cbReceiptReference = "R-1",
                cbReceiptMoment = new DateTime(2026, 9, 22, 10, 15, 30, 123, DateTimeKind.Utc).AddTicks(4567),
                ftReceiptCase = ItReceiptCase,
                ftReceiptCaseData = "[1, 2]",
                cbCustomer = "{\"CustomerName\":\"Max\"}",
                cbUser = "Max",
                cbPreviousReceiptReference = string.Empty,
                cbReceiptAmount = 12.5m,
                cbChargeItems = new[] { new ChargeItem { Amount = 12.5m, Quantity = 1, Description = "Item", ftChargeItemCase = 0x4954_0000_0000_0001L, ftChargeItemCaseData = "{\"a\":1}", VATRate = 22 } },
                cbPayItems = new[] { new PayItem { Amount = 12.5m, Quantity = 1, Description = "Cash", ftPayItemCase = 0x4954_0000_0000_0001L } },
            };

            var v2 = PostFiscalizationMapper.ToV2(request);

            v2.ftCashBoxID.Should().Be(cashBoxId);
            v2.ftQueueID.Should().BeNull();
            v2.ftPosSystemId.Should().BeNull();
            v2.cbTerminalID.Should().Be("T1");
            v2.cbReceiptReference.Should().Be("R-1");
            v2.cbReceiptMoment.Should().Be(request.cbReceiptMoment);
            v2.ftReceiptCase.Should().Be((ReceiptCase) ItReceiptCase);
            v2.cbReceiptAmount.Should().Be(12.5m);
            v2.cbPreviousReceiptReference.Should().BeNull();
            v2.ftReceiptCaseData.Should().BeOfType<JsonElement>().Which.ValueKind.Should().Be(JsonValueKind.Array);
            v2.cbCustomer.Should().BeOfType<JsonElement>().Which.GetProperty("CustomerName").GetString().Should().Be("Max");
            v2.cbUser.Should().BeOfType<JsonElement>().Which.GetString().Should().Be("Max");
            var chargeItem = v2.cbChargeItems.Should().ContainSingle().Subject;
            chargeItem.Amount.Should().Be(12.5m);
            chargeItem.VATRate.Should().Be(22);
            chargeItem.ftChargeItemCase.Should().Be((ChargeItemCase) 0x4954_0000_0000_0001UL);
            chargeItem.ftChargeItemCaseData.Should().BeOfType<JsonElement>().Which.GetProperty("a").GetInt32().Should().Be(1);
            v2.cbPayItems.Should().ContainSingle().Which.ftPayItemCase.Should().Be((PayItemCase) 0x4954_0000_0000_0001UL);
        }

        [Fact]
        public void ToV2_Request_KeepsStringsThatAreNotJson()
        {
            var request = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid().ToString(),
                ftReceiptCase = ItReceiptCase,
                cbCustomer = "{not json",
                cbPreviousReceiptReference = "prev",
                cbChargeItems = new ChargeItem[0],
                cbPayItems = new PayItem[0],
            };

            var v2 = PostFiscalizationMapper.ToV2(request);

            v2.cbCustomer.Should().BeOfType<JsonElement>().Which.GetString().Should().Be("{not json");
            v2.cbPreviousReceiptReference.Should().NotBeNull();
            v2.cbPreviousReceiptReference.SingleValue.Should().Be("prev");
        }

        [Fact]
        public void ToV2_Response_EmbedsStateData_NormalizesIds_AndMapsSignatures()
        {
            var queueId = Guid.NewGuid();
            var queueItemId = Guid.NewGuid();
            var response = new ReceiptResponse
            {
                ftCashBoxID = string.Empty,
                ftQueueID = queueId.ToString(),
                ftQueueItemID = queueItemId.ToString(),
                ftQueueRow = 7,
                cbReceiptReference = "R-1",
                ftReceiptIdentification = "ft1#",
                ftState = 0x4954_2000_0000_0000L,
                ftStateData = "{\"IT\":{\"Document\":1}}",
                ftSignatures = new[] { new SignaturItem { Caption = "rt-document", Data = "0001", ftSignatureFormat = 1, ftSignatureType = 0x4954_2000_0000_0010L } },
            };

            var v2 = PostFiscalizationMapper.ToV2(response);

            v2.ftCashBoxID.Should().BeNull();
            v2.ftQueueID.Should().Be(queueId);
            v2.ftQueueItemID.Should().Be(queueItemId);
            v2.ftQueueRow.Should().Be(7);
            v2.ftReceiptIdentification.Should().Be("ft1#");
            v2.ftState.Should().Be((State) 0x4954_2000_0000_0000UL);
            v2.ftState.IsState(State.Error).Should().BeFalse();
            v2.ftStateData.Should().BeOfType<JsonElement>().Which.GetProperty("IT").GetProperty("Document").GetInt32().Should().Be(1);
            var signature = v2.ftSignatures.Should().ContainSingle().Subject;
            signature.Caption.Should().Be("rt-document");
            signature.ftSignatureFormat.Should().Be(SignatureFormat.Text);
            signature.ftSignatureType.Should().Be((SignatureType) 0x4954_2000_0000_0010UL);
        }

        [Fact]
        public void ToV2_Response_WithoutSignaturesOrStateData_MapsToEmptyListAndNull()
        {
            var response = new ReceiptResponse { ftQueueID = null, ftQueueItemID = "garbage", ftState = 0, ftSignatures = null, ftStateData = null };

            var v2 = PostFiscalizationMapper.ToV2(response);

            v2.ftQueueID.Should().Be(Guid.Empty);
            v2.ftQueueItemID.Should().Be(Guid.Empty);
            v2.ftSignatures.Should().NotBeNull().And.BeEmpty();
            v2.ftStateData.Should().BeNull();
        }

        [Fact]
        public void MergeIntoV1_TakesSignaturesStateAndStateData_AndKeepsEverythingElse()
        {
            var target = new ReceiptResponse
            {
                ftReceiptIdentification = "ft1#",
                ftReceiptHeader = new[] { "header" },
                ftState = 0x4954_2000_0000_0000L,
                ftStateData = "{\"IT\":{\"Document\":1}}",
                ftSignatures = new[] { new SignaturItem { Caption = "rt-document", Data = "0001", ftSignatureFormat = 1, ftSignatureType = 0x4954_2000_0000_0010L } },
            };
            var source = new V2.ReceiptResponse
            {
                ftReceiptIdentification = "changed by nobody",
                ftState = ((State) 0x4954_2000_0000_0000UL).WithState(State.Error),
                ftStateData = new MiddlewareStateData { PostFiscalization = new PostFiscalizationStateData { FiscalizationSucceeded = true, EInvoicing = PostFiscalizationStateData.Failed } },
                ftSignatures = new List<V2.SignatureItem>
                {
                    new V2.SignatureItem { Caption = "rt-document", Data = "0001", ftSignatureFormat = SignatureFormat.Text, ftSignatureType = (SignatureType) 0x4954_2000_0000_0010UL },
                    new V2.SignatureItem { Caption = "einvoicing-failed", Data = "timeout", ftSignatureFormat = SignatureFormat.Text, ftSignatureType = (SignatureType) 0x4954_2000_0000_3000UL },
                },
            };

            PostFiscalizationMapper.MergeIntoV1(target, source);

            target.ftState.Should().Be(unchecked((long) 0x4954_2000_EEEE_EEEEUL));
            target.ftSignatures.Select(signature => signature.Caption).Should().Equal("rt-document", "einvoicing-failed");
            target.ftSignatures[1].ftSignatureType.Should().Be(unchecked((long) 0x4954_2000_0000_3000UL));
            target.ftSignatures[1].ftSignatureFormat.Should().Be(1);
            target.ftStateData.Should().Contain("\"PostFiscalization\"").And.Contain("\"FiscalizationSucceeded\":true").And.Contain("\"EInvoicing\":\"failed\"");
            target.ftReceiptIdentification.Should().Be("ft1#", "only signatures, state data and state are merged back");
            target.ftReceiptHeader.Should().Equal("header");
        }

        [Fact]
        public void StateDataToString_CoversEveryShape()
        {
            PostFiscalizationMapper.StateDataToString(null).Should().BeNull();
            PostFiscalizationMapper.StateDataToString("plain").Should().Be("plain");
            PostFiscalizationMapper.StateDataToString(JsonSerializer.Deserialize<JsonElement>("\"text\"")).Should().Be("text");
            PostFiscalizationMapper.StateDataToString(JsonSerializer.Deserialize<JsonElement>("null")).Should().BeNull();
            PostFiscalizationMapper.StateDataToString(JsonSerializer.Deserialize<JsonElement>("{\"a\":1}")).Should().Be("{\"a\":1}");
            PostFiscalizationMapper.StateDataToString(new MiddlewareStateData { PostFiscalization = new PostFiscalizationStateData { FiscalizationSucceeded = true } })
                .Should().Contain("\"PostFiscalization\":{\"FiscalizationSucceeded\":true");
        }

        [Theory]
        [InlineData(0x4445_0000_0000_000CUL, true)]  // DE B2B-invoice
        [InlineData(0x4445_0000_0000_000DUL, true)]  // DE B2C-invoice
        [InlineData(0x4445_0000_0002_000CUL, true)]  // DE B2B-invoice with a flag set
        [InlineData(0x4445_0000_0000_0001UL, false)] // DE Pos-receipt
        [InlineData(0x4445_0000_0000_000EUL, false)] // DE Info-invoice: not in the allow list
        [InlineData(0x4954_0000_0000_1001UL, true)]  // IT B2C invoice via the type nibble
        [InlineData(0x4954_0000_0000_0001UL, false)] // IT Pos-receipt
        [InlineData(0x4154_0000_0000_000CUL, false)] // AT: no allow list yet
        [InlineData(0x4652_0000_0000_0003UL, false)] // FR Invoice: no allow list yet
        public void LegacyInvoiceReceiptCases_CombineTheTypeNibbleWithThePerMarketAllowList(ulong receiptCase, bool expected)
        {
            LegacyInvoiceReceiptCases.IsInvoiceDocument(new V2.ReceiptRequest { ftReceiptCase = (ReceiptCase) receiptCase }).Should().Be(expected);
        }

        [Fact]
        public void LegacyConventions_MatchTheUncaughtExceptionSignatureAndState()
        {
            PostFiscalizationMapper.LegacyFailureSignatureType(ItReceiptCase).Should().Be(unchecked((long) 0x4954_2000_0000_3000UL));
            PostFiscalizationMapper.LegacyErrorState(ItReceiptCase).Should().Be(unchecked((long) 0x4954_2000_EEEE_EEEEUL));
            PostFiscalizationMapper.LegacyFailState(ItReceiptCase).Should().Be(unchecked((long) 0x4954_2000_FFFF_FFFFUL));
            PostFiscalizationMapper.LegacyFailureSignatureType(new V2.ReceiptRequest { ftReceiptCase = (ReceiptCase) 0x4954_0000_0000_0001UL })
                .Should().Be((SignatureType) 0x4954_2000_0000_3000UL);
        }
    }
}
