﻿using BM.Models;
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
        [Inject] private ICliDocumentService? _documentService { get; init; }
        #endregion
        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<EnumModel>? ListEnums { get; set; } //đơn vị tính
        public List<EnumModel>? ListSuppliesTypes { get; set; } //Loại vật tư
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
        public SearchModel ItemFilter { get; set; } = new SearchModel();

        public List<ComboboxModel>? ListKinds { get; set; } //kiểu của vât tư

        public bool IsShowOutBound { get; set; } = false;
        public EditContext? _EditOutBoundContext { get; set; }
        public OutBoundModel OutBoundUpdate { get; set; } = new OutBoundModel();
        public IEnumerable<ComboboxModel>? ListUsers { get; set; } // danh sách nhân viên
        public List<SuppliesModel>? ListSuppliesOutBound { get; set; }

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
                    ListSuppliesTypes = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.@SuppliesType));
                    ListKinds = new List<ComboboxModel>()
                    {
                        new ComboboxModel() {Code = nameof(SuppliesKind.@Popular), Name = "Phổ thông"},
                        new ComboboxModel() {Code = nameof(SuppliesKind.@Promotion), Name = "Khuyến mãi"},
                        new ComboboxModel() {Code = nameof(SuppliesKind.@Ink), Name = "Mực - Loại Tê"},
                    };

                    // danh sách nhân viên
                    var listUsers = await _masterDataService!.GetDataUsersAsync();
                    if (listUsers != null && listUsers.Any()) ListUsers = listUsers.Select(m => new ComboboxModel()
                    {
                        Code = m.EmpNo,
                        Name = $"{m.EmpNo}-{m.FullName}"
                    });

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
            
            ListSupplies = await _masterDataService!.GetDataSuppliesAsync(ItemFilter);
        }

        #endregion

        #region Protected Functions
        protected async void ShowOutBoundHandler(EnumType pAction = EnumType.Add, OutBoundModel? pOutBound = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                   
                    await ShowLoader();

                    OutBoundUpdate.BranchName = pBranchName;
                    OutBoundUpdate.FullName = FullName;
                    OutBoundUpdate.BranchId = pBranchId;
                    OutBoundUpdate.Type = nameof(OutBoundType.ByRequest);
                }
                else
                {

                }
                ItemFilter.Type = nameof(SuppliesKind.Promotion) + "," + nameof(SuppliesKind.Ink);
                ListSuppliesOutBound = await _masterDataService!.GetDataSuppliesAsync(ItemFilter);
                IsShowOutBound = true;
                _EditOutBoundContext = new EditContext(OutBoundUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "ShowOutBound");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async void SaveOutBoundHandler(EnumType pProcess = EnumType.Update)
        {
            try
            {
                string sAction = nameof(EnumType.Add);
                //bool isConfirm = false;
                //isConfirm = await _rDialogs!.ConfirmAsync($"Bạn có chắc muốn lưu phiếu xuất này ?", "Thông báo");
                //if (!isConfirm) return;
                await ShowLoader();
                if (ListSuppliesOutBound != null && ListSuppliesOutBound.Any())
                {
                    var CheckListSupplies = ListSuppliesOutBound.Where(d => d.Qty > d.QtyInv).FirstOrDefault();
                    if (CheckListSupplies != null)
                    {
                        ShowWarning("Số lượng xuất phải <= Tổng số lượng tồn kho");
                        await ShowLoader(false);
                        return;
                    }
                    var listSuppliesOutBound = ListSuppliesOutBound.Select(m => new SuppliesOutBoundModel()
                    {
                        SuppliesCode = m.SuppliesCode,
                        SuppliesName = m.SuppliesName,
                        EnumId = m.EnumId,
                        EnumName = m.EnumName,
                        Qty = m.SuppliesCode =="VT008" ?m.Qty/200: m.Qty,
                        QtyInv = m.QtyInv
                    });
                    OutBoundUpdate.ChargeUser = OutBoundUpdate.ListChargeUser == null || !OutBoundUpdate.ListChargeUser.Any() ? "" : string.Join(",", OutBoundUpdate.ListChargeUser);
                    OutBoundUpdate.SuppliesQtyList = JsonConvert.SerializeObject(listSuppliesOutBound);
                }
                bool isSuccess = await _documentService!.UpdateOutBound(JsonConvert.SerializeObject(OutBoundUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    IsShowOutBound = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "SaveDocHandler");
                IsShowOutBound = false;
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

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
                if(ListSupplies == null || !ListSupplies.Any())
                {
                    ShowWarning("Không tìm thấy thông tin Vật tư. Vui lòng làm mới mại trang!");
                    return;
                }    
                IsShowIntoInv = true;
                ListInvetoryCreate = new List<InvetoryModel>();
                foreach(var item in ListSupplies)
                {
                    ListInvetoryCreate.Add(new InvetoryModel { SuppliesCode = item.SuppliesCode, SuppliesName = item.SuppliesName, EnumId = item.EnumId,EnumName= item.EnumName });
                }
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
                if (ListSupplies != null && ListSupplies.Count > 0)
                {
                    var checkTrungTen = ListSupplies.Where(d => d.SuppliesName == SuppliesUpdate.SuppliesName).FirstOrDefault();
                    if (checkTrungTen != null)
                    {
                        ShowWarning($"Tên vật tư [{SuppliesUpdate.SuppliesName}] đã tồn tại. Vui lòng nhập tên khác !");
                        return;
                    }
                }
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


        /// <summary>
        /// lưu nhập kho
        /// </summary>
        /// <param name="pEnum"></param>
        protected async void SaveDataInvHandler(EnumType pEnum = EnumType.SaveAndClose)
        {
            try
            {
                string sAction = IsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
                if (ListInvetoryCreate == null || ListInvetoryCreate.Count == 0)
                {
                    ShowWarning("Không có dòng dữ liệu nào để lưu tồn kho");
                    return;
                }

                var listCheckSave = ListInvetoryCreate.Where(d => d.QtyInv > 0 && d.Price > 0).FirstOrDefault();
                if (listCheckSave == null)
                {
                    ShowWarning("Bạn phải nhận [Số lượng] và [Giá] để có để lưu được tồn kho");
                    return;
                }

                var listCheck = ListInvetoryCreate.Where(d => d.QtyInv <= 0 || d.Price <= 0 || d.QtyInv == null  || d.Price == null).FirstOrDefault();
                var confirm = listCheck !=null ? await _rDialogs!.ConfirmAsync($" {"Những dòng dữ liệu có [Số lượng] <= 0 hoặc [Giá] <=0 sẽ không được lưu, Bạn có chắc muốn lưu?"} ")  : await _rDialogs!.ConfirmAsync($" {"Bạn có chắc muốn lưu những dòng đã chọn"} ");
                if (!confirm) return;

                List<InvetoryModel>? listInvetoryCreate = ListInvetoryCreate.Where(d => d.QtyInv > 0 && d.Price > 0).ToList();
                InvetoryHistoryUpdate.BranchId = pBranchId;
                await ShowLoader();
                bool isSuccess = await _masterDataService!.UpdateInvetoryAsync(JsonConvert.SerializeObject(InvetoryHistoryUpdate), JsonConvert.SerializeObject(listInvetoryCreate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataInv();
                    await getData();
                    if (pEnum == EnumType.SaveAndCreate)
                    {
                        ListInvetoryCreate = new List<InvetoryModel>();
                        return;
                    }

                    var listUsers = await _masterDataService!.GetDataUsersAsync();
                    if (listUsers != null && listUsers.Any()) ListUsers = listUsers.Select(m => new ComboboxModel()
                    {
                        Code = m.EmpNo,
                        Name = $"{m.EmpNo}-{m.FullName}"
                    });
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
