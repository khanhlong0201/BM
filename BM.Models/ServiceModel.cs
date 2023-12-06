using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BM.Models;

/// <summary>
/// dịch vụ
/// </summary>
public class ServiceModel : Auditable
{
    public string? ServiceCode { get; set; }
    [Required(ErrorMessage = "Vui lòng điền Tên dịch vụ")]
    public string? ServiceName { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn Loại dịch vụ")]
    public string? EnumId { get; set; }
    [Range(1000, double.MaxValue, ErrorMessage = "Vui lòng nhập đơn giá")]
    public double Price { get; set; }
    public string? EnumName { get; set; }
    public string? Description { get; set; }
    public double WarrantyPeriod { get; set; } // số tháng bảo thành
    public int QtyWarranty { get; set; } // số lần bảo hành
}

/// <summary>
/// bảng giá
/// </summary>
public class PriceModel : Auditable
{
    public int Id { get; set; }
    public string? ServiceCode { get; set; }
    public bool IsActive { get; set; }
    [Range(1000, double.MaxValue, ErrorMessage = "Vui lòng nhập đơn giá")]
    public double Price { get; set; }
}

/// <summary>
/// Hóa đơn
/// </summary>
public class SalesOrderModel
{
    public int Id { get; set; }
    public int LineNum { get; set; }
    public string ServiceCode { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string ChemicalFormula { get; set; } = ""; // công thức mực
    public double WarrantyPeriod { get; set; }
    public int QtyWarranty { get; set; }
    public double Price { get; set; }
    public double PriceOld { get; set; }
    public int Qty { get; set; }
    public double Amount
    {
        get
        {
            return Qty * Price;
        }
        private set { }
    }

    public List<string>? ListUserAdvise { get; set; } // nhân viên tư vấn
    public List<string>? ListUserImplements { get; set; } // nhân viên thực hiện
}

public class TreatmentRegimenModel : Auditable
{
    public int Id { get; set; }
    public int LineNum { get; set; }
    [Required(ErrorMessage = "Vui lòng điền bước thực hiện")]
    public string? Name { get; set; }
    [Required(ErrorMessage = "Vui lòng điền nội dung thực hiện")]
    public string? Title { get; set; }
    public string? ServiceCode { get; set; }
}
