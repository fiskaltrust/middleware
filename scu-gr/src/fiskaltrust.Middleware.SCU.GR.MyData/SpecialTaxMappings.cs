using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

/// <summary>
/// Provides mapping functionality for Greek withholding taxes, fees, stamp duties, and other taxes based on AADE requirements.
/// Maps charge item descriptions to withholding tax categories, percentages, fee categories, stamp duty categories, and other tax categories.
/// </summary>
public static class SpecialTaxMappings
{
    /// <summary>
    /// Represents a withholding tax mapping with code, description, and percentage.
    /// </summary>
    public record WithholdingTaxMapping(
        int Code,
        string GreekDescription,
        decimal? Percentage,
        bool IsFixedAmount = false
    );

    /// <summary>
    /// Represents a fee mapping with code, description, and percentage.
    /// </summary>
    public record FeeMapping(
        int Code,
        string GreekDescription,
        decimal? Percentage,
        bool IsFixedAmount = false
    );

    /// <summary>
    /// Represents a stamp duty mapping with code, description, and percentage.
    /// </summary>
    public record StampDutyMapping(
        int Code,
        string GreekDescription,
        decimal? Percentage,
        bool IsFixedAmount = false
    );

    /// <summary>
    /// Represents an other tax mapping with code, description, and percentage.
    /// </summary>
    public record OtherTaxMapping(
        int Code,
        string GreekDescription,
        decimal? Percentage,
        bool IsFixedAmount = false
    );

