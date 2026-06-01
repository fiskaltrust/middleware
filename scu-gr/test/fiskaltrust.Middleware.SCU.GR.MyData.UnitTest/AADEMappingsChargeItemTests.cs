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
            row.otherMeasurementUnitQuantitySpecified.Should().BeFalse();
        }

        [Fact]
        public void ApplyMeasurementUnit_WithStandardUnit_WithoutUnitQuantity_KeepsQuantity()
        {
            // Rule: Unit set, UnitQuantity missing → ChargeItem.Quantity is used as UnitQuantity
            // (i.e. the row's quantity stays as ChargeItem.Quantity). Standard units (kg, ...)
            // do not populate otherMeasurement* fields (those are reserved for measurementUnit=7).
            var row = new InvoiceRowType { quantity = 2.5m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 2.5m, Unit = "kg" };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnitSpecified.Should().BeTrue();
            row.measurementUnit.Should().Be(MyDataMeasurementUnit.Kg);
            row.quantity.Should().Be(2.5m);
            row.otherMeasurementUnitTitle.Should().BeNull();
            row.otherMeasurementUnitQuantitySpecified.Should().BeFalse();
        }

        [Fact]
        public void ApplyMeasurementUnit_WithStandardUnit_AndUnitQuantity_UsesUnitQuantityForRowQuantity()
        {
            // Rule: Unit set, UnitQuantity set → row.quantity = UnitQuantity.
            var row = new InvoiceRowType { quantity = 3m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 3m, Unit = "kg", UnitQuantity = 12.5m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.Kg);
            row.quantity.Should().Be(12.5m);
            row.quantitySpecified.Should().BeTrue();
            row.otherMeasurementUnitTitle.Should().BeNull();
            row.otherMeasurementUnitQuantitySpecified.Should().BeFalse();
        }

        [Fact]
        public void ApplyMeasurementUnit_WithFreeFormUnit_AndUnitQuantity_FillsAllOtherFields()
        {
            // Per myDATA spec §5.4 rule #9: measurementUnit=7 requires both
            // otherMeasurementUnitTitle (packaging description) and otherMeasurementUnitQuantity.
            // Both row.quantity and row.otherMeasurementUnitQuantity are sourced from
            // ChargeItem.UnitQuantity when present.
            var row = new InvoiceRowType { quantity = 3m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 3m, Unit = "Παλέτες", UnitQuantity = 30m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.OtherPieces);
            row.otherMeasurementUnitTitle.Should().Be("Παλέτες");
            row.otherMeasurementUnitQuantitySpecified.Should().BeTrue();
            row.otherMeasurementUnitQuantity.Should().Be(30);
            row.quantity.Should().Be(30m);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithFreeFormUnit_WithoutUnitQuantity_FallsBackToQuantity()
        {
            // When UnitQuantity is missing, ChargeItem.Quantity stands in as item count (per the
            // user fallback rule). otherMeasurementUnitQuantity still carries the same value, since
            // we only have one count to work with — but the spec contract (both fields populated)
            // is satisfied.
            var row = new InvoiceRowType { quantity = 7m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 7m, Unit = "barrels" };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.OtherPieces);
            row.otherMeasurementUnitTitle.Should().Be("barrels");
            row.otherMeasurementUnitQuantitySpecified.Should().BeTrue();
            row.otherMeasurementUnitQuantity.Should().Be(7);
            row.quantity.Should().Be(7m);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithUnitQuantity_AlwaysEmitsPositiveQuantity()
        {
            // AADE schema requires quantity > 0 (minExclusive="0"). Even if UnitQuantity is provided
            // as a negative value, the row must carry the absolute value.
            var row = new InvoiceRowType { quantity = 3m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 3m, Unit = "kg", UnitQuantity = -12.5m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.quantity.Should().Be(12.5m);
        }

        [Fact]
        public void EnsureOtherPiecesMandatoryFields_WhenOverrideFlipsToSeven_FillsTitleFromUnit()
        {
            // Simulates an override that set measurementUnit=7 without populating the title.
            // The guard must back-fill from chargeItem.Unit so the row stays spec-valid.
            var row = new InvoiceRowType
            {
                quantity = 5m,
                quantitySpecified = true,
                measurementUnit = MyDataMeasurementUnit.OtherPieces,
                measurementUnitSpecified = true
            };
            var chargeItem = new ChargeItem { Quantity = 5m, Unit = "Παλέτες" };

            AADEMappings.EnsureOtherPiecesMandatoryFields(row, chargeItem);

            row.otherMeasurementUnitTitle.Should().Be("Παλέτες");
            row.otherMeasurementUnitQuantitySpecified.Should().BeTrue();
            row.otherMeasurementUnitQuantity.Should().Be(5);
        }

        [Fact]
        public void EnsureOtherPiecesMandatoryFields_WithExistingTitle_DoesNotOverwrite()
        {
            // If the override already supplied a title we must respect it.
            var row = new InvoiceRowType
            {
                quantity = 10m,
                quantitySpecified = true,
                measurementUnit = MyDataMeasurementUnit.OtherPieces,
                measurementUnitSpecified = true,
                otherMeasurementUnitTitle = "Δοχεία",
                otherMeasurementUnitQuantity = 2,
                otherMeasurementUnitQuantitySpecified = true
            };
            var chargeItem = new ChargeItem { Quantity = 5m, Unit = "Παλέτες" };

            AADEMappings.EnsureOtherPiecesMandatoryFields(row, chargeItem);

            row.otherMeasurementUnitTitle.Should().Be("Δοχεία");
            row.otherMeasurementUnitQuantity.Should().Be(2);
        }

        [Fact]
        public void EnsureOtherPiecesMandatoryFields_WhenMeasurementUnitNotSeven_DoesNothing()
        {
            // Other measurement units must not get otherMeasurement* fields.
            var row = new InvoiceRowType
            {
                quantity = 5m,
                quantitySpecified = true,
                measurementUnit = MyDataMeasurementUnit.Kg,
                measurementUnitSpecified = true
            };
            var chargeItem = new ChargeItem { Quantity = 5m, Unit = "kg" };

            AADEMappings.EnsureOtherPiecesMandatoryFields(row, chargeItem);

            row.otherMeasurementUnitTitle.Should().BeNull();
            row.otherMeasurementUnitQuantitySpecified.Should().BeFalse();
        }

        [Fact]
        public void ApplyMeasurementUnit_WithExplicitOtherPiecesCode_StillSetsMandatoryOtherFields()
        {
            // Even when the caller passes the AADE code name "OtherPieces" as Unit, the spec
            // mandates that both otherMeasurementUnitTitle and otherMeasurementUnitQuantity are
            // filled. We pass the original string through as the title.
            var row = new InvoiceRowType { quantity = 4m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 4m, Unit = "OtherPieces", UnitQuantity = 4m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.OtherPieces);
            row.otherMeasurementUnitTitle.Should().Be("OtherPieces");
            row.otherMeasurementUnitQuantitySpecified.Should().BeTrue();
            row.otherMeasurementUnitQuantity.Should().Be(4);
            row.quantity.Should().Be(4m);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithoutUnit_NormalizesNegativeRowQuantityToPositive()
        {
            // Refund flows pre-set row.quantity to a negative value (AADEFactory line 1189).
            // AADE schema requires quantity > 0 even on the empty-Unit / default-Pieces path.
            var row = new InvoiceRowType { quantity = -5m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 5m };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.Pieces);
            row.quantity.Should().Be(5m);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithStandardUnit_AndNegativeRowQuantity_NormalizesToPositive()
        {
            // Same normalization applies when Unit maps to a standard code and UnitQuantity is missing.
            var row = new InvoiceRowType { quantity = -2.5m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 2.5m, Unit = "kg" };

            AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            row.measurementUnit.Should().Be(MyDataMeasurementUnit.Kg);
            row.quantity.Should().Be(2.5m);
        }

        [Fact]
        public void ApplyMeasurementUnit_WithFractionalUnitQuantityForOtherPieces_Throws()
        {
            // otherMeasurementUnitQuantity is an int in the AADE schema; we refuse silent
            // rounding of fractional source quantities (UnitQuantity takes precedence over Quantity).
            var row = new InvoiceRowType { quantity = 1.5m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 1.5m, Unit = "barrels", UnitQuantity = 1.5m };

            var act = () => AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            act.Should().Throw<Exception>()
                .WithMessage("*whole number*otherMeasurementUnitQuantity*");
        }

        [Fact]
        public void ApplyMeasurementUnit_WithFractionalQuantityFallbackForOtherPieces_Throws()
        {
            // When UnitQuantity is absent the fallback is ChargeItem.Quantity, which must
            // also be a whole number for measurementUnit=7.
            var row = new InvoiceRowType { quantity = 1.5m, quantitySpecified = true };
            var chargeItem = new ChargeItem { Quantity = 1.5m, Unit = "barrels" };

            var act = () => AADEMappings.ApplyMeasurementUnit(row, chargeItem);

            act.Should().Throw<Exception>()
                .WithMessage("*whole number*otherMeasurementUnitQuantity*");
        }

        [Fact]
        public void EnsureOtherPiecesMandatoryFields_WithFractionalQuantity_Throws()
        {
            // Same whole-number contract applies in the post-override guard.
            var row = new InvoiceRowType
            {
                quantity = 5m,
                quantitySpecified = true,
                measurementUnit = MyDataMeasurementUnit.OtherPieces,
                measurementUnitSpecified = true
            };
            var chargeItem = new ChargeItem { Quantity = 2.5m, Unit = "barrels" };

            var act = () => AADEMappings.EnsureOtherPiecesMandatoryFields(row, chargeItem);

            act.Should().Throw<Exception>()
                .WithMessage("*whole number*otherMeasurementUnitQuantity*");
        }
    }
}
