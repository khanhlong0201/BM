using System.ComponentModel.DataAnnotations;

namespace BM.Models;

public class OutBoundModel : Auditable
{
    public int? DocEntry { get; set; }
    public string? VoucherNo { get; set; }
    public int? BaseEntry { get; set; }
    public int? IdDraftDetail { get; set; }
    public string? ColorImplement { get; set; }
    public string? SuppliesQtyList { get; set; }
    public string? AnesthesiaType { get; set; }
    public int? AnesthesiaQty { get; set; }
    public string? DarkTestColor { get; set; }
    public string? CoadingColor { get; set; }
    public string? LibColor { get; set; }
    public DateTime? StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set; }
    public string? Problems { get; set; }
    public string? ChargeUser { get; set; }
    public string? BranchId { get; set; }
}
