using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class InvetoryModel : Auditable
    {
        public int? Absid { get; set; }
        public string? SuppliesCode { get; set; }
        public string? SuppliesName { get; set; }
        public string? EnumId { get; set; } //đơn vị tính
        public string? EnumName { get; set; }
        public decimal? QtyInv { get; set; }
        public decimal? Price { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? SuppliesTypeCode { get; set; } //Mã nhóm vật tư
        public string? SuppliesTypeName { get; set; } //Tên nhóm vật tư
        public string? Type { get; set; } //Mã loại vật tư (vật tư bình thường, vật tư khuyến mãi, vật tư loại tê-mực )
        public string? TypeName { get; set; } //tên loại vật tư (vật tư bình thường, vật tư khuyến mãi, vật tư loại tê-mực )
        public decimal Total { get; set; } // tổng tiền
    }
}