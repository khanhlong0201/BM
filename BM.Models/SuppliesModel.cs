using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class SuppliesModel
    {
        [Required(ErrorMessage = "Vui lòng điền Mã vật tư")]
        public string? SuppliesCode { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Tên vật tư")]
        public string? SuppliesName { get; set; }


        [Required(ErrorMessage = "Vui lòng điền Đơn vị tính")]
        public string? EnumId { get; set; }
        public string? EnumName { get; set; }
        public DateTime? DateCreate { get; set; }
        public int? UserCreate { get; set; }
        public DateTime? DateUpdate { get; set; }
        public int? UserUpdate { get; set; }
    }
}