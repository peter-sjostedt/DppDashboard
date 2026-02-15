namespace HospitexDPP.Models
{
    public class EnumOption
    {
        public string Value { get; set; } = "";
        public string DisplayName { get; set; } = "";

        public override string ToString() => DisplayName;
    }
}
