using BM.Models;
using BM.Models.Shared;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace BM.Web.Features.Controllers
{
    public class ServiceCallListController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<ServiceCallListController>? _logger { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<ServiceCallModel>? ListDocuments { get; set; }
        public IEnumerable<ServiceCallModel>? SelectedDocuments { get; set; } = new List<ServiceCallModel>();
        public List<ComboboxModel>? ListStatus { get; set; }
        public SearchModel ItemFilter = new SearchModel();
        public string? ReasonDeny { get; set; } // lý do hủy
        public bool IsShowDialogDelete { get; set; }

        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }
        public List<CustomerDebtsModel>? ListCusDebts { get; set; }
        #endregion

        #region Override Functions
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Theo dõi" },
                    new BreadcrumbModel() { Text = "Theo dõi bảo hành" },
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                ListStatus = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(DocStatus.All), Name = "Tất cả"},
                    new ComboboxModel() {Code = nameof(DocStatus.Pending), Name = "Chờ xử lý"},
                    new ComboboxModel() {Code = nameof(DocStatus.Closed), Name = "Hoàn thành"},
                    new ComboboxModel() {Code = nameof(DocStatus.Cancled), Name = "Đã hủy phiếu"},
                };
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "OnInitializedAsync");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                try
                {
                    ItemFilter.StatusId = nameof(DocStatus.Pending);
                    ItemFilter.FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    ItemFilter.ToDate = _dateTimeService!.GetCurrentVietnamTime();
                    // đọc giá tri câu query
                    var uri = _navManager?.ToAbsoluteUri(_navManager.Uri);
                    if (uri != null && QueryHelpers.ParseQuery(uri.Query).Count > 0)
                    {
                        string key = uri.Query.Substring(5); // để tránh parse lỗi;    
                        Dictionary<string, string> pParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(EncryptHelper.Decrypt(key));
                        if (pParams != null && pParams.Any() && pParams.ContainsKey("pStatusId")) ItemFilter.StatusId = pParams["pStatusId"];
                    }
                    //
                    await _progressService!.SetPercent(0.4);
                    await getDataDocuments();
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
        private async Task getDataDocuments()
        {
            ListDocuments = new List<ServiceCallModel>();
            SelectedDocuments = new List<ServiceCallModel>();
            ItemFilter.UserId = pUserId;
            ItemFilter.IsAdmin = pIsAdmin;
            ListDocuments = await _documentService!.GetServiceCallsAsync(ItemFilter);
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
                await getDataDocuments();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceCallListController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async void OnRowDoubleClickHandler(GridRowClickEventArgs args)
        {
            try
            {
                ServiceCallModel? oItem = (args.Item as ServiceCallModel);
                if (oItem == null) return;
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pDocEntry", $"{oItem.DocEntry}"},
                    { "pIsCreate", $"{false}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navManager!.NavigateTo($"/service-call?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "OnRowDoubleClickHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OpenDialogDeleteHandler()
        {
            try
            {
                if (SelectedDocuments == null || !SelectedDocuments.Any())
                {
                    ShowWarning("Vui lòng chọn dòng để hủy!");
                    return;
                }
                var checkData = SelectedDocuments.FirstOrDefault(m => m.StatusId != nameof(DocStatus.Pending));
                if (checkData != null)
                {
                    ShowWarning("Chỉ được phép hủy các phiếu bảo hành có tình trạng [Chờ xử lý]!");
                    return;
                }
                ReasonDeny = "";
                IsShowDialogDelete = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceCallListController", "OpenDialogDeleteHandler");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// xác nhận hủy đơn hàng
        /// </summary>
        protected async void CancleDocListHandler()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ReasonDeny))
                {
                    ShowWarning("Vui lòng điền lý do hủy phiếu bảo hành!");
                    return;
                }
                await ShowLoader();
                bool isSuccess = await _documentService!.CancleDocList(string.Join(",", SelectedDocuments!.Select(m => m.DocEntry))
                    , ReasonDeny, pUserId, nameof(EnumTable.ServiceCalls));
                if (isSuccess)
                {
                    IsShowDialogDelete = false;
                    await getDataDocuments();
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceCallListController", "CancleDocListHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }
        #endregion
    }
}
