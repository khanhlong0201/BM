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
    public const string FORMAT_TIME = "HH:mm";
    public const int PAGE_SIZE = 100;

    public const string MESSAGE_INVALID_DATA = "Không đúng định dạng dữ liệu!";
    public const string MESSAGE_LOGIN_EXPIRED = "Hết phiên đăng nhập!";
    public const string MESSAGE_INSERT = "Đã tạo mới";
    public const string MESSAGE_UPDATE = "Đã cập nhât";
    public const string MESSAGE_DELETE = "Đã xóa các dòng được chọn!";
    public const string MESSAGE_NO_CHOSE_DATA = "Không có dòng nào được chọn!";
    public const string MESSAGE_CONFIRM_DELETE = "Bạn có chắc muốn xóa các dòng được chọn?";
    public const string MESSAGE_NO_DATA = "Không tìm thấy dữ liệu. Vui lòng thử lại!";
}


public static class EndpointConstants
{
    public const string URL_MASTERDATA_GETBRANCH = "MasterData/GetBranchs";
    public const string URL_MASTERDATA_UPDATE_BRANCH = "MasterData/UpdateBranch";

    public const string URL_MASTERDATA_GET_USER = "MasterData/GetUsers";
    public const string URL_MASTERDATA_UPDATE_USER = "MasterData/UpdateUser";

    public const string URL_MASTERDATA_GET_ENUM = "MasterData/GetEnumsByType";
    public const string URL_MASTERDATA_UPDATE_ENUM = "MasterData/UpdateEnum";

    public const string URL_MASTERDATA_GET_CUSTOMER = "MasterData/GetCustomers";
    public const string URL_MASTERDATA_GET_CUSTOMER_BY_ID = "MasterData/GetCustomerById";
    public const string URL_MASTERDATA_UPDATE_CUSTOMER = "MasterData/UpdateCustomer";

    public const string URL_MASTERDATA_GET_SERVICE = "MasterData/GetServices";
    public const string URL_MASTERDATA_UPDATE_SERVICE = "MasterData/UpdateService";
    public const string URL_MASTERDATA_GET_PRICE_BY_SERVICE = "MasterData/GetPricesByService";
    public const string URL_MASTERDATA_UPDATE_PRICE = "MasterData/UpdatePrice";
    public const string URL_MASTERDATA_GET_TREATMENT_BY_SERVICE = "MasterData/GetTreatmentByService";
    public const string URL_MASTERDATA_UPDATE_TREATMENT = "MasterData/UpdateTreatment";

    public const string URL_MASTERDATA_USER_LOGIN = "MasterData/Login";
    public const string URL_MASTERDATA_DELETE = "MasterData/DeleteData";
    public const string URL_DOCUMENT_UPDATE_SALES_ORDER = "Document/UpdateSalesOrder";
    public const string URL_DOCUMENT_UPDATE_SERVICE_CALL = "Document/UpdateServiceCall";

    public const string URL_MASTERDATA_GET_SUPPLIES = "MasterData/GetSupplies";
    public const string URL_MASTERDATA_UPDATE_SUPPLIES = "MasterData/UpdateSupplies";
    public const string URL_MASTERDATA_GET_SUPPLIES_OUTBOUND = "MasterData/GetSuppliesOutBound";

    public const string URL_MASTERDATA_GET_INVETORY = "MasterData/GetInventory";
    public const string URL_MASTERDATA_UPDATE_INVETORY = "MasterData/UpdateInventory";

    public const string URL_DOCUMENT_GET_SALES_ORDER = "Document/GetDocList";
    public const string URL_DOCUMENT_GET_DOC_BY_ID = "Document/GetDocById";
    public const string URL_DOCUMENT_GET_DOC_BY_CUSNO = "Document/GetDocClosedByGuest";
    public const string URL_DOCUMENT_CANCLE_DOC_LIST = "Document/CancleDocList";
    public const string URL_DOCUMENT_REMINDER_BY_MONTH = "Document/ReminderByMonth";
    public const string URL_DOCUMENT_CUSTOMER_DEBTS_BY_DOC = "Document/GetCustomerDebtsByDoc";
    public const string URL_DOCUMENT_UPDATE_CUSTOMER_DEBTS = "Document/UpdateCustomerDebts";
    public const string URL_DOCUMENT_REVENUE_REPORT = "Document/GetRevenueReport";

    public const string URL_DOCUMENT_REPORT = "Document/GetReport";

    public const string URL_DOCUMENT_UPDATE_OUTBOUND = "Document/UpdateOutBound";
    public const string URL_DOCUMENT_GET_OUTBOUND = "Document/GetOutBound";
    public const string URL_DOCUMENT_CANCLE_OUTBOUND_LIST = "Document/CancleOutBoundList";


}