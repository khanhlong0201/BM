using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System.Linq;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace BM.Web.Features.Controllers
{
    public class CustomerController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<CustomerController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        #endregion
        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<CustomerModel>? ListCustomers { get; set; }
        public IEnumerable<CustomerModel>? SelectedCustomers { get; set; } = new List<CustomerModel>();
        public CustomerModel CustomerUpdate { get; set; } = new CustomerModel();
        public EditContext? _EditContext { get; set; }
        public bool IsShowDialog { get; set; }
        public bool IsCreate { get; set; } = true;
        public List<BranchModel>? ListBranchs { get; set; }
        public List<ComboboxModel>? ListSkinsType { get; set; } // ds loại da
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
                    new BreadcrumbModel() { Text = "Hồ sơ khách hàng" },
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
                    await getDataCustomers();
                    ListBranchs = await _masterDataService!.GetDataBranchsAsync();
                    var listSkinsType = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.SkinType));
                    if(listSkinsType !=null && listSkinsType.Any())
                    {
                        ListSkinsType = listSkinsType.Select(m=> new ComboboxModel()
                        {
                            Code = m.EnumId,
                            Name = m.EnumName,
                        }).ToList();
                    }    

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

        private async Task getDataCustomers()
        {
            ListCustomers = new List<CustomerModel>();
            SelectedCustomers = new List<CustomerModel>();
            ListCustomers = await _masterDataService!.GetDataCustomersAsync();
        }

        #endregion

        #region Protected Functions

        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataCustomers();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, CustomerModel? pItemDetails = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                    if (ListSkinsType != null && ListSkinsType.Any()) ListSkinsType.ForEach(item => item.IsCheck = false);
                    IsCreate = true;
                    CustomerUpdate = new CustomerModel();
                }
                else
                {
                    CustomerUpdate.CusNo = pItemDetails!.CusNo;
                    CustomerUpdate.FullName = pItemDetails.FullName;
                    CustomerUpdate.Phone1 = pItemDetails.Phone1;
                    CustomerUpdate.Phone2 = pItemDetails.Phone2;
                    CustomerUpdate.CINo = pItemDetails.CINo;
                    CustomerUpdate.Email = pItemDetails.Email;
                    CustomerUpdate.FaceBook = pItemDetails.FaceBook;
                    CustomerUpdate.Zalo = pItemDetails.Zalo;
                    CustomerUpdate.Address = pItemDetails.Address;
                    CustomerUpdate.DateOfBirth = pItemDetails.DateOfBirth;
                    CustomerUpdate.SkinType = pItemDetails.SkinType;
                    CustomerUpdate.BranchId = pItemDetails.BranchId;
                    CustomerUpdate.Remark = pItemDetails.Remark;
                    CustomerUpdate.DateCreate = pItemDetails.DateCreate;
                    CustomerUpdate.UserCreate = pItemDetails.UserCreate;
                    var lstSkinType = JsonConvert.DeserializeObject<List<string>>(pItemDetails.SkinType + "");
                    if (lstSkinType != null && lstSkinType.Any() && ListSkinsType != null && ListSkinsType.Any())
                    {
                        ListSkinsType.ForEach(item =>
                        {
                            if (lstSkinType.Contains(item.Code + "")) item.IsCheck = true;
                        });
                    }    
                    IsCreate = false;
                }
                IsShowDialog = true;
                _EditContext = new EditContext(CustomerUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerController", "OnOpenDialogHandler");
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
                string skinType = JsonConvert.SerializeObject(ListSkinsType?.Where(m => m.IsCheck).Select(m => m.Code).ToArray());
                CustomerUpdate.SkinType = skinType;
                bool isSuccess = await _masterDataService!.UpdateCustomerAsync(JsonConvert.SerializeObject(CustomerUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataCustomers();
                    if (pEnum == EnumType.SaveAndCreate)
                    {
                        CustomerUpdate = new CustomerModel();
                        _EditContext = new EditContext(CustomerUpdate);
                        return;
                    }
                    IsShowDialog = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerController", "SaveDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as CustomerModel);

        protected void CreateTicketHandler()
        {
            try
            {
                if(SelectedCustomers == null || !SelectedCustomers.Any())
                {
                    ShowWarning("Vui lòng chọn Khách hàng cần Lập đơn hàng");
                    return;
                }    
                if(SelectedCustomers.ToList().Count > 1)
                {
                    ShowWarning("Chỉ được phép chọn 1 Khách hàng để Lập đơn hàng");
                    return;
                }
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pCusNo", $"{SelectedCustomers.First().CusNo}" },
                    { "pIsCreate", $"{true}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navManager!.NavigateTo($"/create-ticket?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerController", "CreateTicketHandler");
                ShowError(ex.Message);
            }
        }    
        #endregion
    }
}
