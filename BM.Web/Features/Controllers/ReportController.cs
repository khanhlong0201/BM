using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Telerik.Blazor.Components;
namespace BM.Web.Features.Controllers
{
    public class ReportController : BMControllerBase, IDisposable
    {
        #region Dependency Injection
        [Inject] private ILogger<SalesDocListController>? _logger { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<ReportModel>? ListReports { get; set; }
        public IEnumerable<ReportModel>? SelectedReports { get; set; } = new List<ReportModel>();
        public List<ComboboxModel>? ListTypeTime{ get; set; }
        public RequestReportModel ItemFilter = new RequestReportModel();
        public TelerikGrid<ReportModel> Grid;
        public List<BranchModel>? ListBranchs { get; set; }
        public List<ComboboxModel>? ListTypeReports { get; set; }
        public string pReportType = "";
        public string pReportTypeName = "";
        private string currentLocation = "";
        #endregion

        #region Override Functions
        /// <summary>
        /// chọn quí tháng
        /// </summary>
        /// <param name="supplies"></param>
        /// <param name="invetory"></param>
        public async void SelectedTypeTimeChanged(string typeTime)
        {
            try
            {
                if (typeTime == null) return;
                ItemFilter.TypeTime = typeTime;
                await getDataReports();
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                ListTypeTime = new List<ComboboxModel>()
                        {
                            new ComboboxModel() {Code = nameof(TypeTime.Qui), Name = "Quí"},
                            new ComboboxModel() {Code = nameof(TypeTime.Thang), Name = "Tháng"},
                        };
                ListTypeReports = new List<ComboboxModel>()
                {

                    new ComboboxModel() {Code = nameof(ReportType.@DoanhThuQuiThangTheoDichVu), Name = "Doanh thu dịch vụ theo quí - tháng"},
                    new ComboboxModel() {Code = nameof(ReportType.@DoanhThuQuiThangTheoLoaiDichVu), Name = "Doanh thu loại dịch vụ theo quí - tháng"},
                    new ComboboxModel() {Code = nameof(ReportType.@DoanhThuTheoDichVu), Name = "Doanh thu theo dịch vụ từ ngày - đến ngày"},
                    new ComboboxModel() {Code = nameof(ReportType.@DoanhThuTheoLoaiDichVu), Name = "Doanh thu theo loại dịch vụ từ ngày - đến ngày"},
                    new ComboboxModel() {Code = nameof(ReportType.@DoanhThuQuiThangTheoNhanVienTuVan), Name = "Doanh thu nhân viên tư vấn theo quí - tháng"},
                    new ComboboxModel() {Code = nameof(ReportType.@DoanhThuQuiThangTheoNhanVienThucHien), Name = "Doanh thu nhân viên thực hiện theo quí - tháng"}
                };
                pReportType = nameof(ReportType.@DoanhThuQuiThangTheoDichVu);
                var uri = _navManager!.ToAbsoluteUri(_navManager.Uri);
                switch (uri.AbsolutePath?.ToUpper())
                {
                    case "/REPORT-REVENUE-MONTH-QUARTER-SERVICE":
                        pReportType = nameof(ReportType.@DoanhThuQuiThangTheoDichVu);
                        break;
                    case "/REPORT-REVENUE-MONTH-QUARTER-SERVICE-TYPE":
                        pReportType = nameof(ReportType.@DoanhThuQuiThangTheoLoaiDichVu);
                        break;
                    case "/REPORT-REVENUE-SERVICE":
                        pReportType = nameof(ReportType.@DoanhThuTheoDichVu);
                        break;
                    case "/REPORT-REVENUE-SERVICE-TYPE":
                        pReportType = nameof(ReportType.@DoanhThuTheoLoaiDichVu);
                        break;
                    case "/REPORT-REVENUE-MONTH-QUARTER-CONSULT-USER":
                        pReportType = nameof(ReportType.@DoanhThuQuiThangTheoNhanVienTuVan);
                        break;
                    case "/REPORT-REVENUE-MONTH-QUARTER-IMPLEMENT-USER":
                        pReportType = nameof(ReportType.@DoanhThuQuiThangTheoNhanVienThucHien);
                        break;
                }
                pReportTypeName = ListTypeReports.FirstOrDefault(m => m.Code == pReportType)?.Name + "";
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Danh mục" },
                    new BreadcrumbModel() { Text =  pReportTypeName}
                };
                ItemFilter.Type = pReportType; // gán loại báo cáo
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                _navManager.LocationChanged += LocationChanged;

            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "OnInitializedAsync");
            }
        }


