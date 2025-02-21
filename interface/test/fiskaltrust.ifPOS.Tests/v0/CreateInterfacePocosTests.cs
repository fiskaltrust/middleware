using System;
using System.IO;
using System.Runtime.Serialization;
using FluentAssertions;
using NUnit.Framework;

namespace fiskaltrust.ifPOS.Tests.v0
{
    public class CreateInterfacePocosTests
    {
        [Test]
        public void Serialize_Deserialize_ChargeItem_ShouldReturn_InitialClass()
        {
            var chargeItem = new ifPOS.v0.ChargeItem();
            chargeItem.Position = 0;

            var stream1 = new MemoryStream();
            var serializer = new DataContractSerializer(typeof(ifPOS.v0.ChargeItem));
            serializer.WriteObject(stream1, chargeItem);
            stream1.Position = 0;
            var xmlChargeItem = (ifPOS.v0.ChargeItem)serializer.ReadObject(stream1);

            xmlChargeItem.Position.Should().Be(chargeItem.Position);
            xmlChargeItem.Quantity.Should().Be(chargeItem.Quantity);
            xmlChargeItem.Description.Should().Be(chargeItem.Description);
            xmlChargeItem.Amount.Should().Be(chargeItem.Amount);
            xmlChargeItem.VATRate.Should().Be(chargeItem.VATRate);
            xmlChargeItem.ftChargeItemCase.Should().Be(chargeItem.ftChargeItemCase);
            xmlChargeItem.ftChargeItemCaseData.Should().Be(chargeItem.ftChargeItemCaseData);
            xmlChargeItem.VATAmount.Should().Be(chargeItem.VATAmount);
            xmlChargeItem.AccountNumber.Should().Be(chargeItem.AccountNumber);
            xmlChargeItem.CostCenter.Should().Be(chargeItem.CostCenter);
            xmlChargeItem.ProductGroup.Should().Be(chargeItem.ProductGroup);
            xmlChargeItem.ProductNumber.Should().Be(chargeItem.ProductNumber);
            xmlChargeItem.ProductBarcode.Should().Be(chargeItem.ProductBarcode);
            xmlChargeItem.Unit.Should().Be(chargeItem.Unit);
            xmlChargeItem.UnitQuantity.Should().Be(chargeItem.UnitQuantity);
            xmlChargeItem.UnitPrice.Should().Be(chargeItem.UnitPrice);
            xmlChargeItem.Moment.Should().Be(chargeItem.Moment);
        }

        [Test]
        public void Serialize_Deserialize_PayItem_ShouldReturn_InitialClass()
        {
            var chargeItem = new ifPOS.v0.PayItem();
            chargeItem.Position = 0;

            var stream1 = new MemoryStream();
            var serializer = new DataContractSerializer(typeof(ifPOS.v0.PayItem));
            serializer.WriteObject(stream1, chargeItem);
            stream1.Position = 0;
            var xmlChargeItem = (ifPOS.v0.PayItem)serializer.ReadObject(stream1);
            xmlChargeItem.Position.Should().Be(chargeItem.Position);
            xmlChargeItem.Quantity.Should().Be(chargeItem.Quantity);
            xmlChargeItem.Description.Should().Be(chargeItem.Description);
            xmlChargeItem.Amount.Should().Be(chargeItem.Amount);
            xmlChargeItem.ftPayItemCase.Should().Be(chargeItem.ftPayItemCase);
            xmlChargeItem.ftPayItemCaseData.Should().Be(chargeItem.ftPayItemCaseData);
            xmlChargeItem.AccountNumber.Should().Be(chargeItem.AccountNumber);
            xmlChargeItem.CostCenter.Should().Be(chargeItem.CostCenter);
            xmlChargeItem.MoneyGroup.Should().Be(chargeItem.MoneyGroup);
            xmlChargeItem.MoneyNumber.Should().Be(chargeItem.MoneyNumber);
            xmlChargeItem.Moment.Should().Be(chargeItem.Moment);
        }

        [Test]
        public void Serialize_Deserialize_SignaturItem_ShouldReturn_InitialClass()
        {
            var chargeItem = new ifPOS.v0.SignaturItem();
            var stream1 = new MemoryStream();
            var serializer = new DataContractSerializer(typeof(ifPOS.v0.SignaturItem));
            serializer.WriteObject(stream1, chargeItem);
            stream1.Position = 0;
            var xmlChargeItem = (ifPOS.v0.SignaturItem)serializer.ReadObject(stream1);
            xmlChargeItem.ftSignatureFormat.Should().Be(chargeItem.ftSignatureFormat);
            xmlChargeItem.ftSignatureType.Should().Be(chargeItem.ftSignatureType);
            xmlChargeItem.Caption.Should().Be(chargeItem.Caption);
            xmlChargeItem.Data.Should().Be(chargeItem.Data);
        }

