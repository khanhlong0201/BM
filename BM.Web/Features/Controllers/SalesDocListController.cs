using BM.Models;
using BM.Models.Shared;
using BM.Web.Commons;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Grid;

namespace BM.Web.Features.Controllers
{
    public class SalesDocListController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<SalesDocListController>? _logger { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<DocumentModel>? ListDocuments { get; set; }
        public IEnumerable<DocumentModel>? SelectedDocuments { get; set; } = new List<DocumentModel>();
        public List<ComboboxModel>? ListStatus { get; set; }
        public SearchModel ItemFilter = new SearchModel();
        public string? ReasonDeny { get; set; } // lý do hủy
        public bool IsShowDialogDelete { get; set; }
        public bool IsShowDialogDebts { get; set; }

        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }
        public List<CustomerDebtsModel>? ListCusDebts { get; set; }
        public double DebtsGuestPay { get; set; } // Số tiền nợ khách trả
        public double TotalDebtAmount { get; set; } // Tổng số tiền nợ còn lại
        public string? VoucherNo { get; set; }
        public int pDocEntry { get; set; }
        public string? DebtRemark { get; set; }
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
                    new BreadcrumbModel() { Text = "Theo dõi đơn hàng" },
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                ListStatus = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(DocStatus.Pending), Name = "Chờ xử lý"},
                    new ComboboxModel() {Code = nameof(DocStatus.Closed), Name = "Đã thanh toán"},
                    new ComboboxModel() {Code = nameof(DocStatus.Cancled), Name = "Đã hủy đơn"},
                    new ComboboxModel() {Code = nameof(DocStatus.All), Name = "Tất cả"},
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
            ListDocuments = new List<DocumentModel>();
            SelectedDocuments = new List<DocumentModel>();
            ItemFilter.UserId = pUserId;
            ItemFilter.IsAdmin = pIsAdmin;
            ListDocuments = await _documentService!.GetDataDocumentsAsync(ItemFilter);
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
                _logger!.LogError(ex, "SalesDocListController", "ReLoadDataHandler");
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
                DocumentModel? oItem = (args.Item as DocumentModel);
                if (oItem == null) return;
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pDocEntry", $"{oItem.DocEntry}"},
                    { "pIsCreate", $"{false}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navManager!.NavigateTo($"/create-ticket?key={key}");
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
                if(SelectedDocuments == null || !SelectedDocuments.Any())
                {
                    ShowWarning("Vui lòng chọn dòng để hủy!");
                    return;
                }
                var checkData = SelectedDocuments.FirstOrDefault(m => m.StatusId != nameof(DocStatus.Pending));
                if (checkData != null)
                {
                    ShowWarning("Chỉ được phép hủy các đơn hàng có tình trạng [Chờ xử lý]!");
                    return;
                }
                ReasonDeny = "";
                IsShowDialogDelete = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "OpenDialogDeleteHandler");
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
                if(string.IsNullOrWhiteSpace(ReasonDeny))
                {
                    ShowWarning("Vui lòng điền lý do hủy đơn!");
                    return;
                }
                await ShowLoader();
                bool isSuccess = await _documentService!.CancleDocList(string.Join(",", SelectedDocuments!.Select(m => m.DocEntry))
                        , ReasonDeny , pUserId, nameof(EnumTable.Drafts));
                if (isSuccess)
                {
                    IsShowDialogDelete = false;
                    await getDataDocuments();
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "CancleDocListHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }
        
        /// <summary>
        /// Xem lịch sử thanh toán
        /// </summary>
        /// <param name="pDocument"></param>
        protected async void OpenDialogDebtsHandler(DocumentModel pDocument, bool pIsShowVoucher = false)
        {
            try
            {
                if(pIsShowVoucher)
                {
                    Dictionary<string, string> pParams = new Dictionary<string, string>
                    {
                        { "pDocEntry", $"{pDocument.DocEntry}"},
                        { "pIsCreate", $"{false}" },
                    };
                    string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                    _navManager!.NavigateTo($"/create-ticket?key={key}");
                    return;
                }    
                VoucherNo = string.Empty;
                pDocEntry = 0;
                if (pDocument == null) return;
                await ShowLoader();
                DebtRemark = string.Empty;
                TotalDebtAmount = 0;
                DebtsGuestPay = 0;
                VoucherNo = pDocument.VoucherNo;
                pDocEntry = pDocument.DocEntry;
                ListCusDebts = await _documentService!.GetCustomerDebtsByDocAsync(pDocument.DocEntry, nameof(EnumType.DebtReminder), "");
                if(ListCusDebts != null && ListCusDebts.Any())
                {
                    TotalDebtAmount = ListCusDebts.OrderByDescending(m => m.Id).First().TotalDebtAmount;
                    DebtsGuestPay = TotalDebtAmount;
                }    
                IsShowDialogDebts = true;
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "OpenDialogDebtsHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// lưu thông tin thanh toán
        /// </summary>
        protected async void SavePaymentHandler()
        {
            try
            {
                if (TotalDebtAmount <= 0)
                {
                    ShowInfo($"Đơn hàng [{VoucherNo}] đã hoàn tất thanh toán. Vui lòng kiểm tra lại thông tin!");
                    return;
                }
                if (ListCusDebts == null || !ListCusDebts.Any()) return;
                if (DebtsGuestPay <= 0)
                {
                    ShowWarning("Vui lòng nhập số tiền khách trả!");
                    return;
                }
                string messageDept = "";
                CustomerDebtsModel oFirstItem = ListCusDebts[0];
                if (DebtsGuestPay < TotalDebtAmount)
                {
                    messageDept = $"Vẫn còn nợ {string.Format(DefaultConstants.FORMAT_GRID_CURRENCY, (TotalDebtAmount - DebtsGuestPay))}đ." +
                        $" Số tiền sẽ được lưu vào công nợ của khách hàng [{oFirstItem.FullName}].";
                }
                bool isConfirm = await _rDialogs!.ConfirmAsync($"{messageDept} Bạn có chắc muốn thanh toán đơn hàng này?", "Thông báo");
                if (!isConfirm) return;
                await ShowLoader();
                CustomerDebtsModel oItem = new CustomerDebtsModel();
                oItem.CusNo = oFirstItem.CusNo;
                oItem.DocEntry = pDocEntry;
                oItem.TotalDebtAmount = TotalDebtAmount - DebtsGuestPay;
                oItem.GuestsPay = DebtsGuestPay;
                oItem.Remark = DebtRemark;
                // call api 
                bool isSuccess = await _documentService!.UpdateCustomerDebtsAsync(JsonConvert.SerializeObject(oItem), pUserId);
                if (isSuccess)
                {
                    await ReloadDataDebtsByDocHandler();
                    await getDataDocuments();
                }    
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "SavePaymentHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await InvokeAsync(StateHasChanged);
                await ShowLoader(false);
            }
        }

        /// <summary>
        /// load lại dữ liệu lịch sử trả nợ
        /// </summary>
        protected async Task ReloadDataDebtsByDocHandler()
        {
            try
            {
                DebtRemark = string.Empty;
                TotalDebtAmount = 0;
                DebtsGuestPay = 0;
                ListCusDebts = await _documentService!.GetCustomerDebtsByDocAsync(pDocEntry, nameof(EnumType.DebtReminder), "");
                if (ListCusDebts != null && ListCusDebts.Any())
                {
                    TotalDebtAmount = ListCusDebts.OrderByDescending(m => m.Id).First().TotalDebtAmount;
                    DebtsGuestPay = TotalDebtAmount;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "ReloadDataDebtsByDoc");
                ShowError(ex.Message);
            }
            finally
            {
                await InvokeAsync(StateHasChanged);
                await ShowLoader(false);
            }
        }
        #endregion
    }
}
