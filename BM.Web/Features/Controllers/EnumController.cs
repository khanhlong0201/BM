using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Newtonsoft.Json;
using Telerik.Blazor.Components;

namespace BM.Web.Features.Controllers
{
    public class EnumController : BMControllerBase, IDisposable
    {
        #region Dependency Injection
        [Inject] private ILogger<EnumController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        #endregion
        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<EnumModel>? ListEnums { get; set; }
        public IEnumerable<EnumModel>? SelectedEnums { get; set; } = new List<EnumModel>();
        public EnumModel EnumUpdate { get; set; } = new EnumModel();
        public List<ComboboxModel>? ListTypeEnums { get; set; }
        public EditContext? _EditContext { get; set; }
        public bool IsShowDialog { get; set; }
        public bool IsCreate { get; set; } = true;
        public HConfirm? _rDialogs { get; set; }
        public string pEnumType = "";
        private string currentLocation = "";
        #endregion

        #region Override Functions
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                ListTypeEnums = new List<ComboboxModel>()
                {

                    new ComboboxModel() {Code = nameof(EnumType.@ServiceType), Name = "Loại dịch vụ"},
                    new ComboboxModel() {Code = nameof(EnumType.@SkinType), Name = "Loại da"}
                };
                pEnumType = nameof(EnumType.ServiceType);
                var uri = _navManager!.ToAbsoluteUri(_navManager.Uri);
                switch (uri.AbsolutePath?.ToUpper())
                {
                    case "/SERVICE-TYPE":
                        pEnumType = nameof(EnumType.@ServiceType);
                        break;
                    case "/SKIN-TYPE":
                        pEnumType = nameof(EnumType.@SkinType);
                        break;
                }
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Danh mục" },
                    new BreadcrumbModel() { Text = ListTypeEnums.FirstOrDefault(m=>m.Code == pEnumType)?.Name }
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                _navManager.LocationChanged += LocationChanged;

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
                    await getDataEnums();
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

        private async Task getDataEnums()
        {
            ListEnums = new List<EnumModel>();
            SelectedEnums = new List<EnumModel>();
            ListEnums = await _masterDataService!.GetDataEnumsAsync(pEnumType);
        }

        void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            // Cập nhật giá trị khi location thay đổi
            currentLocation = e.Location;
            if(currentLocation == "/service-type")
            {

            }
            else if (currentLocation == "/skin-type")
            { 
            }    
        }

        void IDisposable.Dispose()
        {
            // Unsubscribe from the event when our component is disposed
            _navManager!.LocationChanged -= LocationChanged;
        }
        #endregion

        #region Protected Functions
        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataEnums();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "EnumController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, EnumModel? pItemDetails = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                    IsCreate = true;
                    EnumUpdate = new EnumModel();
                    EnumUpdate.EnumType = pEnumType;
                }
                else
                {
                    EnumUpdate.EnumId = pItemDetails!.EnumId;
                    EnumUpdate.EnumName = pItemDetails!.EnumName;
                    EnumUpdate.EnumType = pItemDetails!.EnumType;
                    EnumUpdate.Description = pItemDetails!.Description;
                    IsCreate = false;
                }
                IsShowDialog = true;
                _EditContext = new EditContext(EnumUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "EnumController", "OnOpenDialogHandler");
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
                bool isSuccess = await _masterDataService!.UpdateEnumAsync(JsonConvert.SerializeObject(EnumUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataEnums();
                    if (pEnum == EnumType.SaveAndCreate)
                    {
                        EnumUpdate = new EnumModel();
                        _EditContext = new EditContext(EnumUpdate);
                        return;
                    }
                    IsShowDialog = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "EnumController", "SaveDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as EnumModel);

        #endregion

    }
}
