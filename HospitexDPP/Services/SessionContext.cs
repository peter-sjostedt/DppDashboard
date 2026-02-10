namespace HospitexDPP.Services
{
    public class SessionContext
    {
        public string? AdminKey { get; set; }
        public string? BrandKey { get; set; }
        public string? SupplierKey { get; set; }

        public string? BrandName { get; set; }
        public string? SupplierName { get; set; }
        public int? BrandId { get; set; }
        public int? SupplierId { get; set; }

        public bool IsAdmin => AdminKey != null;
        public bool IsBrand => BrandKey != null;
        public bool IsSupplier => SupplierKey != null;
        public bool HasMultipleRoles => new[] { IsAdmin, IsBrand, IsSupplier }.Count(x => x) > 1;
    }
}
