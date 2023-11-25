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
    public class ServiceController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<ServiceController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<ServiceModel>? ListServices { get; set; }
        public IEnumerable<ServiceModel>? SelectedServices { get; set; } = new List<ServiceModel>();
        public ServiceModel ServiceUpdate { get; set; } = new ServiceModel();
        public EditContext? _EditContext { get; set; }
        public bool IsShowDialog { get; set; }
        public bool IsCreate { get; set; } = true;
        public List<EnumModel>? ListServicesType { get; set; } // ds loại dịch vụ
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
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Dịch vụ" }
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
                    await getDataServices();
                    ListServicesType = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.ServiceType));
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
        private async Task getDataServices()
        {
            ListServices = new List<ServiceModel>();
            SelectedServices = new List<ServiceModel>();
            ListServices = await _masterDataService!.GetDataServicesAsync();
        }

        #endregion

        #region Protected Functions
        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataServices();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, ServiceModel? pItemDetails = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                    IsCreate = true;
                    ServiceUpdate = new ServiceModel();
                }
                else
                {
                    ServiceUpdate.ServiceCode = pItemDetails!.ServiceCode;
                    ServiceUpdate.ServiceName = pItemDetails!.ServiceName;
                    ServiceUpdate.Price = pItemDetails!.Price;
                    ServiceUpdate.EnumId = pItemDetails!.EnumId;
                    ServiceUpdate.EnumName = pItemDetails!.EnumName;
                    ServiceUpdate.WarrantyPeriod = pItemDetails!.WarrantyPeriod;
                    ServiceUpdate.QtyWarranty = pItemDetails!.QtyWarranty;
                    ServiceUpdate.Description = pItemDetails!.Description;
                    IsCreate = false;
                }
                IsShowDialog = true;
                _EditContext = new EditContext(ServiceUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "OnOpenDialogHandler");
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
                bool isSuccess = await _masterDataService!.UpdateServiceAsync(JsonConvert.SerializeObject(ServiceUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataServices();
                    if (pEnum == EnumType.SaveAndCreate)
                    {
                        ServiceUpdate = new ServiceModel();
                        _EditContext = new EditContext(ServiceUpdate);
                        return;
                    }
                    IsShowDialog = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "SaveDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }
        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as ServiceModel);

        #endregion
    }
}
