namespace BM.Models
{
    public class CustomerModel : Auditable
    {
        public string? CusNo { get; set; }
        public string? FullName { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? CINo { get; set; } // CCCD
        public string? Email { get; set; }
        public string? FaceBook { get; set; }
        public string? Zalo { get; set; }
        public string? Address { get; set; }
        public string? SkinType { get; set; }
        public string? BranchId { get; set; }
        public string? Remark { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}