using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class BranchModel
    {
        public string? BranchId { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Tên chi nhánh")]
        public string? BranchName { get; set; }

        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Địa chỉ")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Số điện thoại")]
        public string? PhoneNumber { get; set; }

        public DateTime? DateCreate { get; set; }
        public int? UserCreate { get; set; }
        public DateTime? DateUpdate { get; set; }
        public int? UserUpdate { get; set; }
    }
}