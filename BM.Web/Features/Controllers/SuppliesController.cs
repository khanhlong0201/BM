using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Telerik.Blazor.Components;

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
        public List<InvetoryModel>? ListInvetoryHistory { get; set; }
        public List<InvetoryModel>? ListInvetoryCreate { get; set; }
        public InvetoryModel SelectedInvetoryCreate { get; set; } = new InvetoryModel();
        public bool IsShowDialog { get; set; }
        public bool IsShowIntoInv { get; set; }
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
                    new BreadcrumbModel() { Text = "Kho" },
                    new BreadcrumbModel() { Text = "Vật tư- tồn kho" }
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
            if (firstRender)
            {
                try
                {
                    await _progressService!.SetPercent(0.4);
                    ListEnums = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.Unit));
                    await getData();

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
        private async Task getData()
        {
            ListSupplies = new List<SuppliesModel>();
            SelectedSupplies = new List<SuppliesModel>();
            ListSupplies = await _masterDataService!.GetDataSuppliesAsync();
        }

        #endregion

        #region Protected Functions
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

        protected async void SaveDataHandler(EnumType pEnum = EnumType.SaveAndClose)
        {
            try
            {
                string sAction = IsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
                var checkData = _EditContext!.Validate();
                if (!checkData) return;
                await ShowLoader();
                bool isSuccess = await _masterDataService!.UpdateSuppliesAsync(JsonConvert.SerializeObject(SuppliesUpdate), sAction, pUserId);
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
        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as SuppliesModel);

        #endregion
    }
}
