using BM.Models;
using BM.Web.Commons;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Resources;

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
        public List<EnumModel>? ListPackages { get; set; } // ds gói dịch vụ vụ
        public bool IsShowDialogPriceList { get; set; }
        public List<PriceModel>? ListPrices { get; set; }
        public IEnumerable<PriceModel>? SelectedPrices { get; set; } = new List<PriceModel>();
        public PriceModel PriceUpdate { get; set; } = new PriceModel();
        public string pServiceCode { get; set; } = "";
        public bool IsShowDialogTreatment { get; set; }
        public List<TreatmentRegimenModel>? ListTreatments { get; set; }
        public TelerikGrid<TreatmentRegimenModel>? RefListTreatments { get; set; }
        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }

        public List<SuppliesModel>? ListSupplies { get; set; } = new List<SuppliesModel>();
        public SearchModel ItemFilter { get; set; } = new SearchModel();
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
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                try
                {
                    await _progressService!.SetPercent(0.4);
                    await getDataServices();
                    ListServicesType = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.ServiceType));
                    ListPackages = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.ServicePack));

                    ItemFilter.Type = nameof(SuppliesKind.Promotion);//khuyến mãi
                    ListSupplies = await _masterDataService!.GetDataSuppliesAsync(ItemFilter);
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
            string loadAll = pIsAdmin ? "ALL" : nameof(EnumTable.Services);
            ListServices = await _masterDataService!.GetDataServicesAsync(pBranchId, pUserId, pLoadAll: loadAll);
        }

        private async Task getDataPricesList()
        {
            ListPrices = new List<PriceModel>();
            SelectedPrices = new List<PriceModel>();
            PriceUpdate = new PriceModel();
            ListPrices = await _masterDataService!.GetDataPricesByServiceAsync(pServiceCode);
        }

        private async Task getDataTreatmentRegimens()
        {
            ListTreatments = new List<TreatmentRegimenModel>();
            ListTreatments = await _masterDataService!.GetDataTreatmentsByServiceAsync(pServiceCode);   
        }
        #endregion

        #region Protected Functions
        protected async void ReLoadDataHandler(EnumTable pTable = EnumTable.Services)
        {
            try
            {
                switch(pTable)
                {
                    case EnumTable.Services:
                        IsInitialDataLoadComplete = false;
                        await getDataServices();
                        break;
                    case EnumTable.Prices:
                        await ShowLoader();
                        await getDataPricesList();
                        break;
                    case EnumTable.TreatmentRegimens:
                        await ShowLoader();
                        await getDataTreatmentRegimens();
                        break;
                    default:
                        break;
                }    
                
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await ShowLoader(false);
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
                    ServiceUpdate.PackageId = pItemDetails!.PackageId +"";
                    ServiceUpdate.EnumName = pItemDetails!.EnumName;
                    ServiceUpdate.WarrantyPeriod = pItemDetails!.WarrantyPeriod;
                    ServiceUpdate.QtyWarranty = pItemDetails!.QtyWarranty;
                    ServiceUpdate.Description = pItemDetails!.Description;
                    ServiceUpdate.ListPromotionSuppliess = pItemDetails.ListPromotionSupplies?.Split(",")?.ToList(); // ldv
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
                ServiceUpdate.ListPromotionSupplies = ServiceUpdate.ListPromotionSuppliess == null || !ServiceUpdate.ListPromotionSuppliess.Any() ? "" : string.Join(",", ServiceUpdate.ListPromotionSuppliess);
                await ShowLoader();
                bool isSuccess = await _masterDataService!.UpdateServiceAsync(JsonConvert.SerializeObject(ServiceUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataServices();
                    if (pEnum == EnumType.SaveAndCreate)
                    {
                        ServiceUpdate = new ServiceModel();
                        _EditContext = new EditContext(ServiceUpdate);
                        IsCreate = true;
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

        #region Bảng giá
        protected async void EditPriceHandler(ServiceModel pService)
        {
            try
            {
                if (pService == null) return;    
                await ShowLoader();
                pServiceCode = pService.ServiceCode + "";
                await getDataPricesList();
                IsShowDialogPriceList = true;
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "EditPriceHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// cập nhật thông tin bảng giá
        /// </summary>
        /// <param name="pEnum"></param>
        protected async void SaveDataPriceListHandler(EnumType pEnum = EnumType.Add)
        {
            try
            {
                string sAction = "";
                string sMessage = "";
                if (pEnum == EnumType.Add)
                {
                    sAction = nameof(EnumType.Add);
                    sMessage = PriceUpdate.IsActive ? "Thêm thông tin bảng giá và sử dụng đơn giá này" : "Thêm thông tin bảng giá";
                    if(PriceUpdate.Id > 0)
                    {
                        ShowWarning("Vui lòng làm mới lại dữ liệu trước khi thêm mới đơn giá");
                        return;
                    }    
                }    
                else
                {
                    if(PriceUpdate.Id <= 0)
                    {
                        ShowWarning("Vui lòng chọn dòng cần cập nhật");
                        return;
                    }
                    sAction = nameof(EnumType.Update);
                    sMessage = PriceUpdate.IsActive ? "Sử dụng đơn giá này" : "Hủy sử dụng đơn giá này";
                }    
                if (PriceUpdate.Price <= 0)
                {
                    ShowWarning("Vui lòng nhập đơn giá");
                    return;
                }
                var isConfirm = await _rDialogs!.ConfirmAsync($" Bạn có chắc muốn {sMessage} ?", "Thông báo");
                if (!isConfirm) return;
                await ShowLoader();
                PriceUpdate.ServiceCode = pServiceCode;
                bool isSuccess = await _masterDataService!.UpdatePriceAsync(JsonConvert.SerializeObject(PriceUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataPricesList();
                    await getDataServices();
                }    
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "SaveDataPriceListHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnRowClickPriceHandler(GridRowClickEventArgs args)
        {
            try
            {
                var oPrice = args.Item as PriceModel;
                PriceUpdate.Id = oPrice!.Id; 
                PriceUpdate.Price = oPrice.Price;
                PriceUpdate.IsActive = oPrice.IsActive;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "OnRowClickPriceHandler");
                ShowError(ex.Message);
            }
        }
        #endregion

        #region Phát đồ điều trị
        protected async void EditTreatmentRegimen(ServiceModel pService)
        {
            try
            {
                if (pService == null) return;
                await ShowLoader();
                pServiceCode = pService.ServiceCode + "";
                await getDataTreatmentRegimens();
                IsShowDialogTreatment = true;
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "EditTreatmentRegimen");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async void AddTreatmentRegimenHandler()
        {
            try
            {
                if (ListTreatments == null) ListTreatments = new List<TreatmentRegimenModel>();
                ListTreatments.Add(new TreatmentRegimenModel() { ServiceCode = pServiceCode });
                // đánh lại số thứ tự
                for (int i = 0; i < ListTreatments.Count; i++) { ListTreatments[i].LineNum = (i + 1); }
                RefListTreatments?.Rebind();
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "AddTreatmentRegimenHandler");
                ShowError(ex.Message);
            }
        }

        protected void RemoveTreatmentRegimenHandler(int pId)
        {
            try
            {
                var oItem = ListTreatments!.FirstOrDefault(m => m.LineNum == pId);
                if (oItem != null)
                {
                    ListTreatments!.Remove(oItem);
                    // đánh lại số thứ tự
                    for (int i = 0; i < ListTreatments.Count; i++) { ListTreatments[i].LineNum = (i + 1); }
                    RefListTreatments?.Rebind();
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "AddItemToSOHandler");
                ShowError(ex.Message);
            }
        }

        protected void UpdateTreatmentRegimenHandler(GridCommandEventArgs args)
        {
            try
            {
                TreatmentRegimenModel oItem = (args.Item as TreatmentRegimenModel)!;
                var target = ListTreatments!.FirstOrDefault(m=>m.LineNum == oItem.LineNum);
                if (target == null) return;
                target.Name = oItem.Name;
                target.Title = oItem.Title;
                RefListTreatments?.Rebind();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "AddItemToSOHandler");
                ShowError(ex.Message);
            }
        }

        protected async void SaveAndClosedTreatmentHandler()
        {
            try
            {
                if(ListTreatments == null || !ListTreatments.Any())
                {
                    ShowWarning("Vui lòng khai báo Phác đồ thực hiện cho dịch vụ!");
                    return;
                }
                if (ListTreatments.Any(m => string.IsNullOrWhiteSpace(m.Name) || string.IsNullOrWhiteSpace(m.Title)))
                {
                    ShowWarning("Vui lòng điền đẩy đủ thông tin [Bước] và [Nội dung] thực hiện!");
                    return;
                }
                var isConfirm = await _rDialogs!.ConfirmAsync($" Bạn có chắc muốn lưu thông tin Phác đồ điều trị ?", "Thông báo");
                if (!isConfirm) return;
                await ShowLoader();
                PriceUpdate.ServiceCode = pServiceCode;
                bool isSuccess = await _masterDataService!.UpdateTreatmentRegimenAsync(JsonConvert.SerializeObject(ListTreatments), nameof(EnumType.Add), pUserId);
                if (isSuccess)
                {
                    await getDataTreatmentRegimens();
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceController", "SaveAndClosedTreatmentHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async void DeleteDataHandler()
        {
            try
            {
                if (SelectedServices == null || !SelectedServices.Any())
                {
                    ShowWarning(DefaultConstants.MESSAGE_NO_CHOSE_DATA);
                    return;
                }
                var confirm = await _rDialogs!.ConfirmAsync($" {DefaultConstants.MESSAGE_CONFIRM_DELETE} ");
                if (!confirm) return;
                await ShowLoader();
                bool isSuccess = await _masterDataService!.DeleteDataAsync(nameof(EnumTable.Services), "", string.Join(",", SelectedServices.Select(m => m.ServiceCode)), pUserId);
                if (isSuccess)
                {
                    await getDataServices();
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
        #endregion

        #endregion
    }
}
