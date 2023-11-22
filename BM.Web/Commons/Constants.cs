namespace BM.Web.Commons;

public static class DefaultConstants
{
    public static readonly DateTime MIN_DATE = DateTime.Now.AddYears(-1);
    public static readonly DateTime MAX_DATE = DateTime.Now.AddYears(1);
    public const string FORMAT_CURRENCY = "#,###.##";
    public const string FORMAT_GRID_CURRENCY = "{0: #,###.##}";
    public const string FORMAT_DATE = "dd/MM/yyyy";
    public const string FORMAT_GRID_DATE = "{0: dd/MM/yyyy}";
    public const string FORMAT_GRID_DATE_TIME = "{0: HH:mm dd/MM/yyyy}";
    public const string FORMAT_DATE_TIME = "HH:mm dd/MM/yyyy";
    public const int PAGE_SIZE = 1000;
}
