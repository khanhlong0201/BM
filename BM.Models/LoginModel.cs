namespace BM.Models;

public class LoginRequestModel
{
    public string UserName { get; set; }
    public string? Password { get; set; }
    public string? BranchId { get; set; }
}
