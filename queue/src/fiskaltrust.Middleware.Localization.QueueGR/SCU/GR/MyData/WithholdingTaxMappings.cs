using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

/// <summary>
/// Provides mapping functionality for Greek withholding taxes based on AADE requirements.
/// Maps charge item descriptions to withholding tax categories and percentages.
/// </summary>
public static class WithholdingTaxMappings
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
    /// Dictionary mapping Greek descriptions to withholding tax information.
    /// Based on the official AADE withholding tax table.
    /// </summary>
    private static readonly Dictionary<string, WithholdingTaxMapping> _withholdingTaxMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Code 1: Περιπτ. β’- Τόκοι - 15%
        ["Περιπτ. β’- Τόκοι - 15%"] = new(1, "Περιπτ. β’- Τόκοι - 15%", 15m),
        // Code 2: Περιπτ. γ’ - Δικαιώματα - 20%
        ["Περιπτ. γ’ - Δικαιώματα - 20%"] = new(2, "Περιπτ. γ’ - Δικαιώματα - 20%", 20m),
        // Code 3: Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%
        ["Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%"] = new(3, "Περιπτ. δ' - Αμοιβές Συμβουλών Διοίκησης - 20%", 20m),
        // Code 4: Περιπτ. δ' - Τεχνικά Έργα - 3%
        ["Περιπτ. δ' - Τεχνικά Έργα - 3%"] = new(4, "Περιπτ. δ' - Τεχνικά Έργα - 3%", 3m),
        // Code 5: Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%
        ["Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%"] = new(5, "Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%", 1m),
        // Code 6: Λοιπά Αγαθά 4%
        ["Λοιπά Αγαθά 4%"] = new(6, "Λοιπά Αγαθά 4%", 4m),
        // Code 7: Παροχή Υπηρεσιών 8%
        ["Παροχή Υπηρεσιών 8%"] = new(7, "Παροχή Υπηρεσιών 8%", 8m),
        // Code 8: Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%
        ["Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%"] = new(8, "Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%", 4m),
        // Code 9: Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%
        ["Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%"] = new(9, "Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%", 10m),
        // Code 10: Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%
        ["Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%"] = new(10, "Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%", 15m),
        // Code 11: Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013 (fixed amount)
        ["Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013"] = new(11, "Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 1 αρ. 15 ν. 4172/2013", null, true),
        // Code 12: Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού 15%
        ["Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού"] = new(12, "Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Αξιωματικών Εμπορικού Ναυτικού", 15m),
        // Code 13: Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού 10%
        ["Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού"] = new(13, "Παρακράτηση Φόρου Μισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 - Κατώτερο Πλήρωμα Εμπορικού Ναυτικού", 10m),
        // Code 14: Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης (fixed amount)
        ["Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης"] = new(14, "Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης", null, true),
        // Code 15: Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013 (fixed amount)
        ["Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013"] = new(15, "Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας παρ. 3 αρ. 15 ν. 4172/2013", null, true),
        // Code 16: Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.) (fixed amount)
        ["Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)"] = new(16, "Παρακρατήσεις συναλλαγών αλλοδαπής βάσει συμβάσεων αποφυγής διπλής φορολογίας (Σ.Α.Δ.Φ.)", null, true),
        // Code 17: Λοιπές Παρακρατήσεις Φόρου (fixed amount)
        ["Λοιπές Παρακρατήσεις Φόρου"] = new(17, "Λοιπές Παρακρατήσεις Φόρου", null, true),
        // Code 18: Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013 5%
        ["Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013"] = new(18, "Παρακράτηση Φόρου Μερίσματα περ.α παρ. 1 αρ. 64 ν. 4172/2013", 5m),
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
    /// Calculates the withholding tax amount based on the net amount and mapping.
    /// </summary>
    /// <param name="netAmount">The net amount to calculate tax on</param>
    /// <param name="mapping">The withholding tax mapping</param>
    /// <returns>The calculated withholding tax amount</returns>
    public static decimal CalculateWithholdingTaxAmount(decimal netAmount, WithholdingTaxMapping mapping)
    {
        if (mapping.IsFixedAmount || !mapping.Percentage.HasValue)
        {
            // For fixed amounts, the amount should be specified in the charge item amount
            // We return 0 here as the actual amount is handled separately
            return 0m;
        }

        return Math.Round(netAmount * (mapping.Percentage.Value / 100m), 2);
    }

    public static bool IsSpecialTaxItem(ChargeItem chargeItem)
    {
        var typeOfService = ((long)chargeItem.ftChargeItemCase >> 4) & 0xF;
        return typeOfService == 0xF;
    }

    public static IReadOnlyDictionary<string, WithholdingTaxMapping> GetAllMappings()
    {
        return _withholdingTaxMappings.AsReadOnly();
    }
}