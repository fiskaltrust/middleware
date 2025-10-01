using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.SCU.MyData
{
    public class WithholdingTaxMappingsTests
    {
        [Fact]
        public void IsWithholdingTaxItem_ShouldReturnTrue_WhenTypeOfServiceIsF()
        {
            var chargeItem = new ChargeItem
            {
                ftChargeItemCase = (ChargeItemCase)0x47520000000000F0 // TypeOfService = 0xF
            };

            var result = WithholdingTaxMappings.IsSpecialTaxItem(chargeItem);
            result.Should().BeTrue();
        }

        [Fact]
        public void IsWithholdingTaxItem_ShouldReturnFalse_WhenTypeOfServiceIsNot0xF()
        {
            var chargeItem = new ChargeItem
            {
                ftChargeItemCase = (ChargeItemCase)0x4752000000000001 // TypeOfService = 0x1
            };

            var result = WithholdingTaxMappings.IsSpecialTaxItem(chargeItem);
            result.Should().BeFalse();
        }

        [Fact]
        public void GetWithholdingTaxMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid description";

            var mapping = WithholdingTaxMappings.GetWithholdingTaxMapping(description);
            mapping.Should().BeNull();
        }

        [Theory]
        [InlineData("Περιπτ. β’- Τόκοι - 15%", 1, 15, false)]
        [InlineData("Περιπτ. γ’ - Δικαιώματα - 20%", 2, 20, false)]
        [InlineData("Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%", 3, 20, false)]
        [InlineData("Περιπτ. δ' - Τεχνικά Έργα - 3%", 4, 3, false)]
        [InlineData("Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%", 5, 1, false)]
        [InlineData("Λοιπά Αγαθά 4%", 6, 4, false)]
        [InlineData("Παροχή Υπηρεσιών 8%", 7, 8, false)]
        [InlineData("Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%", 8, 4, false)]
        [InlineData("Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%", 9, 10, false)]
        [InlineData("Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%", 10, 15, false)]
        [InlineData("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013", 11, null, true)]
        [InlineData("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού", 12, 15, false)]
        [InlineData("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού", 13, 10, false)]
        [InlineData("Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης", 14, null, true)]
        [InlineData("Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013", 15, null, true)]
        [InlineData("Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)", 16, null, true)]
        [InlineData("Λοιπές Παρακρατήσεις Φόρου", 17, null, true)]
        [InlineData("Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013", 18, 5, false)]
        public void GetWithholdingTaxMapping_ShouldHandleAllDefinedMappings(string description, int expectedCode, int? expectedPercentage, bool expectedIsFixed)
        {
            // Act
            var mapping = WithholdingTaxMappings.GetWithholdingTaxMapping(description);

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
            mapping.Percentage.Should().Be(expectedPercentage, $"percentage for '{description}' should be {expectedPercentage}");
            mapping.IsFixedAmount.Should().Be(expectedIsFixed, $"IsFixedAmount for '{description}' should be {expectedIsFixed}");
        }

        [Theory]
        [InlineData("Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013")]
        [InlineData("Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης")]
        [InlineData("Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013")]
        [InlineData("Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)")]
        [InlineData("Λοιπές Παρακρατήσεις Φόρου")]
        public void CalculateWithholdingTaxAmount_ShouldReturnZeroForFixedAmountType(string description)
        {
            var mapping = WithholdingTaxMappings.GetWithholdingTaxMapping(description);
            var netAmount = 1000m;

            var withholdingAmount = WithholdingTaxMappings.CalculateWithholdingTaxAmount(netAmount, mapping);
            withholdingAmount.Should().Be(0m); // Fixed amount types return 0, amount comes from charge item
        }

        [Fact]
        public void CalculateWithholdingTaxAmount_ShouldHandleNullReference()
        {
            // Act & Assert
            Action act = () => WithholdingTaxMappings.CalculateWithholdingTaxAmount(1000m, null!);
            act.Should().Throw<NullReferenceException>();
        }
    }
}