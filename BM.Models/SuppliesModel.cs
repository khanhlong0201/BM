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
        public decimal? QtyIntoInv { get; set; }
        public decimal? QtyOutBound { get; set; }
        public decimal? Price { get; set; }
        public decimal? Qty { get; set; }
        public string? Type { get; set; } //loại vật tư (vật tư bình thường, vật tư khuyến mãi, vật tư loại tê-mực )
        public string? SuppliesTypeCode { get; set; } //Mã nhóm vật tư
        public string? SuppliesTypeName{ get; set; } //Tên nhóm vật tư
        public string? TypeName { get; set; } //tên loại vật tư (vật tư bình thường, vật tư khuyến mãi, vật tư loại tê-mực )

    }
}