using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BM.Models;
public class DocumentModel : Auditable
{
    public int DocEntry { get; set; }
    public string? CusNo { get; set; }
    public string? DiscountCode { get; set; }
    public double Total { get; set; }
    public double GuestsPay { get; set; }
    // khách nợ => nế khách trả nhỏ hơn tổng -> nợ cộng dồn vô công nợ của khách
    public double Debt { get; set; } 
    public string? StatusBefore { get; set; }
    public string? HealthStatus { get; set; }
    public string? NoteForAll { get; set; }
    public string? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? VoucherNo { get; set; }
    public int BaseEntry { get; set; }

    #region Thông tin khách hàng
    public string? FullName { get; set; }
    public string? Phone1 { get; set; }
    public string? CINo { get; set; } // CCCD
    public string? Email { get; set; }
    public string? FaceBook { get; set; }
    public string? Zalo { get; set; }
    public string? Address { get; set; }
    public string? SkinType { get; set; }
    public string? BranchId { get; set; }
    public string? Remark { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? BranchName { get; set; }
    public string? Service { get; set; }
    #endregion
}

public class DocumentDetailModel : Auditable
{
    public int Id { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceName { get; set; }
    public double Price { get; set; }
    public double PriceOld { get; set; }
    public int Qty { get; set; }
    public double LineTotal { get; set; }
    public int DocEntry { get; set; }
    public string? ActionType { get; set; }
    public string? ConsultUserId { get; set; } // ds mã nhân viên tư vấn
    public string? ImplementUserId { get; set; } // ds mã nhân viên thực hiện
    public string? ChemicalFormula { get; set; } // công thức mực
    public double WarrantyPeriod { get; set; }
    public int QtyWarranty { get; set; }
}

public class CustomerDebtsModel
{
    public int Id { get; set; }
    public int DocEntry { get; set; }
    public string? CusNo { get; set; }
    public string? FullName { get; set; }
    public double GuestsPay { get; set; }
    public double TotalDebtAmount { get; set; }
    public DateTime? DateCreate { get; set; }
    public int? UserCreate { get; set; }
    public string? Remark { get; set; }
    public bool IsDelay { get; set; } // trĩ hoãn lại
    public DateTime? DateDelay { get; set; } // ngày delay lại
}

