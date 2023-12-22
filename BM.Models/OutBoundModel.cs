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
    public int? AnesthesiaCount { get; set; }
    public string? DarkTestColor { get; set; }
    public string? CoadingColor { get; set; }
    public string? LibColor { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Problems { get; set; }
    public string? ChargeUser { get; set; }
    public string? ChargeUserName { get; set; }
    public string? BranchId { get; set; }
    public string ServiceCode { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string ChemicalFormula { get; set; } = ""; // công thức mực
    public List<string>? ListUserImplements { get; set; } // danh sách nhân viên thực hiện
    public string? BranchName { get; set; }
    public string? FullName { get; set; } // tên khách hàng
    public string? CusNo { get; set; } // mã khách hàng
    public string? Remark { get; set; } // đặc điểm khách hàng
    public string? HealthStatus { get; set; } //tình trạng đặc điểm khách hàng
    public string?  ImplementUserId { get; set; } // nhân viên thực hiện
    public string? VoucherNoDraft { get; set; }// số chứng từ 
    public List<string>? ListChargeUser { get; set; } // danh sách nhân viên phụ trách
    public string? UserNameCreate { get; set; }
    public string? Type { get; set; } // kiểu xuất kho ( theo dịch vụ, xuất kho bất kỳ thời điểm nào)
    public string? TypeName { get; set; } //Tên kiểu xuất kho ( theo dịch vụ, xuất kho bất kỳ thời điểm nào)
}
