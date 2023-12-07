using System.ComponentModel.DataAnnotations;

namespace BM.Models;

public class UserModel : Auditable
{
    public int Id { get; set; }
    public string? EmpNo { get; set; }

    [Required(ErrorMessage = "Vui lòng điền Tên tài khoản")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Vui lòng điền Mật khẩu")]
    public string? Password { get; set; }

    public string? LastPassword { get; set; }

    [Required(ErrorMessage = "Vui lòng điền Tên nhân viên")]
    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Chi nhánh")]
    public string? BranchId { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfWork { get; set; }
    public bool IsAdmin { get; set; }

    [Required(ErrorMessage = "Vui lòng điền nhập lại Mật khẩu")]
    public string? ReEnterPassword { get; set; }
}