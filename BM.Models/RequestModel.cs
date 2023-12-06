namespace BM.Models;

public class RequestModel
{
    public int UserId { get; set; }
    public string? Json { get; set; }
    public string? Type { get; set; }
    public string? JsonDetail { get; set; }
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

public class ComboboxModel
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool IsCheck{ get; set; }
}

public class SearchModel
{
    public int UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? StatusId { get; set; }
    public bool IsAdmin { get; set; }
}

public enum EnumType
{
    @Add,
    @Update,
    @Delete,
    @SaveAndClose,
    @SaveAndCreate,
    @Report,
    @ServiceType,
    @SkinType,
    @ServicePack,
    @Unit,
    @StateOfHealth
}

public enum EnumTable
{
    @Enums,
    @Services,
    @Drafts,
    @DraftDetails,
    @Users,
    @Customers,
    @Prices
}

public enum DocStatus
{
    @Pending,
    @Closed,
    @All
}