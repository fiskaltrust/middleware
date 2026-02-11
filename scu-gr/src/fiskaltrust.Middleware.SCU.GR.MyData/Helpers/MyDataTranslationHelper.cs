using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.GR.MyData.Helpers
{
    /// <summary>
    /// Class responsible for sanitizing data and translating codes from the myDATA specification into human-readable descriptions in Greek.
    /// </summary>
    public static class MyDataTranslationHelper
    {

        /// <summary>
        /// Formats an address for display
        /// </summary>
        public static string FormatAddress(AddressType address)
        {
            if (address == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address.street))
            {
                var streetPart = address.street.Trim();
                if (!string.IsNullOrWhiteSpace(address.number) && address.number != "0")
                {
                    streetPart += $" {address.number}";
                }
                parts.Add(streetPart);
            }

            var cityPart = new List<string>();
            if (!string.IsNullOrWhiteSpace(address.postalCode))
            {
                cityPart.Add(address.postalCode.Trim());
            }
            if (!string.IsNullOrWhiteSpace(address.city))
            {
                cityPart.Add(address.city.Trim());
            }
            if (cityPart.Count > 0)
            {
                parts.Add(string.Join(" ", cityPart));
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Translates the move purpose code to its Greek description.
        /// Based on Greek myDATA specification.
        /// </summary>
        public static string GetMovePurposeDescription(int movePurpose)
        {
            return movePurpose switch
            {
                1 => "Πώληση",                                      // Sale
                2 => "Πώληση για λογαριασμό τρίτων",               // Sale on behalf of third parties
                3 => "Δειγματισμός",                               // Sampling
                4 => "Έκθεση",                                      // Exhibition
                5 => "Επιστροφή",                                   // Return
                6 => "Φύλαξη",                                      // Safekeeping
                7 => "Επεξεργασία",                                // Processing
                8 => "Εσωτερική διακίνηση",                        // Internal movement
                9 => "Καταστροφή",                                  // Destruction
                10 => "Αποστολή για λογαριασμό τρίτων",            // Dispatch on behalf of third parties
                11 => "Μετασκευή",                                 // Transformation
                12 => "Εξαγωγή",                                    // Export
                13 => "Εισαγωγή",                                   // Import
                14 => "Πιστωτικό υποκατάστημα",                    // Credit establishment
                15 => "Κέντρο διανομής",                           // Distribution center
                16 => "Άλλο",                                       // Other
                _ => movePurpose.ToString()                        // Unknown code - return as-is
            };
        }
    }
}