        //protected override async Task OnInitializedAsync()
        //{
        //    try
        //    {
        //        await base.OnInitializedAsync();
        //        ListBreadcrumbs = new List<BreadcrumbModel>
        //        {
        //            new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
        //            new BreadcrumbModel() { Text = "Báo cáo" },
        //            new BreadcrumbModel() { Text = "Doanh thu quí - tháng theo dịch vụ" },
        //        };
        //        await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
        //        ListTypeTime = new List<ComboboxModel>()
        //        {
        //            new ComboboxModel() {Code = nameof(TypeTime.Qui), Name = "Quí"},
        //            new ComboboxModel() {Code = nameof(TypeTime.Thang), Name = "Tháng"},
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger!.LogError(ex, "OnInitializedAsync");
        //    }
        //}

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                try
                {
                    ItemFilter.TypeTime = nameof(TypeTime.Thang);
                    ItemFilter.FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);// ngày đầu tháng
                    ItemFilter.ToDate = _dateTimeService!.GetCurrentVietnamTime(); //ngày hiện tại
                    ItemFilter.TypeTime = nameof(TypeTime.Thang);
                    // đọc giá tri câu query
                    var uri = _navManager?.ToAbsoluteUri(_navManager.Uri);
                    if (uri != null && QueryHelpers.ParseQuery(uri.Query).Count > 0)
                    {
                        string key = uri.Query.Substring(5); // để tránh parse lỗi;    
                        //Dictionary<string, string> pParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(EncryptHelper.Decrypt(key));
                        //if (pParams != null && pParams.Any() && pParams.ContainsKey("pStatusId")) ItemFilter.StatusId = pParams["pStatusId"];
                    }
                    //
                    await _progressService!.SetPercent(0.4);
                    await getDataReports();
                    ListBranchs = await _masterDataService!.GetDataBranchsAsync();
                    Grid?.Rebind();
                }
                catch (Exception ex)
                {
                    _logger!.LogError(ex, "OnAfterRenderAsync");
                    ShowError(ex.Message);
                }
                finally
                {
                    await _progressService!.Done();
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        #endregion

        #region Private Functions
        void IDisposable.Dispose()
        {
            // Unsubscribe from the event when our component is disposed
            _navManager!.LocationChanged -= LocationChanged;
        }
        async Task setDataBreadCrumChanged(string location = "")
        {
            switch (location.ToUpper())
            {
                case "/REPORT-REVENUE-MONTH-QUARTER-SERVICE":
                    pReportType = nameof(ReportType.@DoanhThuQuiThangTheoDichVu);
                    break;
                case "/REPORT-REVENUE-MONTH-QUARTER-SERVICE-TYPE":
                    pReportType = nameof(ReportType.@DoanhThuQuiThangTheoLoaiDichVu);
                    break;
                case "/REPORT-REVENUE-SERVICE":
                    pReportType = nameof(ReportType.@DoanhThuTheoDichVu);
                    break;
                case "/REPORT-REVENUE-SERVICE-TYPE":
                    pReportType = nameof(ReportType.@DoanhThuTheoLoaiDichVu);
                    break;
                case "/REPORT-REVENUE-MONTH-QUARTER-CONSULT-USER":
                    pReportType = nameof(ReportType.@DoanhThuQuiThangTheoNhanVienTuVan);
                    break;
                case "/REPORT-REVENUE-MONTH-QUARTER-IMPLEMENT-USER":
                    pReportType = nameof(ReportType.@DoanhThuQuiThangTheoNhanVienThucHien);
                    break;
            }
            ItemFilter.Type = pReportType;
            pReportTypeName = ListTypeReports?.FirstOrDefault(m => m.Code == pReportType)?.Name + "";
            ListBreadcrumbs = new List<BreadcrumbModel>
                    {
                        new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                        new BreadcrumbModel() { Text = "Báo cáo" },
                        new BreadcrumbModel() { Text = pReportTypeName }
                    };
            await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
            await getDataReports();
            await InvokeAsync(StateHasChanged);
        }

        async void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            try
            {
                // Cập nhật giá trị khi location thay đổi
                currentLocation = e.Location;
                if (currentLocation != null)
                {
                   // currentLocation = currentLocation.ToUpper();
                    if (currentLocation.Contains("/report-revenue-month-quarter-service-type"))
                    {
                        await setDataBreadCrumChanged("/report-revenue-month-quarter-service-type");
                    }
                    else if (currentLocation.Contains("/report-revenue-month-quarter-service"))
                    {
                        await setDataBreadCrumChanged("/report-revenue-month-quarter-service");
                    }
                    else if (currentLocation.Contains("/report-revenue-service"))
                    {
                        await setDataBreadCrumChanged("/report-revenue-service");
                    }
                    else if (currentLocation.Contains("/report-revenue-service-type"))
                    {
                        await setDataBreadCrumChanged("/report-revenue-service-type");
                    }
                    else if (currentLocation.Contains("/report-revenue-month-quarter-consult-user"))
                    {
                        await setDataBreadCrumChanged("/report-revenue-month-quarter-consult-user");
                    }
                    else if (currentLocation.Contains("/report-revenue-month-quarter-implement-user"))
                    {
                        await setDataBreadCrumChanged("/report-revenue-month-quarter-implement-user");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "EnumController", "LocationChanged");
                ShowError(ex.Message);
            }
        }

        private async Task getDataReports()
        {
            ListReports = new List<ReportModel>();
            ItemFilter.UserId = pUserId;
            //ItemFilter.IsAdmin = pIsAdmin;
            ListReports = await _documentService!.GetDataReportAsync(ItemFilter);
            Grid?.Rebind();
            await InvokeAsync(StateHasChanged);
        }

        #endregion

        #region Protected Functions
        protected async void ReLoadDataHandler()
        {
            try
            {
                if (ItemFilter.FromDate.HasValue && ItemFilter.ToDate.HasValue
                    && ItemFilter.FromDate.Value.Date > ItemFilter.ToDate.Value.Date)
                {
                    ShowWarning("Dữ liệu tìm kiếm không hợp lệ. [Từ ngày] <= [Đến ngày]");
                    return;
                }
                IsInitialDataLoadComplete = false;
                await getDataReports();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ReportController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }
        #endregion
    }
}
