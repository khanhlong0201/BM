using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BM.Models;
public class ReportModel : Auditable
{
    public string? CusNo { get; set; }
    public double TotalReveune { get; set; }
    public string? FullName { get; set; }
    public string? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int Qty { get; set; }
    public double Price { get; set; }
    public string? ConsultUserId { get; set; } // mã nhân viên tư vấn
    public string? ImplementUserId { get; set; } // mã mã nhân viên thực hiện
    public string? ConsultUserName { get; set; } // Tên nhân viên tư vấn
    public string? ImplementUserName { get; set; } // Tên mã nhân viên thực hiện
    public string? ServiceCode { get; set; }
    public string? ServiceName { get; set; }
    public string? EnumId { get; set; }
    public string? EnumName { get; set; }
    public double Total_01 { get; set; }
    public double Total_02 { get; set; }
    public double Total_03 { get; set; }
    public double Total_04 { get; set; }
    public double Total_05 { get; set; }
    public double Total_06 { get; set; }
    public double Total_07 { get; set; }
    public double Total_08 { get; set; }
    public double Total_09 { get; set; }
    public double Total_10 { get; set; }
    public double Total_11 { get; set; }
    public double Total_12{ get; set; }
    public double LineTotal { get; set; }
}

public class RequestReportModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? BranchId { get; set; }
    public string? TypeTime { get; set; } // quí or tháng
    public string? Type { get; set; } // loại báo cáo ( doanh thu theo ldv hay doanh thu theo dv)
    public int UserId { get; set; }
}


