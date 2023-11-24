using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class EnumModel : Auditable
    {
        public string? EnumId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn Loại danh mục")]
        public string? EnumType { get; set; }
        [Required(ErrorMessage = "Vui lòng điền Tên danh mục")]
        public string? EnumName { get; set; }
        public string? Description { get; set; }
    }
}