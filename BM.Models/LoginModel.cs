using System.ComponentModel.DataAnnotations;

namespace BM.Models;

public class LoginRequestModel
{
    [Required(ErrorMessage = "Vui lòng điền Tên đăng nhập")]
    public string UserName { get; set; }
    [Required(ErrorMessage = "Vui lòng điền mật khẩu")]
    public string? Password { get; set; }
    public string? BranchId { get; set; }
}
