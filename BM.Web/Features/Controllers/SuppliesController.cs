using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Telerik.Blazor.Components;
using BM.Web.Commons;
namespace BM.Web.Features.Controllers
{
    public class SupliesController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<BranchController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        #endregion
        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<EnumModel>? ListEnums { get; set; } //đơn vị tính
        public List<SuppliesModel>? ListSupplies { get; set; }
        public IEnumerable<SuppliesModel>? SelectedSupplies { get; set; } = new List<SuppliesModel>();
        public SuppliesModel SuppliesUpdate { get; set; } = new SuppliesModel();
        public EditContext? _EditContext { get; set; }
        public EditContext? _EditInvContext { get; set; }
        public List<InvetoryModel>? ListInvetoryHistory { get; set; } = new List<InvetoryModel>();
        public List<InvetoryModel>? ListInvetoryCreate { get; set; } = new List<InvetoryModel>();
        public IEnumerable<InvetoryModel>? SelectedInvetoryHistory { get; set; } = new List<InvetoryModel>();
        public InvetoryModel InvetoryHistoryUpdate { get; set; } = new InvetoryModel();
        public bool IsShowDialog { get; set; }
        public bool IsShowIntoInv { get; set; }
        public bool IsShowIntoUpdateInv { get; set; }
        public bool IsCreate { get; set; } = true;
        public HConfirm? _rDialogs { get; set; }
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
                    new BreadcrumbModel() { Text = "Quản lý vật tư tồn kho" }
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
                    await _progressService!.SetPercent(0.4);
                    ListEnums = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.Unit));
                    await getData();
                    await getDataInv();

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
        private async Task getDataInv()
        {
            ListInvetoryHistory = new List<InvetoryModel>();
            SelectedInvetoryHistory = new List<InvetoryModel>();
            ListInvetoryHistory = await _masterDataService!.GetDataInvetoryAsync();
        }

        private async Task getData()
        {
            ListSupplies = new List<SuppliesModel>();
            SelectedSupplies = new List<SuppliesModel>();
            ListSupplies = await _masterDataService!.GetDataSuppliesAsync();
        }

        #endregion

        #region Protected Functions
        protected async void ReLoadDataInvHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataInv();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SuppliesController", "ReLoadDataInvHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getData();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SuppliesController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, SuppliesModel? pItemDetails = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                    IsCreate = true;
                    SuppliesUpdate = new SuppliesModel();
                }
                else
                {
                    SuppliesUpdate.SuppliesCode = pItemDetails!.SuppliesCode;
                    SuppliesUpdate.SuppliesName = pItemDetails!.SuppliesName;
                    SuppliesUpdate.EnumId = pItemDetails!.EnumId;
                    IsCreate = false;
                }
                IsShowDialog = true;
                _EditContext = new EditContext(SuppliesUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SuppliesController", "OnOpenDialogHandler");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// mở sửa tồn kho
        /// </summary>
        /// <param name="pAction"></param>
        /// <param name="pItemDetails"></param>
        protected void OnOpenDialogInvHandler(EnumType pAction = EnumType.Add, InvetoryModel? pItemDetails = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                    IsCreate = true;
                    InvetoryHistoryUpdate = new InvetoryModel();
                }
                else
                {
                    InvetoryHistoryUpdate.Absid = pItemDetails!.Absid;
                    InvetoryHistoryUpdate.SuppliesCode = pItemDetails!.SuppliesCode;
                    InvetoryHistoryUpdate.EnumId = pItemDetails!.EnumId;
                    InvetoryHistoryUpdate.EnumName = pItemDetails!.EnumName;
                    InvetoryHistoryUpdate.QtyInv = pItemDetails!.QtyInv;
                    InvetoryHistoryUpdate.Price = pItemDetails!.Price;
                    InvetoryHistoryUpdate.BranchId = pItemDetails!.BranchId;
                    IsCreate = false;
                }
                IsShowIntoUpdateInv = true;
                _EditInvContext = new EditContext(InvetoryHistoryUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SuppliesController", "OnOpenDialogInvHandler");
                ShowError(ex.Message);
            }
        }
        /// <summary>
        /// mở nhập kho
        /// </summary>
        protected void OnOpenIntoInvHandler()
        {
            try
            {
                IsShowIntoInv = true;
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SuppliesController", "OnOpenIntoInvHandler");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// lưu vật tư
        /// </summary>
        /// <param name="pEnum"></param>
        protected async void SaveDataHandler(EnumType pEnum = EnumType.SaveAndClose)
        {
            try
            {
                string sAction = IsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
                var checkData = _EditContext!.Validate();
                if (!checkData) return;
                await ShowLoader();
                bool isSuccess = await _masterDataService!.UpdateInvetoryAsync(JsonConvert.SerializeObject(InvetoryHistoryUpdate), JsonConvert.SerializeObject(ListInvetoryCreate), sAction, pUserId);
                if (isSuccess)
                {
                    await getData();
                    if (pEnum == EnumType.SaveAndCreate)
                    {
                        SuppliesUpdate = new SuppliesModel();
                        _EditContext = new EditContext(SuppliesUpdate);
                        return;
                    }
                    IsShowDialog = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SuppliesController", "SaveDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }


        /// <summary>
        /// lưu nhập kho
        /// </summary>
        /// <param name="pEnum"></param>
        protected async void SaveDataInvHandler(EnumType pEnum = EnumType.SaveAndClose)
        {
            try
            {
                string sAction = IsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
                await ShowLoader();

                bool isSuccess = await _masterDataService!.UpdateInvetoryAsync(JsonConvert.SerializeObject(InvetoryHistoryUpdate), JsonConvert.SerializeObject(ListInvetoryCreate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataInv();
                    if (pEnum == EnumType.SaveAndCreate)
                    {
                        ListInvetoryCreate = new List<InvetoryModel>();
                        return;
                    }
                    IsShowIntoInv = false;
                    IsShowIntoUpdateInv = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SuppliesController", "SaveDataInvHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// sửa vật tư
        /// </summary>
        /// <param name="args"></param>
        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as SuppliesModel);
       
        
        /// <summary>
        /// sửa tồn kho
        /// </summary>
        /// <param name="args"></param>
        protected void OnRowDoubleClickInvHandler(GridRowClickEventArgs args) => OnOpenDialogInvHandler(EnumType.Update, args.Item as InvetoryModel);


        /// <summary>
        /// xóa vật tư
        /// </summary>
        protected async void DeleteDataHandler()
        {
            try
            {
                if (SelectedSupplies == null || !SelectedSupplies.Any())
                {
                    ShowWarning(DefaultConstants.MESSAGE_NO_CHOSE_DATA);
                    return;
                }
                var confirm = await _rDialogs!.ConfirmAsync($" {DefaultConstants.MESSAGE_CONFIRM_DELETE} ");
                if (!confirm) return;
                await ShowLoader();
                bool isSuccess = await _masterDataService!.DeleteDataAsync(nameof(EnumTable.Supplies), "", string.Join(",", SelectedSupplies.Select(m => m.SuppliesCode)), pUserId);
                if (isSuccess)
                {
                    await getData();
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "UserController", "DeleteDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// xóa tồn kho
        /// </summary>
        protected async void DeleteDataInvHandler()
        {
            try
            {
                if (SelectedInvetoryHistory == null || !SelectedInvetoryHistory.Any())
                {
                    ShowWarning(DefaultConstants.MESSAGE_NO_CHOSE_DATA);
                    return;
                }
                var confirm = await _rDialogs!.ConfirmAsync($" {DefaultConstants.MESSAGE_CONFIRM_DELETE} ");
                if (!confirm) return;
                await ShowLoader();
                bool isSuccess = await _masterDataService!.DeleteDataAsync(nameof(EnumTable.Inventory), "", string.Join(",", SelectedInvetoryHistory.Select(m => m.Absid)), pUserId);
                if (isSuccess)
                {
                    await getDataInv();
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "UserController", "DeleteDataInvHandler");
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
