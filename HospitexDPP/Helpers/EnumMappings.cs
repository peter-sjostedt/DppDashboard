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
