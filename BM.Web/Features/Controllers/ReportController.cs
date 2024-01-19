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
        public List<ComboboxModel>? ListTypeTime{ get; set; } // quí hay tháng 
        public RequestReportModel ItemFilter = new RequestReportModel();
        public TelerikGrid<ReportModel> Grid;
        public List<BranchModel>? ListBranchs { get; set; }
        public List<ComboboxModel>? ListTypeReports { get; set; } // loại báo cáo
        public List<ComboboxModel>? ListKinds { get; set; } // từ ngày - đến ngày hay quí - tháng
        public List<ComboboxModel>? ListServiceTypes { get; set; } // từ ngày - đến ngày hay quí - tháng
        public List<ComboboxModel>? ListUserTypes { get; set; } // từ ngày - đến ngày hay quí - tháng

        public List<ComboboxModel>? ListAndCharts { get; set; } // lưới hay là biểu đồ

        public List<ReportChartModel> ListChart { get; set; } = new List<ReportChartModel>();

        public List<int>? ListYears { get; set; }
        public int pYearDefault { get; set; }
        public string TilteReport = "BIỂU ĐỒ DOANH THU";

        public string pKind = "";
        public string pServiceType = "";
        public string pUserType = "";
        public string pChart = "";



        public string pReportType = "";
        public string pReportTypeName = "";
        private string currentLocation = "";
        #endregion

        #region Override Functions
        public async void SelectedKind(string kind)
        {
            try
            {
                pKind = kind;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async void SelectedChart(string chart)
        {
            try
            {
                pChart = chart;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// chọn loại báo cáo
        /// </summary>
        /// <param name="supplies"></param>
        /// <param name="invetory"></param>
        public async void SelectedReportTypeChanged(string report)
        {
            try
            {
                if (report == null) return;
                 pReportType = report; // gán loại báo cáo
                ItemFilter.Type = report;
                pReportTypeName = ListTypeReports.FirstOrDefault(m => m.Code == pReportType)?.Name + "";
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Báo cáo" },
                    new BreadcrumbModel() { Text =  pReportTypeName}
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                _navManager.LocationChanged += LocationChanged;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

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

                    new ComboboxModel() {Code = nameof(ReportType.DoanhThuDichVuLoaiDichVu), Name = "Doanh thu dịch vụ - loại dịch vụ"},
                    new ComboboxModel() {Code = nameof(ReportType.BaoCaoKPINhanVien), Name = "Báo cáo KPI nhân viên"},
                    new ComboboxModel() {Code = nameof(ReportType.BaoCaoNhapXuatKho), Name = "Báo cáo nhập xuất kho"},

                };

                ListKinds = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(Kind.QuiThang), Name = "Quí - tháng"},
                    new ComboboxModel() {Code = nameof(Kind.TuNgayDenNgay), Name = "Từ ngày - đến ngày"},

                };

                ListServiceTypes = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(ServiceType.Service), Name = "Dịch vụ"},
                    new ComboboxModel() {Code = nameof(ServiceType.ServiceType), Name = "Loại dịch vụ"},
                };

                ListUserTypes = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(UserType.ConsultUser), Name = "Nhân viên tư vấn"},
                    new ComboboxModel() {Code = nameof(UserType.ImplementUser), Name = "Nhân viên thực hiện"},
                };

                ListAndCharts = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(ChartReportType.List), Name = "Lưới"},
                    new ComboboxModel() {Code = nameof(ChartReportType.Chart), Name = "Biểu đồ"},
                };

                ListYears = new List<int>();
                pYearDefault = DateTime.Now.Year;
                for (int i = 2023; i < (DateTime.Now.Year + 1); i++)
                {
                    ListYears.Add(i);
                }

                pReportType = nameof(ReportType.DoanhThuDichVuLoaiDichVu);
                var uri = _navManager!.ToAbsoluteUri(_navManager.Uri);
                switch (uri.AbsolutePath.ToString())
                {
                    case "/report-service":
                        pReportType = nameof(ReportType.@DoanhThuDichVuLoaiDichVu);
                        break;
                    case "/report-user":
                        pReportType = nameof(ReportType.@BaoCaoKPINhanVien);
                        break;
                    case "/report-outbound":
                        pReportType = nameof(ReportType.@BaoCaoNhapXuatKho);
                        break;
                }
                pReportTypeName = ListTypeReports.FirstOrDefault(m => m.Code == pReportType)?.Name + "";
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Báo cáo" },
                    new BreadcrumbModel() { Text =  pReportTypeName}
                };
                ItemFilter.Type = pReportType; // gán loại báo cáo

                pKind = nameof(Kind.QuiThang);
                pServiceType = nameof(ServiceType.Service);
                pUserType = nameof(UserType.ConsultUser);
                pChart = nameof(ChartReportType.List);

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
                    ListBranchs.Add(new BranchModel { BranchId = "", BranchName = "Tất cả chi nhánh" });
                    ItemFilter.BranchId = ListBranchs.LastOrDefault().BranchId;
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
            switch (location)
            {
                case "/report-outbound":
                    pReportType = nameof(ReportType.@BaoCaoNhapXuatKho);
                    break;
                case "/report-service":
                    pReportType = nameof(ReportType.@DoanhThuDichVuLoaiDichVu);
                    break;
                case "/report-user":
                    pReportType = nameof(ReportType.BaoCaoKPINhanVien);
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
                    if (currentLocation.Contains("/report-outbound"))
                    {
                        await setDataBreadCrumChanged("/report-outbound");
                    }
                    else if (currentLocation.Contains("/report-service"))
                    {
                        await setDataBreadCrumChanged("/report-service");
                    }
                    else if (currentLocation.Contains("/report-user"))
                    {
                        await setDataBreadCrumChanged("/report-user");
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

            if(pReportType == nameof(ReportType.@DoanhThuDichVuLoaiDichVu))
            {
                if (pServiceType == nameof(ServiceType.Service))
                {
                    if (pKind == nameof(Kind.QuiThang))
                    {
                        ItemFilter.Type = "DoanhThuQuiThangTheoDichVu";
                        TilteReport = "BIỂU ĐỒ DOANH THU THEO DỊCH VỤ";
                    }
                    else if (pKind == nameof(Kind.TuNgayDenNgay))
                    {
                        ItemFilter.Type = "DoanhThuTheoDichVu";
                    }
                }
                else if (pServiceType == nameof(ServiceType.ServiceType))
                {
                    if (pKind == nameof(Kind.QuiThang))
                    {
                        ItemFilter.Type = "DoanhThuQuiThangTheoLoaiDichVu";
                        TilteReport = "BIỂU ĐỒ DOANH THU THEO LOẠI DỊCH VỤ";
                    }
                    else if (pKind == nameof(Kind.TuNgayDenNgay))
                    {
                        ItemFilter.Type = "DoanhThuTheoLoaiDichVu";
                    }
                }

            }
            else if (pReportType == nameof(ReportType.BaoCaoKPINhanVien))
            {
                if (pUserType == nameof(UserType.ConsultUser))
                {
                    if (pKind == nameof(Kind.QuiThang))
                    {
                        ItemFilter.Type = "DoanhThuQuiThangTheoNhanVienTuVan";
                        TilteReport = "BIỂU ĐỒ DOANH THU THEO NHÂN VIÊN TƯ VẤN";
                    }
                    else if (pKind == nameof(Kind.TuNgayDenNgay))
                    {
                        ItemFilter.Type = "DoanhThuTheoNhanVienTuVan";
                    }
                }
                else if (pUserType == nameof(UserType.ImplementUser))
                {
                    if (pKind == nameof(Kind.QuiThang))
                    {
                        ItemFilter.Type = "DoanhThuQuiThangTheoNhanVienThucHien";
                        TilteReport = "BIỂU ĐỒ DOANH THU THEO NHÂN VIÊN THỰC HIỆN";
                    }
                    else if (pKind == nameof(Kind.TuNgayDenNgay))
                    {
                        ItemFilter.Type = "DoanhThuTheoNhanVienThucHien";
                    }
                }

            }
            //else if (pReportType == nameof(ReportType.@BaoCaoNhapXuatKho))
            //{
            //    if (pReportType == nameof(ReportType.@BaoCaoNhapXuatKho))
            //    {

            //    }
            //    else if (pReportType == nameof(ReportType.@BaoCaoNhapXuatKho))
            //    {

            //    }
            //}


            //ItemFilter.IsAdmin = pIsAdmin;
            ItemFilter.Year = pYearDefault;
            ListReports = await _documentService!.GetDataReportAsync(ItemFilter);


            ListChart = new List<ReportChartModel>();
            
            if(pChart == nameof(ChartReportType.Chart))
            {

                if (ListReports !=null && ListReports.Count > 0)
                {
                    foreach (var item in ListReports)
                    {
                        List<object> val = new List<object>();
                        string[] xAxisItems = new string[] { };
                        if (pKind == nameof(Kind.QuiThang) && ItemFilter.TypeTime == nameof(TypeTime.Qui))
                        {
                            val.Add(item.Total_01);
                            val.Add(item.Total_02);
                            val.Add(item.Total_03);
                            val.Add(item.Total_04);
                            xAxisItems = new string[] { "Q1", "Q2", "Q3", "Q4" };
                        }
                        else if (pKind == nameof(Kind.QuiThang) && ItemFilter.TypeTime == nameof(TypeTime.Thang))
                        {
                            val.Add(item.Total_01);
                            val.Add(item.Total_02);
                            val.Add(item.Total_03);
                            val.Add(item.Total_04);
                            val.Add(item.Total_04);
                            val.Add(item.Total_05);
                            val.Add(item.Total_06);
                            val.Add(item.Total_07);
                            val.Add(item.Total_08);
                            val.Add(item.Total_09);
                            val.Add(item.Total_10);
                            val.Add(item.Total_11);
                            val.Add(item.Total_12);
                            xAxisItems = new string[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
                        }
                        if(pReportType == nameof(ReportType.DoanhThuDichVuLoaiDichVu))
                        {
                            if(pServiceType == nameof(ServiceType.Service))
                            {
                                ListChart.Add(new ReportChartModel { ListTitle = xAxisItems, ListValue = val, Title = item.ServiceCode +" - " +item.ServiceName }); 
                            }else if (pServiceType == nameof(ServiceType.ServiceType))
                            {
                                ListChart.Add(new ReportChartModel { ListTitle = xAxisItems, ListValue = val, Title = item.EnumId + " - " + item.EnumName });
                            }
                        }
                        else if (pReportType == nameof(ReportType.BaoCaoKPINhanVien))
                        {
                            if (pUserType == nameof(UserType.ConsultUser))
                            {
                                ListChart.Add(new ReportChartModel { ListTitle = xAxisItems, ListValue = val, Title = item.ConsultUserId + " - " + item.ConsultUserName });
                            }
                            else if (pUserType == nameof(UserType.ImplementUser))
                            {
                                ListChart.Add(new ReportChartModel { ListTitle = xAxisItems, ListValue = val, Title = item.ImplementUserId + " - " + item.ImplementUserName });
                            }
                        }
                    }
                }
            }

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
