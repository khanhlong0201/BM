using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace BM.Web.Features.Controllers
{
    public class DocumentController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<DocumentController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }
        #endregion

        #region Properties
        public double TotalDue { get; set; } = 0.0;
        public CustomerModel CustomerUpdate { get; set; } = new CustomerModel();
        public List<SalesOrderModel>? ListSalesOrder { get; set; } // ds đơn hàng
        public IEnumerable<IGrouping<string, ServiceModel>>? ListGroupServices { get; set; }
        public HConfirm? _rDialogs { get; set; }
        //
        public bool pIsCreate { get; set; } = false;
        public const string DATA_CUSTOMER_EMPTY = "Chưa cập nhật";
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
                    new BreadcrumbModel() { Text = "Lập đơn hàng" }
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                CustomerUpdate.CusNo = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.BranchName = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.FullName = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.CINo = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Phone1 = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Zalo = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.FaceBook = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Address = DATA_CUSTOMER_EMPTY;
                CustomerUpdate.Remark = DATA_CUSTOMER_EMPTY;
                // đọc giá tri câu query
                var uri = _navigationManager?.ToAbsoluteUri(_navigationManager.Uri);
                if (uri != null && QueryHelpers.ParseQuery(uri.Query).Count > 0)
                {
                    string key = uri.Query.Substring(5); // để tránh parse lỗi;    
                    Dictionary<string, string> pParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(EncryptHelper.Decrypt(key));
                    if(pParams != null && pParams.Any())
                    {
                        if (pParams.ContainsKey("pCusNo")) CustomerUpdate.CusNo = pParams["pCusNo"];
                        if (pParams.ContainsKey("pIsCreate")) pIsCreate = Convert.ToBoolean(pParams["pIsCreate"]);
                    }    
                }    
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
                    // lấy thông tin khách hàng
                    if(pIsCreate)
                    {
                        var oCustomer = await _masterDataService!.GetCustomerByIdAsync(CustomerUpdate.CusNo + "");
                        if (oCustomer == null) return;
                        CustomerUpdate.BranchName = oCustomer.BranchName ?? DATA_CUSTOMER_EMPTY;
                        CustomerUpdate.FullName = oCustomer.FullName ?? DATA_CUSTOMER_EMPTY;
                        CustomerUpdate.CINo = oCustomer.CINo ?? DATA_CUSTOMER_EMPTY;
                        CustomerUpdate.Phone1 = oCustomer.CINo ?? DATA_CUSTOMER_EMPTY;
                        CustomerUpdate.Zalo = oCustomer.FaceBook ?? DATA_CUSTOMER_EMPTY;
                        CustomerUpdate.FaceBook = oCustomer.FaceBook ?? DATA_CUSTOMER_EMPTY;
                        CustomerUpdate.Address = oCustomer.Address ?? DATA_CUSTOMER_EMPTY;
                        CustomerUpdate.Remark = oCustomer.Remark ?? DATA_CUSTOMER_EMPTY;

                    }    
                    await _progressService!.SetPercent(0.6);
                    // lấy danh sách dịch vụ
                    var listServices = await _masterDataService!.GetDataServicesAsync();
                    if(listServices != null && listServices.Any())
                    {
                        ListGroupServices = listServices.GroupBy(m => $"{m.EnumName}");
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
        #endregion

        #region Protected Functions

        /// <summary>
        /// add item to Grid Sales Order
        /// </summary>
        protected void AddItemToSOHandler(ServiceModel oService)
        {
            try
            {
                if (ListSalesOrder == null) ListSalesOrder = new List<SalesOrderModel>();
                var oItemExists = ListSalesOrder.FirstOrDefault(m => m.ServiceCode == oService.ServiceCode);
                if(oItemExists != null) oItemExists.Qty = oItemExists.Qty + 1;
                else
                {
                    SalesOrderModel oItem = new SalesOrderModel();
                    oItem.Qty = 1;
                    oItem.PriceOld = oService.Price;
                    oItem.Price = oService.Price;
                    oItem.ServiceCode = oService.ServiceCode + "";
                    oItem.ServiceName = oService.ServiceName + "";
                    ListSalesOrder.Add(oItem);
                }

                // đánh lại số thứ tự
                TotalDue = 0.0;
                for (int i = 0; i < ListSalesOrder.Count(); i++)
                {
                    ListSalesOrder[i].Id = (i + 1);
                    TotalDue += ListSalesOrder[i].Amount;
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "AddItemToSOHandler");
                ShowError(ex.Message);
            }
        }

        protected async void SaveDocHandler()
        {
            try
            {
                if(pIsCreate)
                {
                    if (string.IsNullOrEmpty(CustomerUpdate.CusNo))
                    {
                        ShowWarning("Không tìm thấy thông tin nhân viên!");
                        return;
                    }
                    if(ListSalesOrder == null || ListSalesOrder.Any())
                    {
                        ShowWarning("Vui lòng chọn dịch vụ!");
                        return;
                    }    
                }    
                else
                {

                }    
                
                await ShowLoader();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "SaveDocHandler");
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
