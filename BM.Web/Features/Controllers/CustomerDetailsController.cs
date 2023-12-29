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
    public class CustomerDetailsController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<CustomerDetailsController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        #region Properties
        public CustomerModel CustomerUpdate { get; set; } = new CustomerModel();
        public const string DATA_CUSTOMER_EMPTY = "Chưa cập nhật";
        public List<DocumentModel>? ListDocHis { get; set; }
        public List<ServiceCallModel>? ListServiceCalls { get; set; } // danh sách lịch sử dặm

        public double NumberOfConvertedPoints { get; set; } // Số điểm quy đổi
        public double NumberOfPointUsed { get; set; } // Số điểm sử dụng
        public bool IsShowPoint { get; set; }
        public List<CustomerDebtsModel>? ListPointByCusNo { get; set; }
        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }
        #endregion
        #endregion

        #region Override Functions
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney", Url = "trang-chu" },
                    new BreadcrumbModel() { Text = "Hồ sơ khách hàng", Url= "customer" },
                    new BreadcrumbModel() { Text = "Thông tin chi tiết" },
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                CustomerUpdate.BranchName = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.FullName = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.CINo = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Phone1 = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Zalo = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.FaceBook = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Address = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Remark = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.SkinType = DATA_CUSTOMER_EMPTY;
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
                    // đọc giá tri câu query
                    var uri = _navigationManager?.ToAbsoluteUri(_navigationManager.Uri);
                    if (uri != null && QueryHelpers.ParseQuery(uri.Query).Count > 0)
                    {
                        string key = uri.Query.Substring(5); // để tránh parse lỗi;    
                        Dictionary<string, string> pParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(EncryptHelper.Decrypt(key));
                        if (pParams != null && pParams.Any() && pParams.ContainsKey("pCusNo")) CustomerUpdate.CusNo = pParams["pCusNo"];
                    }    
                    await _progressService!.SetPercent(0.4);
                    if(string.IsNullOrWhiteSpace(CustomerUpdate.CusNo))
                    {
                        ShowWarning("Vui lòng tải lại trang hoặc liên hệ IT để được hổ trợ");
                        return;
                    }
                    var oCustomer = await _masterDataService!.GetCustomerByIdAsync(CustomerUpdate.CusNo);
                    if (oCustomer == null) return;
                    CustomerUpdate.BranchName = oCustomer.BranchName ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.FullName = oCustomer.FullName ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.CINo = oCustomer.CINo ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.Phone1 = oCustomer.Phone1 ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.Zalo = oCustomer.Zalo ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.FaceBook = oCustomer.FaceBook ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.Address = oCustomer.Address ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.Remark = oCustomer.Remark ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.SkinType = oCustomer.SkinType ?? DATA_CUSTOMER_EMPTY;
                    CustomerUpdate.TotalDebtAmount = oCustomer.TotalDebtAmount;
                    CustomerUpdate.Point = oCustomer.Point;

                    // call lấy danh sách chi tiết
                    ListDocHis = await _documentService!.GetDocByCusNoAsync(CustomerUpdate.CusNo + "");
                    SearchModel ItemFilter = new SearchModel();
                    ItemFilter.Type = nameof(EnumTable.Customers);
                    ItemFilter.CusNo = CustomerUpdate.CusNo;
                    ListServiceCalls = await _documentService!.GetServiceCallsAsync(ItemFilter);
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

        #region Protected Functions
        protected void OnViewDerailsHandler(int pDocEntry, string pLinkPage = "create-ticket")
        {
            try
            {
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pDocEntry", $"{pDocEntry}"},
                    { "pIsCreate", $"{false}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navigationManager!.NavigateTo($"/{pLinkPage}?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerDetailsController", "OnViewDerailsHandler");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// hiển thị popup
        /// </summary>
        /// <param name="pType"></param>
        protected async Task ShowPopupHandler(string pType = nameof(DocumentModel.Point))
        {
            try
            {
                if (pType == nameof(DocumentModel.Point))
                {
                    if (string.IsNullOrEmpty(CustomerUpdate.CusNo))
                    {
                        ShowWarning("Không tìm thấy thông tin khách hàng. Vui lòng tải lại trang!");
                        return;
                    }
                    await ShowLoader();
                    ListPointByCusNo = await _documentService!.GetCustomerDebtsByDocAsync(-1, nameof(EnumType.PointCustomer), CustomerUpdate.CusNo + "");
                    await Task.Delay(70);
                    NumberOfPointUsed = 0;
                    IsShowPoint = true;
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "ShowPopupHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Lưu thông tin qui đổi điểm để lấy quà.
        /// </summary>
        /// <returns></returns>
        protected async Task SaveNumberOfPointUsedHandler()
        {
            try
            {
                if (string.IsNullOrEmpty(CustomerUpdate.CusNo))
                {
                    ShowWarning("Không tìm thấy thông tin khách hàng. Vui lòng tải lại trang!");
                    return;
                }
                if (NumberOfPointUsed <= 0)
                {
                    ShowWarning("Vui lòng nhập số điểm cần quy đổi");
                    return;
                }
                bool isConfirm = await _rDialogs!.ConfirmAsync($"Bạn có chắc Quy đổi [{NumberOfPointUsed}] điểm của khách hàng [{CustomerUpdate.CusNo}] không ?", "Thông báo");
                if (!isConfirm) return;
                await ShowLoader();
                CustomerDebtsModel oItem = new CustomerDebtsModel();
                oItem.CusNo = CustomerUpdate.CusNo;
                oItem.DocEntry = -1;
                oItem.TotalDebtAmount = NumberOfPointUsed;
                oItem.GuestsPay = -1;
                oItem.Remark = $"Quy đổi {NumberOfPointUsed} điểm tại trang Thông tin chi tiết khách hàng";
                // call api 
                bool isSuccess = await _documentService!.UpdatePointByCusNoAsync(JsonConvert.SerializeObject(oItem), pUserId);
                if (isSuccess)
                {
                    IsShowPoint = false;
                    var oCustomer = await _masterDataService!.GetCustomerByIdAsync(CustomerUpdate.CusNo);
                    if (oCustomer == null) return;
                    CustomerUpdate.Point = oCustomer.Point;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "SaveNumberOfPointUsedHandler");
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