        [Test]
        public void Serialize_Deserialize_ReceiptRequest_ShouldReturn_InitialClass()
        {
            var chargeItem = new ifPOS.v0.ReceiptRequest();
            chargeItem.ftCashBoxID = Guid.NewGuid().ToString();
            chargeItem.ftQueueID = Guid.NewGuid().ToString();
            chargeItem.ftPosSystemId = Guid.NewGuid().ToString();
            chargeItem.ftReceiptCaseData = Guid.NewGuid().ToString();
            chargeItem.cbReceiptAmount = (decimal)1.0;
            chargeItem.cbUser = Guid.NewGuid().ToString();
            chargeItem.cbArea = Guid.NewGuid().ToString();
            chargeItem.cbCustomer = Guid.NewGuid().ToString();
            chargeItem.cbSettlement = Guid.NewGuid().ToString();
            chargeItem.cbPreviousReceiptReference = Guid.NewGuid().ToString();

            var stream1 = new MemoryStream();
            var serializer = new DataContractSerializer(typeof(ifPOS.v0.ReceiptRequest));
            serializer.WriteObject(stream1, chargeItem);
            stream1.Position = 0;
            var xmlChargeItem = (ifPOS.v0.ReceiptRequest)serializer.ReadObject(stream1);
            xmlChargeItem.ftCashBoxID.Should().Be(chargeItem.ftCashBoxID);
            xmlChargeItem.ftQueueID.Should().Be(chargeItem.ftQueueID);
            xmlChargeItem.ftPosSystemId.Should().Be(chargeItem.ftPosSystemId);
            xmlChargeItem.cbTerminalID.Should().Be(chargeItem.cbTerminalID);

            xmlChargeItem.cbReceiptReference.Should().Be(chargeItem.cbReceiptReference);
            xmlChargeItem.cbReceiptMoment.Should().Be(chargeItem.cbReceiptMoment);
            xmlChargeItem.cbChargeItems.Should().BeEquivalentTo(chargeItem.cbChargeItems);
            xmlChargeItem.cbPayItems.Should().BeEquivalentTo(chargeItem.cbPayItems);
            xmlChargeItem.ftReceiptCase.Should().Be(chargeItem.ftReceiptCase);
            xmlChargeItem.ftReceiptCaseData.Should().Be(chargeItem.ftReceiptCaseData);
            xmlChargeItem.cbReceiptAmount.Should().Be(chargeItem.cbReceiptAmount);
            xmlChargeItem.cbUser.Should().Be(chargeItem.cbUser);
            xmlChargeItem.cbArea.Should().Be(chargeItem.cbArea);
            xmlChargeItem.cbCustomer.Should().Be(chargeItem.cbCustomer);
            xmlChargeItem.cbSettlement.Should().Be(chargeItem.cbSettlement);
            xmlChargeItem.cbPreviousReceiptReference.Should().Be(chargeItem.cbPreviousReceiptReference);
        }


        [Test]
        public void Serialize_Deserialize_ReceiptResponse_ShouldReturn_InitialClass()
        {
            var chargeItem = new ifPOS.v0.ReceiptResponse();
            chargeItem.ftStateData = Guid.NewGuid().ToString();
            chargeItem.ftReceiptHeader = new string[]
            {
                 Guid.NewGuid().ToString(),
                  Guid.NewGuid().ToString()
            };
            chargeItem.ftChargeItems = new ifPOS.v0.ChargeItem[]
            {
            };
            chargeItem.ftPayItems = new ifPOS.v0.PayItem[]
            {
            };
            chargeItem.ftPayLines = new string[]
            {
                 Guid.NewGuid().ToString(),
                 Guid.NewGuid().ToString()
            };
            chargeItem.ftSignatures = new ifPOS.v0.SignaturItem[]
            {
            };
            chargeItem.ftReceiptFooter = new string[]
            {
                 Guid.NewGuid().ToString(),
                 Guid.NewGuid().ToString()
            };

            var stream1 = new MemoryStream();
            var serializer = new DataContractSerializer(typeof(ifPOS.v0.ReceiptResponse));
            serializer.WriteObject(stream1, chargeItem);
            stream1.Position = 0;
            var xmlChargeItem = (ifPOS.v0.ReceiptResponse)serializer.ReadObject(stream1);

            xmlChargeItem.ftCashBoxID.Should().Be(chargeItem.ftCashBoxID);
            xmlChargeItem.ftQueueID.Should().Be(chargeItem.ftQueueID);
            xmlChargeItem.ftQueueItemID.Should().Be(chargeItem.ftQueueItemID);
            xmlChargeItem.ftQueueRow.Should().Be(chargeItem.ftQueueRow);
            xmlChargeItem.cbTerminalID.Should().Be(chargeItem.cbTerminalID);
            xmlChargeItem.cbReceiptReference.Should().Be(chargeItem.cbReceiptReference);
            xmlChargeItem.ftCashBoxIdentification.Should().Be(chargeItem.ftCashBoxIdentification);
            xmlChargeItem.ftReceiptIdentification.Should().Be(chargeItem.ftReceiptIdentification);
            xmlChargeItem.ftReceiptMoment.Should().Be(chargeItem.ftReceiptMoment);
            xmlChargeItem.ftReceiptHeader.Should().BeEquivalentTo(chargeItem.ftReceiptHeader);
            xmlChargeItem.ftChargeItems.Should().BeEquivalentTo(chargeItem.ftChargeItems);
            xmlChargeItem.ftPayItems.Should().BeEquivalentTo(chargeItem.ftPayItems);
            xmlChargeItem.ftPayLines.Should().BeEquivalentTo(chargeItem.ftPayLines);
            xmlChargeItem.ftSignatures.Should().BeEquivalentTo(chargeItem.ftSignatures);
            xmlChargeItem.ftReceiptFooter.Should().BeEquivalentTo(chargeItem.ftReceiptFooter);
            xmlChargeItem.ftState.Should().Be(chargeItem.ftState);
            xmlChargeItem.ftStateData.Should().Be(chargeItem.ftStateData);
        }
    }
}
