using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Extensions
{
    public class ReceiptRequestExtensionsTests
    {
        [Fact]
        public void GetTotals_ShouldCalculateTotalsCorrectly()
        {
            // Arrange
            var request = new ReceiptRequest
            {
                cbChargeItems = new ChargeItem[]
                {
                    new ChargeItem { ftChargeItemCase = 0x465200000000001E, Amount = 10.0m, VATRate = 5.5m },
                    new ChargeItem { ftChargeItemCase = 0x465200000000001C, Amount = 20.0m, VATRate = 10.0m },
                    new ChargeItem { ftChargeItemCase = 0x465200000000001D, Amount = 30.0m, VATRate = 20.0m },
                    new ChargeItem { ftChargeItemCase = 0x465200000000001F, Amount = 40.0m, VATRate = 0.0m },
                    new ChargeItem { ftChargeItemCase = 0x4652000000000020, Amount = 50.0m, VATRate = 15.0m }
                },
                cbPayItems = new PayItem[]
                {
                    new PayItem { ftPayItemCase = 0x4652000000000000, Amount = 100.0m },
                    new PayItem { ftPayItemCase = 0x4652000000000004, Amount = 200.0m },
                    new PayItem { ftPayItemCase = 0x465200000000000B, Amount = 300.0m },
                    new PayItem { ftPayItemCase = 0x4652000000000013, Amount = 500.0m }
                }
            };

            var expectedTotals = new Totals
            {
                Totalizer = 150.0m,
                CITotalNormal = 10.0m,
                CITotalReduced1 = 20.0m,
                CITotalReduced2 = 30.0m,
                CITotalReducedS = 40.0m,
                CITotalZero = 50.0m,
                PITotalCash = 100.0m,
                PITotalNonCash = 200.0m,
                PITotalInternal = 300.0m,
                PITotalUnknown = 500.0m
            };

            // Act
            var actualTotals = request.GetTotals();

            // Assert
            Assert.Equal(expectedTotals.Totalizer, actualTotals.Totalizer);
            Assert.Equal(expectedTotals.CITotalNormal, actualTotals.CITotalNormal);
            Assert.Equal(expectedTotals.CITotalReduced1, actualTotals.CITotalReduced1);
            Assert.Equal(expectedTotals.CITotalReduced2, actualTotals.CITotalReduced2);
            Assert.Equal(expectedTotals.CITotalReducedS, actualTotals.CITotalReducedS);
            Assert.Equal(expectedTotals.CITotalZero, actualTotals.CITotalZero);
            Assert.Equal(expectedTotals.CITotalUnknown, actualTotals.CITotalUnknown);
            Assert.Equal(expectedTotals.PITotalCash, actualTotals.PITotalCash);
            Assert.Equal(expectedTotals.PITotalNonCash, actualTotals.PITotalNonCash);
            Assert.Equal(expectedTotals.PITotalInternal, actualTotals.PITotalInternal);
            Assert.Equal(expectedTotals.PITotalUnknown, actualTotals.PITotalUnknown);
        }
    }
}