    /// <summary>
    /// Dictionary mapping Greek descriptions to withholding tax information.
    /// Based on the official AADE withholding tax table.
    /// </summary>
    private static readonly Dictionary<string, WithholdingTaxMapping> _withholdingTaxMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Code 1: Περιπτ. β'- Τόκοι - 15%
        // Code 1: Case b - Interest - 15%
        ["Περιπτ. β'- Τόκοι - 15%"] = new(1, "Περιπτ. β'- Τόκοι - 15%", 15m),
        // Code 2: Περιπτ. γ' - Δικαιώματα - 20%
        // Code 2: Case c - Rights/Royalties - 20%
        ["Περιπτ. γ' - Δικαιώματα - 20%"] = new(2, "Περιπτ. γ' - Δικαιώματα - 20%", 20m),
        // Code 3: Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%
        // Code 3: Case d - Board of Directors Fees - 20%
        ["Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%"] = new(3, "Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%", 20m),
        // Code 4: Περιπτ. δ' - Τεχνικά Έργα - 3%
        // Code 4: Case d - Technical Works - 3%
        ["Περιπτ. δ' - Τεχνικά Έργα - 3%"] = new(4, "Περιπτ. δ' - Τεχνικά Έργα - 3%", 3m),
        // Code 5: Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%
        // Code 5: Liquid fuels and tobacco products 1%
        ["Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%"] = new(5, "Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%", 1m),
        // Code 6: Λοιπά Αγαθά 4%
        // Code 6: Other Goods 4%
        ["Λοιπά Αγαθά 4%"] = new(6, "Λοιπά Αγαθά 4%", 4m),
        // Code 7: Παροχή Υπηρεσιών 8%
        // Code 7: Service Provision 8%
        ["Παροχή Υπηρεσιών 8%"] = new(7, "Παροχή Υπηρεσιών 8%", 8m),
        // Code 8: Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%
        // Code 8: Advance Tax for Architects and Engineers on Contractual Fees, for Study and Design Preparation 4%
        ["Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%"] = new(8, "Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%", 4m),
        // Code 9: Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%
        // Code 9: Advance Tax for Architects and Engineers on Contractual Fees, concerning any other nature works 10%
        ["Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%"] = new(9, "Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%", 10m),
        // Code 10: Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%
        // Code 10: Advance Tax on Lawyers' Fees 15%
        ["Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%"] = new(10, "Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%", 15m),
        // Code 11: Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013 (fixed amount)
        // Code 11: Withholding Tax on Employment Services par. 1 art. 15 law 4172/2013 (fixed amount)
        ["Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013"] = new(11, "Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013", null, true),
        // Code 12: Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού 15%
        // Code 12: Withholding Tax on Employment Services par. 2 art. 15 law 4172/2013 - Merchant Marine Officers 15%
        ["Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού"] = new(12, "Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού", 15m),
        // Code 13: Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού 10%
        // Code 13: Withholding Tax on Employment Services par. 2 art. 15 law 4172/2013 - Lower Crew Merchant Marine 10%
        ["Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού"] = new(13, "Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού", 10m),
        // Code 14: Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης (fixed amount)
        // Code 14: Withholding of Special Solidarity Contribution (fixed amount)
        ["Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης"] = new(14, "Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης", null, true),
        // Code 15: Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013 (fixed amount)
        // Code 15: Withholding Tax on Compensation due to Employment Relationship Termination par. 3 art. 15 law 4172/2013 (fixed amount)
        ["Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013"] = new(15, "Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013", null, true),
        // Code 16: Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.) (fixed amount)
        // Code 16: Foreign transaction withholdings based on double taxation avoidance agreements (D.T.A.A.) (fixed amount)
        ["Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)"] = new(16, "Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)", null, true),
        // Code 17: Λοιπές Παρακρατήσεις Φόρου (fixed amount)
        // Code 17: Other Tax Withholdings (fixed amount)
        ["Λοιπές Παρακρατήσεις Φόρου"] = new(17, "Λοιπές Παρακρατήσεις Φόρου", null, true),
        // Code 18: Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013 5%
        // Code 18: Withholding Tax on Dividends case a par. 1 art. 64 law 4172/2013 5%
        ["Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013"] = new(18, "Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013", 5m),
    };

    /// <summary>
    /// Dictionary mapping Greek descriptions to fee information.
    /// Based on the official AADE fee table.
    /// </summary>
    private static readonly Dictionary<string, FeeMapping> _feeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Code 1: Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%
        // Code 1: For monthly bill up to 50 euros 12%
        ["Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%"] = new(1, "Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%", 12m),
        // Code 2: Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%
        // Code 2: For monthly bill from 50.01 to 100 euros 15%
        ["Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%"] = new(2, "Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%", 15m),
        // Code 3: Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%
        // Code 3: For monthly bill from 100.01 to 150 euros 18%
        ["Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%"] = new(3, "Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%", 18m),
        // Code 4: Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%
        // Code 4: For monthly bill from 150.01 euros and above 20%
        ["Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%"] = new(4, "Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%", 20m),
        // Code 5: Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)
        // Code 5: Prepaid mobile fee on the value of talk time (12%)
        ["Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)"] = new(5, "Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)", 12m),
        // Code 6: Τέλος στη συνδρομητική τηλεόραση 10%
        // Code 6: Fee on subscription television 10%
        ["Τέλος στη συνδρομητική τηλεόραση 10%"] = new(6, "Τέλος στη συνδρομητική τηλεόραση 10%", 10m),
        // Code 7: Τέλος συνδρομητών σταθερής τηλεφωνίας 5%
        // Code 7: Fixed telephony subscribers fee 5%
        ["Τέλος συνδρομητών σταθερής τηλεφωνίας 5%"] = new(7, "Τέλος συνδρομητών σταθερής τηλεφωνίας 5%", 5m),
        // Code 8: Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α – 0,07 ευρώ ανά τεμάχιο (fixed amount)
        // Code 8: Environmental fee & plastic bag law 2339/2001 art. 6a – 0.07 euros per piece (fixed amount)
        ["Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο"] = new(8, "Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο", null, true),
        // Code 9: Εισφορά δακοκτονίας 2%
        // Code 9: Fruit fly control contribution 2%
        ["Εισφορά δακοκτονίας 2%"] = new(9, "Εισφορά δακοκτονίας 2%", 2m),
        // Code 10: Λοιπά τέλη (fixed amount)
        // Code 10: Other fees (fixed amount)
        ["Λοιπά τέλη"] = new(10, "Λοιπά τέλη", null, true),
        // Code 11: Τέλη Λοιπών Φόρων (fixed amount)
        // Code 11: Other taxes fees (fixed amount)
        ["Τέλη Λοιπών Φόρων"] = new(11, "Τέλη Λοιπών Φόρων", null, true),
        // Code 12: Εισφορά δακοκτονίας (fixed amount)
        // Code 12: Fruit fly control contribution (fixed amount)
        ["Εισφορά δακοκτονίας"] = new(12, "Εισφορά δακοκτονίας", null, true),
        // Code 13: Για μηνιαίο λογαριασμό κάθε σύνδεσης (10%)
        // Code 13: For monthly bill of each connection (10%)
        ["Για μηνιαίο λογαριασμό κάθε σύνδεσης (10%)"] = new(13, "Για μηνιαίο λογαριασμό κάθε σύνδεσης (10%)", 10m),
        // Code 14: Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (10%)
        // Code 14: Prepaid mobile fee on the value of talk time (10%)
        ["Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (10%)"] = new(14, "Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (10%)", 10m),
        // Code 15: Τέλος κινητής και καρτοκινητής για φυσικά πρόσωπα ηλικίας 15 έως και 29 ετών (0%)
        // Code 15: Mobile and prepaid mobile fee for natural persons aged 15 to 29 years (0%)
        ["Τέλος κινητής και καρτοκινητής για φυσικά πρόσωπα ηλικίας 15 έως και 29 ετών (0%)"] = new(15, "Τέλος κινητής και καρτοκινητής για φυσικά πρόσωπα ηλικίας 15 έως και 29 ετών (0%)", 0m),
        // Code 16: Εισφορά προστασίας περιβάλλοντος πλαστικών προϊόντων 0,04 λεπτά ανά τεμάχιο (fixed amount)
        // Code 16: Environmental protection contribution for plastic products 0.04 cents per piece (fixed amount)
        ["Εισφορά προστασίας περιβάλλοντος πλαστικών προϊόντων 0,04 λεπτά ανά τεμάχιο [άρθρο 4 ν. 4736/2020]"] = new(16, "Εισφορά προστασίας περιβάλλοντος πλαστικών προϊόντων 0,04 λεπτά ανά τεμάχιο [άρθρο 4 ν. 4736/2020]", null, true),
        // Code 17: Τέλος ανακύκλωσης 0,08 λεπτά ανά τεμάχιο (fixed amount)
        // Code 17: Recycling fee 0.08 cents per piece (fixed amount)
        ["Τέλος ανακύκλωσης 0,08 λεπτά ανά τεμάχιο [άρθρο 80 ν. 4819/2021]"] = new(17, "Τέλος ανακύκλωσης 0,08 λεπτά ανά τεμάχιο [άρθρο 80 ν. 4819/2021]", null, true),
        // Code 18: Τέλος διαμονής παρεπιδημούντων (fixed amount)
        // Code 18: Tourist accommodation fee (fixed amount)
        ["Τέλος διαμονής παρεπιδημούντων"] = new(18, "Τέλος διαμονής παρεπιδημούντων", null, true),
        // Code 19: Τέλος επί των ακαθάριστων εσόδων των εστιατορίων και συναφών καταστημάτων (fixed amount)
        // Code 19: Fee on gross revenues of restaurants and related establishments (fixed amount)
        ["Τέλος επί των ακαθάριστων εσόδων των εστιατορίων και συναφών καταστημάτων"] = new(19, "Τέλος επί των ακαθάριστων εσόδων των εστιατορίων και συναφών καταστημάτων", null, true),
        // Code 20: Τέλος επί των ακαθάριστων εσόδων των κέντρων διασκέδασης (fixed amount)
        // Code 20: Fee on gross revenues of entertainment centers (fixed amount)
        ["Τέλος επί των ακαθάριστων εσόδων των κέντρων διασκέδασης"] = new(20, "Τέλος επί των ακαθάριστων εσόδων των κέντρων διασκέδασης", null, true),
        // Code 21: Τέλος επί των ακαθάριστων εσόδων των καζίνο (fixed amount)
        // Code 21: Fee on gross revenues of casinos (fixed amount)
        ["Τέλος επί των ακαθάριστων εσόδων των καζίνο"] = new(21, "Τέλος επί των ακαθάριστων εσόδων των καζίνο", null, true),
        // Code 22: Λοιπά τέλη επί των ακαθάριστων εσόδων (fixed amount)
        // Code 22: Other fees on gross revenues (fixed amount)
        ["Λοιπά τέλη επί των ακαθάριστων εσόδων"] = new(22, "Λοιπά τέλη επί των ακαθάριστων εσόδων", null, true),
    };

    /// <summary>
    /// Dictionary mapping Greek descriptions to stamp duty information.
    /// Based on the official AADE stamp duty table.
    /// </summary>
    private static readonly Dictionary<string, StampDutyMapping> _stampDutyMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Code 1: Συντελεστής 1,2 %
        // Code 1: Rate 1.2%
        ["Συντελεστής 1,2 %"] = new(1, "Συντελεστής 1,2 %", 1.2m),
        // Code 2: Συντελεστής 2,4 %
        // Code 2: Rate 2.4%
        ["Συντελεστής 2,4 %"] = new(2, "Συντελεστής 2,4 %", 2.4m),
        // Code 3: Συντελεστής 3,6 %
        // Code 3: Rate 3.6%
        ["Συντελεστής 3,6 %"] = new(3, "Συντελεστής 3,6 %", 3.6m),
        // Code 4: Λοιπές περιπτώσεις (fixed amount)
        // Code 4: Other cases (fixed amount)
        ["Λοιπές περιπτώσεις Χαρτοσήμου"] = new(4, "Λοιπές περιπτώσεις Χαρτοσήμου", null, true),
    };

    /// <summary>
    /// Dictionary mapping Greek descriptions to other tax information.
    /// Based on the official AADE other tax table.
    /// </summary>
    private static readonly Dictionary<string, OtherTaxMapping> _otherTaxMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Code 1: α1) ασφάλιστρα κλάδου πυρός 20% (15%)
        // Code 1: a1) fire insurance premiums 20% (15%)
        ["α1) ασφάλιστρα κλάδου πυρός 20%"] = new(1, "α1) ασφάλιστρα κλάδου πυρός 20%", 15m),
        // Code 2: α2) ασφάλιστρα κλάδου πυρός 20% (5%)
        // Code 2: a2) fire insurance premiums 20% (5%)
        ["α2) ασφάλιστρα κλάδου πυρός 20%"] = new(2, "α2) ασφάλιστρα κλάδου πυρός 20%", 5m),
        // Code 3: β) ασφάλιστρα κλάδου ζωής 4%
        // Code 3: b) life insurance premiums 4%
        ["β) ασφάλιστρα κλάδου ζωής 4%"] = new(3, "β) ασφάλιστρα κλάδου ζωής 4%", 4m),
        // Code 4: γ) ασφάλιστρα λοιπών κλάδων 15%
        // Code 4: c) other insurance premiums 15%
        ["γ) ασφάλιστρα λοιπών κλάδων 15%"] = new(4, "γ) ασφάλιστρα λοιπών κλάδων 15%", 15m),
        // Code 5: δ) απαλλασσόμενα φόρου ασφαλίστρων 0%
        // Code 5: d) tax-exempt insurance premiums 0%
        ["δ) απαλλασσόμενα φόρου ασφαλίστρων 0%"] = new(5, "δ) απαλλασσόμενα φόρου ασφαλίστρων 0%", 0m),
        // Code 6: Ξενοδοχεία 1‑2 αστέρων 0,50 € (fixed amount)
        // Code 6: Hotels 1-2 stars 0.50 € (fixed amount)
        ["Ξενοδοχεία 1-2 αστέρων 0,50 €"] = new(6, "Ξενοδοχεία 1-2 αστέρων 0,50 €", null, true),
        // Code 7: Ξενοδοχεία 3 αστέρων 1,50 € (fixed amount)
        // Code 7: Hotels 3 stars 1.50 € (fixed amount)
        ["Ξενοδοχεία 3 αστέρων 1,50 €"] = new(7, "Ξενοδοχεία 3 αστέρων 1,50 €", null, true),
        // Code 8: Ξενοδοχεία 4 αστέρων 3,00 € (fixed amount)
        // Code 8: Hotels 4 stars 3.00 € (fixed amount)
        ["Ξενοδοχεία 4 αστέρων 3,00 €"] = new(8, "Ξενοδοχεία 4 αστέρων 3,00 €", null, true),
        // Code 9: Ξενοδοχεία 5 αστέρων 4,00 € (fixed amount)
        // Code 9: Hotels 5 stars 4.00 € (fixed amount)
        ["Ξενοδοχεία 4 αστέρων 4,00 €"] = new(9, "Ξενοδοχεία 4 αστέρων 4, 00 €", null, true),
        // Code 10: Ενοικιαζόμενα – επιπλωμένα δωμάτια – διαμερίσματα 0,50 € (fixed amount)
        // Code 10: Rental furnished rooms - apartments 0.50 € (fixed amount)
        ["Ενοικιαζόμενα - επιπλωμένα δωμάτια - διαμερίσματα 0,50 €"] = new(10, "Ενοικιαζόμενα - επιπλωμένα δωμάτια - διαμερίσματα 0,50 €", null, true),
        // Code 11: Ειδικός Φόρος στις διαφημίσεις που προβάλλονται από την τηλεόραση (ΕΦΤΔ) 5%
        // Code 11: Special tax on advertisements broadcast on television (EFTD) 5%
        ["Ειδικός Φόρος στις διαφημίσεις που προβάλλονται από την τηλεόραση (ΕΦΤΔ) 5%"] = new(11, "Ειδικός Φόρος στις διαφημίσεις που προβάλλονται από την τηλεόραση (ΕΦΤΔ) 5%", 5m),
        // Code 12: 3.1 Φόρος πολυτελείας 10% επί της φορολογητέας αξίας για τα ενδοκοινοτικώς αποκτώμενα και εισαγόμενα από τρίτες χώρες
        // Code 12: 3.1 Luxury tax 10% on the taxable value for intra-community acquisitions and imports from third countries
        ["3.1 Φόρος πολυτελείας 10% επί της φορολογητέας αξίας για τα ενδοκοινοτικώς αποκτούμενα και εισαγόμενα από τρίτες χώρες 10%"] = new(12, "3.1 Φόρος πολυτελείας 10% επί της φορολογητέας αξίας για τα ενδοκοινοτικώς αποκτούμενα και εισαγόμενα από τρίτες χώρες 10%", 10m),
        // Code 13: 3.2 Φόρος πολυτελείας 10% επί της τιμής πώλησης προ Φ.Π.Α. για τα εγχωρίως παραγόμενα είδη
        // Code 13: 3.2 Luxury tax 10% on the selling price before VAT for domestically produced goods
        ["3.2 Φόρος πολυτελείας 10% επί της τιμής πώλησης προ Φ.Π.Α. για τα εγχωρίως παραγόμενα είδη 10%"] = new(13, "3.2 Φόρος πολυτελείας 10% επί της τιμής πώλησης προ Φ.Π.Α. για τα εγχωρίως παραγόμενα είδη 10%", 10m),
        // Code 14: Δικαίωμα του Δημοσίου στα εισιτήρια των καζίνο (80% επί του εισιτηρίου)
        // Code 14: State right on casino tickets (80% on the ticket)
        ["Δικαίωμα του Δημοσίου στα εισιτήρια των καζίνο (80% επί του εισιτηρίου)"] = new(14, "Δικαίωμα του Δημοσίου στα εισιτήρια των καζίνο (80% επί του εισιτηρίου)", 80m),
        // Code 15: ασφάλιστρα κλάδου πυρός 20%
        // Code 15: fire insurance premiums 20%
        ["ασφάλιστρα κλάδου πυρός 20%"] = new(15, "ασφάλιστρα κλάδου πυρός 20%", 20m),
        // Code 16: Λοιποί Τελωνειακοί Δασμοί‑Φόροι (fixed amount)
        // Code 16: Other customs duties-taxes (fixed amount)
        ["Λοιποί Τελωνειακοί Δασμοί-Φόροι"] = new(16, "Λοιποί Τελωνειακοί Δασμοί-Φόροι", null, true),
        // Code 17: Λοιποί Φόροι (fixed amount)
        // Code 17: Other taxes (fixed amount)
        ["Λοιποί Φόροι"] = new(17, "Λοιποί Φόροι", null, true),
        // Code 18: Επιβαρύνσεις Λοιπών Φόρων (fixed amount)
        // Code 18: Other tax charges (fixed amount)
        ["Επιβαρύνσεις Λοιπών Φόρων"] = new(18, "Επιβαρύνσεις Λοιπών Φόρων", null, true),
        // Code 19: ΕΦΚ (fixed amount)
        // Code 19: EFK (Special Consumption Tax) (fixed amount)
        ["ΕΦΚ"] = new(19, "ΕΦΚ", null, true),
        // Code 20: Ξενοδοχεία 1‑2 αστέρων 1,50€ (ανά Δωμ./Διαμ.) (fixed amount)
        // Code 20: Hotels 1-2 stars 1.50€ (per Room/Apt.) (fixed amount)
        ["Ξενοδοχεία 1-2 αστέρων 1,50€ (ανά Δωμ./Διαμ.)"] = new(20, "Ξενοδοχεία 1-2 αστέρων 1,50€ (ανά Δωμ./Διαμ.)", null, true),
        // Code 21: Ξενοδοχεία 3 αστέρων 3,00€ (ανά Δωμ./Διαμ.) (fixed amount)
        // Code 21: Hotels 3 stars 3.00€ (per Room/Apt.) (fixed amount)
        ["Ξενοδοχεία 3 αστέρων 3,00€ (ανά Δωμ./Διαμ.)"] = new(21, "Ξενοδοχεία 3 αστέρων 3,00€ (ανά Δωμ./Διαμ.)", null, true),
        // Code 22: Ξενοδοχεία 4 αστέρων 7,00€ (ανά Δωμ./Διαμ.) (fixed amount)
        // Code 22: Hotels 4 stars 7.00€ (per Room/Apt.) (fixed amount)
        ["Ξενοδοχεία 4 αστέρων 7,00€ (ανά Δωμ./Διαμ.)"] = new(22, "Ξενοδοχεία 4 αστέρων 7,00€ (ανά Δωμ./Διαμ.)", null, true),
        // Code 23: Ξενοδοχεία 5 αστέρων 10,00€ (ανά Δωμ./Διαμ.) (fixed amount)
        // Code 23: Hotels 5 stars 10.00€ (per Room/Apt.) (fixed amount)
        ["Ξενοδοχεία 5 αστέρων 10,00€ (ανά Δωμ./Διαμ.)"] = new(23, "Ξενοδοχεία 5 αστέρων 10,00€ (ανά Δωμ./Διαμ.)", null, true),
        // Code 24: Ενοικιαζόμενα επιπλωμένα δωμάτια – διαμερίσματα 1,50€ (ανά Δωμ./Διαμ.) (fixed amount)
        // Code 24: Rental furnished rooms – apartments 1.50€ (per Room/Apt.) (fixed amount)
        ["Ενοικιαζόμενα επιπλωμένα δωμάτια – διαμερίσματα 1,50€ (ανά Δωμ./Διαμ.)"] = new(24, "Ενοικιαζόμενα επιπλωμένα δωμάτια – διαμερίσματα 1,50€ (ανά Δωμ./Διαμ.)", null, true),
        // Code 25: Ακίνητα βραχυχρόνιας μίσθωσης 1,50€ (fixed amount)
        // Code 25: Short-term rental properties 1.50€ (fixed amount)
        ["Ακίνητα βραχυχρόνιας μίσθωσης 1,50€"] = new(25, "Ακίνητα βραχυχρόνιας μίσθωσης 1,50€", null, true),
        // Code 26: Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 10,00€ (fixed amount)
        // Code 26: Short-term rental houses over 80 sqm 10.00€ (fixed amount)
        ["Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 10,00€"] = new(26, "Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 10,00€", null, true),
        // Code 27: Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 10,00€ (fixed amount)
        // Code 27: Self-service accommodations – tourist furnished villas 10.00€ (fixed amount)
        ["Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 10,00€"] = new(27, "Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 10,00€", null, true),
        // Code 28: Ακίνητα βραχυχρόνιας μίσθωσης 0,50€ (fixed amount)
        // Code 28: Short-term rental properties 0.50€ (fixed amount)
        ["Ακίνητα βραχυχρόνιας μίσθωσης 0,50€"] = new(28, "Ακίνητα βραχυχρόνιας μίσθωσης 0,50€", null, true),
        // Code 29: Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 4,00€ (fixed amount)
        // Code 29: Short-term rental houses over 80 sqm 4.00€ (fixed amount)
        ["Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 4,00€"] = new(29, "Ακίνητα βραχυχρόνιας μίσθωσης μονοκατοικίες άνω των 80 τ.μ. 4,00€", null, true),
        // Code 30: Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 4,00€ (fixed amount)
        // Code 30: Self-service accommodations – tourist furnished villas 4.00€ (fixed amount)
        ["Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 4,00€"] = new(30, "Αυτοεξυπηρετούμενα καταλύματα – τουριστικές επιπλωμένες επαύλεις (βίλες) 4,00€", null, true),
    };

    /// <summary>
    /// Gets withholding tax mapping for a given charge item description.
    /// </summary>
    /// <param name="description">The description of the charge item</param>
    /// <returns>Withholding tax mapping if found, null otherwise</returns>
    public static WithholdingTaxMapping? GetWithholdingTaxMapping(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        // Try exact match first
        if (_withholdingTaxMappings.TryGetValue(description.Trim(), out var exactMapping))
        {
            return exactMapping;
        }

        // Try partial matching for more flexible description matching
        foreach (var kvp in _withholdingTaxMappings)
        {
            var key = kvp.Key;
            var mapping = kvp.Value;

            // Check if the description contains the key or key contains the description
            if (description.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                key.Contains(description, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets fee mapping for a given charge item description.
    /// </summary>
    /// <param name="description">The description of the charge item</param>
    /// <returns>Fee mapping if found, null otherwise</returns>
    public static FeeMapping? GetFeeMapping(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        // Try exact match first
        if (_feeMappings.TryGetValue(description.Trim(), out var exactMapping))
        {
            return exactMapping;
        }

        // Try partial matching for more flexible description matching
        foreach (var kvp in _feeMappings)
        {
            var key = kvp.Key;
            var mapping = kvp.Value;

            // Check if the description contains the key or key contains the description
            if (description.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                key.Contains(description, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets stamp duty mapping for a given charge item description.
    /// </summary>
    /// <param name="description">The description of the charge item</param>
    /// <returns>Stamp duty mapping if found, null otherwise</returns>
    public static StampDutyMapping? GetStampDutyMapping(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        // Try exact match first
        if (_stampDutyMappings.TryGetValue(description.Trim(), out var exactMapping))
        {
            return exactMapping;
        }

        // Try partial matching for more flexible description matching
        foreach (var kvp in _stampDutyMappings)
        {
            var key = kvp.Key;
            var mapping = kvp.Value;

            // Check if the description contains the key or key contains the description
            if (description.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                key.Contains(description, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets other tax mapping for a given charge item description.
    /// </summary>
    /// <param name="description">The description of the charge item</param>
    /// <returns>Other tax mapping if found, null otherwise</returns>
    public static OtherTaxMapping? GetOtherTaxMapping(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        // Try exact match first
        if (_otherTaxMappings.TryGetValue(description.Trim(), out var exactMapping))
        {
            return exactMapping;
        }

        // Try partial matching for more flexible description matching
        foreach (var kvp in _otherTaxMappings)
        {
            var key = kvp.Key;
            var mapping = kvp.Value;

            // Check if the description contains the key or key contains the description
            if (description.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                key.Contains(description, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return null;
    }

    public static bool IsSpecialTaxItem(ChargeItem chargeItem)
    {
        return chargeItem.ftChargeItemCase.IsTypeOfService((ChargeItemCaseTypeOfService) 0xF0);
    }
}