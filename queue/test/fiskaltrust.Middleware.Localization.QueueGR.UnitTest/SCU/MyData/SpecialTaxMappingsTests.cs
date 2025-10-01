using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.SCU.MyData
{
    public class SpecialTaxMappingsTests
    {
        [Fact]
        public void IsSpecialTaxItem_ShouldReturnTrue_WhenTypeOfServiceIsF()
        {
            var chargeItem = new ChargeItem
            {
                ftChargeItemCase = (ChargeItemCase)0x47520000000000F0 // TypeOfService = 0xF
            };

            var result = SpecialTaxMappings.IsSpecialTaxItem(chargeItem);
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSpecialTaxItem_ShouldReturnFalse_WhenTypeOfServiceIsNot0xF()
        {
            var chargeItem = new ChargeItem
            {
                ftChargeItemCase = (ChargeItemCase)0x4752000000000001 // TypeOfService = 0x1
            };

            var result = SpecialTaxMappings.IsSpecialTaxItem(chargeItem);
            result.Should().BeFalse();
        }

        [Fact]
        public void GetWithholdingTaxMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid description";

            var mapping = SpecialTaxMappings.GetWithholdingTaxMapping(description);
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
            var mapping = SpecialTaxMappings.GetWithholdingTaxMapping(description);

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
            mapping.Percentage.Should().Be(expectedPercentage, $"percentage for '{description}' should be {expectedPercentage}");
            mapping.IsFixedAmount.Should().Be(expectedIsFixed, $"IsFixedAmount for '{description}' should be {expectedIsFixed}");
        }

        [Fact]
        public void GetFeeMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid fee description";

            var mapping = SpecialTaxMappings.GetFeeMapping(description);
            mapping.Should().BeNull();
        }

        [Theory]
        [InlineData("Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%", 1, 12, false)]
        [InlineData("Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%", 2, 15, false)]
        [InlineData("Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%", 3, 18, false)]
        [InlineData("Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%", 4, 20, false)]
        [InlineData("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)", 5, 12, false)]
        [InlineData("Τέλος στη συνδρομητική τηλεόραση 10%", 6, 10, false)]
        [InlineData("Τέλος συνδρομητών σταθερής τηλεφωνίας 5%", 7, 5, false)]
        [InlineData("Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο", 8, null, true)]
        [InlineData("Εισφορά δακοκτονίας 2%", 9, 2, false)]
        [InlineData("Λοιπά τέλη", 10, null, true)]
        [InlineData("Τέλη Λοιπών Φόρων", 11, null, true)]
        [InlineData("Εισφορά δακοκτονίας", 12, null, true)]
        [InlineData("Για μηνιαίο λογαριασμό κάθε σύνδεσης (10%)", 13, 10, false)]
        [InlineData("Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (10%)", 14, 10, false)]
        [InlineData("Τέλος κινητής και καρτοκινητής για φυσικά πρόσωπα ηλικίας 15 έως και 29 ετών (0%)", 15, 0, false)]
        [InlineData("Εισφορά προστασίας περιβάλλοντος πλαστικών προϊόντων 0,04 λεπτά ανά τεμάχιο [άρθρο 4 ν. 4736/2020]", 16, null, true)]
        [InlineData("Τέλος ανακύκλωσης 0,08 λεπτά ανά τεμάχιο [άρθρο 80 ν. 4819/2021]", 17, null, true)]
        [InlineData("Τέλος διαμονής παρεπιδημούντων", 18, null, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των εστιατορίων και συναφών καταστημάτων", 19, null, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των κέντρων διασκέδασης", 20, null, true)]
        [InlineData("Τέλος επί των ακαθάριστων εσόδων των καζίνο", 21, null, true)]
        [InlineData("Λοιπά τέλη επί των ακαθάριστων εσόδων", 22, null, true)]
        public void GetFeeMapping_ShouldHandleAllDefinedMappings(string description, int expectedCode, int? expectedPercentage, bool expectedIsFixed)
        {
            // Act
            var mapping = SpecialTaxMappings.GetFeeMapping(description);

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
            mapping.Percentage.Should().Be(expectedPercentage, $"percentage for '{description}' should be {expectedPercentage}");
            mapping.IsFixedAmount.Should().Be(expectedIsFixed, $"IsFixedAmount for '{description}' should be {expectedIsFixed}");
        }

        [Fact]
        public void GetStampDutyMapping_ShouldReturnNullWhenDescriptionNotFound()
        {
            var description = "Invalid stamp duty description";

            var mapping = SpecialTaxMappings.GetStampDutyMapping(description);
            mapping.Should().BeNull();
        }

        [Theory]
        [InlineData("Συντελεστής 1,2 %", 1, "1,2", false)]
        [InlineData("Συντελεστής 2,4 %", 2, "2,4", false)]
        [InlineData("Συντελεστής 3,6 %", 3, "3,6", false)]
        [InlineData("Λοιπές περιπτώσεις Χαρτοσήμου", 4, null, true)]
        public void GetStampDutyMapping_ShouldHandleAllDefinedMappings(string description, int expectedCode, string? expectedPercentageStr, bool expectedIsFixed)
        {
            // Act
            var mapping = SpecialTaxMappings.GetStampDutyMapping(description);
            decimal? expectedPercentage = expectedPercentageStr != null ? decimal.Parse(expectedPercentageStr) : null;

            // Assert
            mapping.Should().NotBeNull($"mapping for '{description}' should exist");
            mapping.Code.Should().Be(expectedCode, $"code for '{description}' should be {expectedCode}");
            mapping.Percentage.Should().Be(expectedPercentage, $"percentage for '{description}' should be {expectedPercentage}");
            mapping.IsFixedAmount.Should().Be(expectedIsFixed, $"IsFixedAmount for '{description}' should be {expectedIsFixed}");
        }
    }
}