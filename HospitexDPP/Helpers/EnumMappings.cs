using HospitexDPP.Models;
using HospitexDPP.Resources;

namespace HospitexDPP.Helpers
{
    public static class EnumMappings
    {
        private static readonly Dictionary<string, List<(string DbValue, string Key)>> Groups = new()
        {
            ["Category"] =
            [
                ("clothing", "Category_Clothing"),
                ("accessories", "Category_Accessories"),
                ("footwear", "Category_Footwear"),
                ("other", "Category_Other"),
            ],
            ["ProductGroup"] =
            [
                ("Top", "ProductGroup_Top"),
                ("Bottom", "ProductGroup_Bottom"),
            ],
            ["LineConcept"] =
            [
                ("Active Wear", "LineConcept_ActiveWear"),
                ("Maternity", "LineConcept_Maternity"),
                ("Protective Wear", "LineConcept_ProtectiveWear"),
                ("Sleep Wear", "LineConcept_SleepWear"),
                ("Healthcare", "LineConcept_Healthcare"),
            ],
            ["TypeItem"] =
            [
                ("Jacket", "TypeItem_Jacket"),
                ("Pants", "TypeItem_Pants"),
                ("Blouse", "TypeItem_Blouse"),
                ("Sweater", "TypeItem_Sweater"),
                ("Tunic", "TypeItem_Tunic"),
                ("Lab coat", "TypeItem_LabCoat"),
                ("Patient gown", "TypeItem_PatientGown"),
            ],
            ["Gender"] =
            [
                ("Male", "Gender_Male"),
                ("Female", "Gender_Female"),
                ("Unisex", "Gender_Unisex"),
            ],
            ["Market"] =
            [
                ("mass-market", "Market_MassMarket"),
                ("mid-price", "Market_MidPrice"),
                ("premium", "Market_Premium"),
                ("luxury", "Market_Luxury"),
            ],
            ["Water"] =
            [
                ("No Water properties", "Water_None"),
                ("Waterproof", "Water_Waterproof"),
                ("Water Repellent", "Water_Repellent"),
                ("Water Resistant", "Water_Resistant"),
            ],
            ["Season"] =
            [
                ("SP", "Season_SP"),
                ("SU", "Season_SU"),
            ],
            ["Component"] =
            [
                ("Body fabric", "Component_BodyFabric"),
                ("Trim", "Component_Trim"),
                ("Lining fabric", "Component_Lining"),
            ],
            ["Material"] =
            [
                ("Textile", "Material_Textile"),
                ("Leather", "Material_Leather"),
                ("Rubber", "Material_Rubber"),
            ],
            ["CircularStrategy"] =
            [
                ("Material Cyclability", "CircularStrategy_MaterialCyclability"),
                ("Mono Cycle", "CircularStrategy_MonoCycle"),
                ("Disassembly", "CircularStrategy_Disassembly"),
                ("Longevity", "CircularStrategy_Longevity"),
                ("Physical Durability", "CircularStrategy_PhysicalDurability"),
            ],
            ["DataCarrier"] =
            [
                ("RFID thread", "DataCarrier_RFID"),
                ("NFC chip", "DataCarrier_NFC"),
                ("QR code", "DataCarrier_QR"),
            ],
        };

        /// <summary>
        /// Translate a DB enum value to a localized display string.
        /// Returns the raw value unchanged if no mapping is found (Category 3 values).
        /// </summary>
        public static string Localize(string prefix, string dbValue)
        {
            if (string.IsNullOrEmpty(dbValue)) return "";

            if (Groups.TryGetValue(prefix, out var items))
            {
                var entry = items.FirstOrDefault(e =>
                    e.DbValue.Equals(dbValue, StringComparison.OrdinalIgnoreCase));
                if (entry.Key != null)
                    return Strings.ResourceManager.GetString(entry.Key, Strings.Culture) ?? dbValue;
            }

            return dbValue;
        }

        /// <summary>
        /// ISO 3166-1 alpha-2 country codes for ComboBox selection.
        /// Sorted alphabetically by display name ("CODE – Name").
        /// </summary>
        public static List<EnumOption> GetCountryOptions()
        {
            var countries = new (string Code, string Name)[]
            {
                ("AR", "Argentina"), ("AT", "Austria"), ("BD", "Bangladesh"),
                ("BE", "Belgium"), ("BR", "Brazil"), ("BG", "Bulgaria"),
                ("KH", "Cambodia"), ("CL", "Chile"), ("CN", "China"),
                ("CO", "Colombia"), ("HR", "Croatia"), ("CZ", "Czech Republic"),
                ("DK", "Denmark"), ("EG", "Egypt"), ("EE", "Estonia"),
                ("ET", "Ethiopia"), ("FI", "Finland"), ("FR", "France"),
                ("DE", "Germany"), ("GR", "Greece"), ("HU", "Hungary"),
                ("IN", "India"), ("ID", "Indonesia"), ("IE", "Ireland"),
                ("IT", "Italy"), ("JP", "Japan"), ("KE", "Kenya"),
                ("KR", "South Korea"), ("LV", "Latvia"), ("LT", "Lithuania"),
                ("MY", "Malaysia"), ("MX", "Mexico"), ("MA", "Morocco"),
                ("MM", "Myanmar"), ("NL", "Netherlands"), ("NO", "Norway"),
                ("PK", "Pakistan"), ("PE", "Peru"), ("PH", "Philippines"),
                ("PL", "Poland"), ("PT", "Portugal"), ("RO", "Romania"),
                ("SK", "Slovakia"), ("SI", "Slovenia"), ("ZA", "South Africa"),
                ("ES", "Spain"), ("LK", "Sri Lanka"), ("SE", "Sweden"),
                ("CH", "Switzerland"), ("TW", "Taiwan"), ("TH", "Thailand"),
                ("TN", "Tunisia"), ("TR", "Turkey"), ("GB", "United Kingdom"),
                ("US", "United States"), ("VN", "Vietnam"),
            };

            return countries
                .OrderBy(c => c.Name)
                .Select(c => new EnumOption
                {
                    Value = c.Code,
                    DisplayName = $"{c.Code} – {c.Name}"
                })
                .ToList();
        }

        /// <summary>
        /// Generate ComboBox option list for a given enum group.
        /// </summary>
        public static List<EnumOption> GetOptions(string prefix)
        {
            if (!Groups.TryGetValue(prefix, out var items)) return [];

            return items.Select(e => new EnumOption
            {
                Value = e.DbValue,
                DisplayName = Strings.ResourceManager.GetString(e.Key, Strings.Culture) ?? e.DbValue
            }).ToList();
        }
    }
}
