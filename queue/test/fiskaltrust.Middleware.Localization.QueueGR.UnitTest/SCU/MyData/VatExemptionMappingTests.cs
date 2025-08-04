using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.SCU.GR.MyData;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
{
    public class VatExemptionMappingTests
    {
        private static ChargeItem CreateChargeItem(int position, decimal amount, int vatRate, string description, ChargeItemCaseTypeOfService chargeItemCaseTypeOfService, ChargeItemCase chargeItemCase)
        {
            return new ChargeItem
            {
                Position = position,
                Amount = amount,
                VATRate = vatRate,
                VATAmount = decimal.Round(amount / (100M + vatRate) * vatRate, 2, MidpointRounding.ToEven),
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithTypeOfService(chargeItemCaseTypeOfService).WithVat(chargeItemCase),
                Quantity = 1,
                Description = description
            };
        }

        [Fact]
        public void GetVatExemptionCategory_UsualVatApplies_ReturnsNull()
        {
            // Arrange
            var chargeItem = CreateChargeItem(1, 100, 24, "Test item", ChargeItemCaseTypeOfService.Delivery, ChargeItemCase.NormalVatRate);
            chargeItem.ftChargeItemCase = chargeItem.ftChargeItemCase.WithNatureOfVat(ChargeItemCaseNatureOfVatGR.UsualVatApplies);

            // Act
            var result = AADEMappings.GetVatExemptionCategory(chargeItem);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetVatExemptionCategory_ForUnsupportedNature_ShouldThrowException()
        {
            // Arrange
            var chargeItem = CreateChargeItem(1, 100, 24, "Test item", ChargeItemCaseTypeOfService.Delivery, ChargeItemCase.NormalVatRate);
            chargeItem.ftChargeItemCase = (ChargeItemCase) (((ulong) chargeItem.ftChargeItemCase & 0xFFFF_FFFF_FFFF_00FF) | (ulong) 0x9900);

            // Act
            var action = () => AADEMappings.GetVatExemptionCategory(chargeItem);

            action.Should().ThrowExactly<NotSupportedException>().WithMessage("The ChargeItemCase 0x4752200000009913 contains a not supported Nature NN.");
        }


        [Theory]
        [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableIntraCommunitySupplies, 14)]
        [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableExportOutsideEU, 8)]
        [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableTaxFreeRetail, 28)]
        [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a, 16)]
        [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableArticle19, 6)]
        [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableArticle22, 7)]
        [InlineData(ChargeItemCaseNatureOfVatGR.ExemptArticle43TravelAgencies, 20)]
        [InlineData(ChargeItemCaseNatureOfVatGR.ExemptArticle25CustomsRegimes, 9)]
        [InlineData(ChargeItemCaseNatureOfVatGR.ExemptArticle39SmallBusinesses, 15)]
        [InlineData(ChargeItemCaseNatureOfVatGR.VatPaidOtherEUArticle13, 3)]
        [InlineData(ChargeItemCaseNatureOfVatGR.VatPaidOtherEUArticle14, 4)]
        [InlineData(ChargeItemCaseNatureOfVatGR.ExcludedArticle2And3, 1)]
        [InlineData(ChargeItemCaseNatureOfVatGR.ExcludedArticle5BusinessTransfer, 2)]
        [InlineData(ChargeItemCaseNatureOfVatGR.ExcludedArticle26TaxWarehouses, 10)]
        [InlineData(ChargeItemCaseNatureOfVatGR.ExcludedArticle27Diplomatic, 11)]
        public void GetVatExemptionCategory_AllNatureTypes_ReturnsCorrectCategory(ChargeItemCaseNatureOfVatGR natureType, int expectedCategory)
        {
            // Arrange
            var chargeItem = CreateChargeItem(1, 100, 24, "Test item", ChargeItemCaseTypeOfService.Delivery, ChargeItemCase.NormalVatRate);
            chargeItem.ftChargeItemCase = chargeItem.ftChargeItemCase.WithNatureOfVat(natureType);

            // Act
            var result = AADEMappings.GetVatExemptionCategory(chargeItem);

            // Assert
            result.Should().Be(expectedCategory);
        }
    }
}
