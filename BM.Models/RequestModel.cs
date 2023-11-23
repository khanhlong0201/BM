using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BM.Models;
public class RequestModel
{
    public int UserId { get; set; }
    public string? Json { get; set; }
    public string? Type { get; set; }
}

public class ResponseModel
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public ResponseModel()
    {
        StatusCode = -1;
        Message = string.Empty;
    }
    public ResponseModel(int status, string? message)
    {
        StatusCode = status;
        Message = message;
    }
}

public enum EnumType
{
    @Add,
    @Update, 
    @Delete,
    @SaveAndClose,
    @SaveAndCreate,
    @Report
}
