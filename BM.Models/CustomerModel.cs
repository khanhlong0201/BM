using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class CustomerModel : Auditable
    {
        public string? CusNo { get; set; }
        [Required(ErrorMessage = "Vui lòng điền Tên khách hàng")]
        public string? FullName { get; set; }
        [Required(ErrorMessage = "Vui lòng điền Số điện thoại 1")]
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? CINo { get; set; } // CCCD
        public string? Email { get; set; }
        public string? FaceBook { get; set; }
        public string? Zalo { get; set; }
        public string? Address { get; set; }
        public string? SkinType { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn Chi nhánh")]
        public string? BranchId { get; set; }
        public string? Remark { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? BranchName { get; set; }
        public double TotalDebtAmount { get; set; }
        public double Point { get; set; } // tích điểm cho KH
    }
}