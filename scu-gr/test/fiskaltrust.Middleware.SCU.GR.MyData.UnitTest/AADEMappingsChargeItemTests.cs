using System;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest
{
    public class AADEMappingsChargeItemTests
    {

        [Theory]
        [InlineData("PieCes", 1)]
        [InlineData("pieces", 1)]
        [InlineData("PIECES", 1)]
        [InlineData("Kg", 2)]
        [InlineData("Litres", 3)]
        [InlineData("Meters", 4)]
        [InlineData("SquareMeters", 5)]
        [InlineData("CubicMeters", 6)]
        [InlineData("OtherPieces", 7)]
        // Empty / null Unit defaults to Pieces (issue #182).
        [InlineData("", 1)]
        [InlineData(null, 1)]
        // A non-empty, free-form Unit string is routed to OtherPieces (issue #182);
        // the original string is preserved by AADEMappings.ApplyMeasurementUnit.
        [InlineData("Unknown", 7)]
        [InlineData("barrels", 7)]
        public void GetMeasurementUnit_ShouldReturnExpectedValue(string unit, int expectedValue)
        {
            // Arrange
            var chargeItem = new ChargeItem
            {
                Unit = unit
            };

            // Act
            var result = AADEMappings.GetMeasurementUnit(chargeItem);

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithoutUnit_DefaultsToPiecesAndKeepsQuantity()
        {
            // Rule: empty Unit → quantity stays as ChargeItem.Quantity, measurementUnit = Pieces.
            var row = new InvoiceRowType { quantity = 5m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 5m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnitSpecified.Should().BeTrue();
            row.measurementUnit.Should().Be(MyDataMeasurementUnit.Pieces);
            row.quantity.Should().Be(5m);
            row.otherMeasurementUnitTitle.Should().BeNull();
        }

        [Fact]
        public void ApplyMeasurementUnit_WithUnit_WithoutUnitQuantity_KeepsQuantity()
        {
            // Rule: Unit set, UnitQuantity missing → ChargeItem.Quantity is used as UnitQuantity
            // (i.e. the row's quantity stays as ChargeItem.Quantity).
            var row = new InvoiceRowType { quantity = 2.5m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 2.5m, Unit = "kg" };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnitSpecified.Should().BeTrue();
            row.measurementUnit.Should().Be(MyDataMeasurementUnit.Kg);
            row.quantity.Should().Be(2.5m);
            row.otherMeasurementUnitTitle.Should().BeNull();
        }

        [Fact]
        public void ApplyMeasurementUnit_WithUnit_AndUnitQuantity_UsesUnitQuantityForRowQuantity()
        {
            // Rule: Unit set, UnitQuantity set → row.quantity = UnitQuantity.
            var row = new InvoiceRowType { quantity = 3m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 3m, Unit = "kg", UnitQuantity = 12.5m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.Kg);
            row.quantity.Should().Be(12.5m);
            row.quantitySpecified.Should().BeTrue();
            row.otherMeasurementUnitTitle.Should().BeNull();
        }

        [Fact]
        public void ApplyMeasurementUnit_WithFreeFormUnit_PreservesTitleAndAppliesQuantityRule()
        {
            // Free-form Unit ("barrels"): goes to OtherPieces, title preserved; quantity rule applies.
            var row = new InvoiceRowType { quantity = 3m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 3m, Unit = "barrels", UnitQuantity = 8m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.OtherPieces);
            row.otherMeasurementUnitTitle.Should().Be("barrels");
            row.quantity.Should().Be(8m);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithRefundSignedQuantity_AndUnitQuantity_PreservesSign()
        {
            // The caller has already set row.quantity = -ChargeItem.Quantity for a refund. When we
            // override with UnitQuantity we must keep the negative sign so refund semantics survive.
            var row = new InvoiceRowType { quantity = -3m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 3m, Unit = "kg", UnitQuantity = 12.5m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.quantity.Should().Be(-12.5m);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithExplicitOtherPiecesCode_DoesNotSetTitle()
        {
            // "OtherPieces" matches the AADE-defined enum name, so we don't carry a title.
            var row = new InvoiceRowType { quantity = 4m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 4m, Unit = "OtherPieces", UnitQuantity = 4m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.OtherPieces);
            row.otherMeasurementUnitTitle.Should().BeNull();
            row.quantity.Should().Be(4m);
        }
    }
}
