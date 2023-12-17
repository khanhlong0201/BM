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
    public class OutBoundListController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<SalesDocListController>? _logger { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<OutBoundModel>? ListOutBounds { get; set; }
        public IEnumerable<OutBoundModel>? SelectedOutBounds { get; set; } = new List<OutBoundModel>();
        public SearchModel ItemFilter = new SearchModel();
        public string? ReasonDeny { get; set; } // lý do hủy
        public bool IsShowDialogDelete { get; set; }
        public bool IsShowDialogDebts { get; set; }
        public bool IsShowOutBound { get; set; } = false;
        public OutBoundModel OutBoundUpdate { get; set; } = new OutBoundModel();
        public List<SuppliesModel> ListSuppplies { get; set; } = new List<SuppliesModel>();
        public EditContext? _EditOutBoundContext { get; set; }
        public IEnumerable<ComboboxModel>? ListUsers { get; set; } // danh sách nhân viên

        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }
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
                    new BreadcrumbModel() { Text = "Theo dõi phiếu xuất kho" },
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
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
                    await getDataOutBounds();
                    var listUsers = await _masterDataService!.GetDataUsersAsync();
                    if (listUsers != null && listUsers.Any()) ListUsers = listUsers.Select(m => new ComboboxModel()
                    {
                        Code = m.EmpNo,
                        Name = $"{m.EmpNo}-{m.FullName}"
                    });
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
        private async Task getDataOutBounds()
        {
            ListOutBounds = new List<OutBoundModel>();
            SelectedOutBounds = new List<OutBoundModel>();
            ItemFilter.UserId = pUserId;
            ItemFilter.IsAdmin = pIsAdmin;
            ListOutBounds = await _documentService!.GetDataOutBoundsAsync(ItemFilter);
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
                await getDataOutBounds();
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

        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as OutBoundModel);
        
        /// <summary>
        /// xem chi tiết phiếu xuất kho
        /// </summary>
        /// <param name="pAction"></param>
        /// <param name="pItemDetails"></param>
        protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, OutBoundModel? pItemDetails = null)
        {
            try
            {
                ListSuppplies = new List<SuppliesModel>();
                OutBoundUpdate.DocEntry = pItemDetails.DocEntry;
                OutBoundUpdate.VoucherNo = pItemDetails.VoucherNo;
                OutBoundUpdate.BaseEntry = pItemDetails.BaseEntry;
                OutBoundUpdate.IdDraftDetail = pItemDetails.IdDraftDetail;
                OutBoundUpdate.ColorImplement = pItemDetails.ColorImplement;
                OutBoundUpdate.SuppliesQtyList = pItemDetails.SuppliesQtyList;
                OutBoundUpdate.AnesthesiaType = pItemDetails.AnesthesiaType;
                OutBoundUpdate.AnesthesiaQty = pItemDetails.AnesthesiaQty;
                OutBoundUpdate.DarkTestColor = pItemDetails.DarkTestColor;
                OutBoundUpdate.CoadingColor = pItemDetails.CoadingColor;
                OutBoundUpdate.LibColor = pItemDetails.LibColor;
                OutBoundUpdate.StartTime = pItemDetails.StartTime;
                OutBoundUpdate.EndTime = pItemDetails.EndTime;
                OutBoundUpdate.Problems = pItemDetails.Problems;
                OutBoundUpdate.ChargeUser = pItemDetails.ChargeUser;
                OutBoundUpdate.ChargeUserName = pItemDetails.ChargeUserName;
                OutBoundUpdate.BranchId = pItemDetails.BranchId;
                OutBoundUpdate.BranchName = pItemDetails.BranchName;
                OutBoundUpdate.ServiceCode = pItemDetails.ServiceCode;
                OutBoundUpdate.ServiceName = pItemDetails.ServiceName;
                OutBoundUpdate.CusNo = pItemDetails.CusNo;
                OutBoundUpdate.FullName = pItemDetails.FullName;
                OutBoundUpdate.Remark = pItemDetails.Remark;
                OutBoundUpdate.HealthStatus = pItemDetails.HealthStatus;
                OutBoundUpdate.DateCreate = pItemDetails.DateCreate;;
                OutBoundUpdate.ListUserImplements = pItemDetails.ImplementUserId?.Split(",")?.ToList(); // nhân viên thực hiện
                
                if(OutBoundUpdate.SuppliesQtyList != null)
                {
                    List<SuppliesModel> lstSuppplies = JsonConvert.DeserializeObject<List<SuppliesModel>>(OutBoundUpdate.SuppliesQtyList);
                    for (int i = 0; i < lstSuppplies.Count; i++)
                    {
                        var item = lstSuppplies[i];
                        SuppliesModel oLine = new SuppliesModel();
                        oLine.SuppliesCode = item.SuppliesCode;
                        oLine.SuppliesName = item.SuppliesName;
                        oLine.Qty = item.Qty;
                        oLine.QtyInv = item.QtyInv;
                        ListSuppplies.Add(oLine);
                    }
                }

                _EditOutBoundContext = new EditContext(OutBoundUpdate);
                IsShowOutBound = true;
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "EnumController", "OnOpenDialogHandler");
                ShowError(ex.Message);
            }
        }

        ///<summary>
        /// đá sang page chi tiết đơn hàng
        /// </summary>
        protected async void OnClickHandlerNavTicKet(OutBoundModel oItem)
        {
            try
            {
                if (oItem == null) return;
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pDocEntry", $"{oItem.BaseEntry}"},
                    { "pIsCreate", $"{false}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navManager!.NavigateTo($"/create-ticket?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "OutBoundController", "OnRowDoubleClickHandler");
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
                if(SelectedOutBounds == null || !SelectedOutBounds.Any())
                {
                    ShowWarning("Vui lòng chọn dòng để hủy!");
                    return;
                }
                //var checkData = SelectedOutBounds.FirstOrDefault(m => m.StatusId != nameof(DocStatus.Pending));
                //if (checkData != null)
                //{
                //    ShowWarning("Chỉ được phép hủy các đơn hàng có tình trạng [Chờ xử lý]!");
                //    return;
                //}
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
        /// xác nhận hủy phiếu xuất kho
        /// </summary>
        protected async void CancleOutBoundListHandler()
        {
            try
            {
                if(string.IsNullOrWhiteSpace(ReasonDeny))
                {
                    ShowWarning("Vui lòng điền lý do phiếu xuất kho!");
                    return;
                }
                await ShowLoader();
                bool isSuccess = await _documentService!.CancleOutBoundList(string.Join(",", SelectedOutBounds!.Select(m => m.DocEntry)), ReasonDeny , pUserId);
                if (isSuccess)
                {
                    IsShowDialogDelete = false;
                    await getDataOutBounds();
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
        
        

        
        #endregion
    }
}
