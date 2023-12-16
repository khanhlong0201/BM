using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class SuppliesModel: Auditable
    {
        public string? SuppliesCode { get; set; }

        [Required(ErrorMessage = "Vui lòng điền Tên vật tư")]
        public string? SuppliesName { get; set; }


        [Required(ErrorMessage = "Vui lòng điền Đơn vị tính")]
        public string? EnumId { get; set; }
        public string? EnumName { get; set; }
        public decimal? QtyInv { get; set; }
        public decimal? Price { get; set; }
        public decimal? Qty { get; set; }
    }
}