using System.ComponentModel.DataAnnotations;

namespace BM.Models
{
    public class InvetoryModel : Auditable
    {
        public string? SuppliesCode { get; set; }
        public string? SuppliesName { get; set; }
        public string? EnumId { get; set; } //đơn vị tính
        public string? EnumName { get; set; }
        public decimal? QtyInv { get; set; }
        public decimal? Price { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

    }
}