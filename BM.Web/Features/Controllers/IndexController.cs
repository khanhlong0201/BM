using Blazored.LocalStorage;
using BM.Models;
using BM.Models.Shared;
using BM.Web.Commons;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace BM.Web.Features.Controllers
{
    public class IndexController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<IndexController>? _logger { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] NavigationManager? _navigationManager { get; set; }
        #endregion

        #region Properties
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        public SchedulerView CurrView { get; set; } = SchedulerView.Month;
        public IEnumerable<SheduleModel>? ListSchedulers { get; set; }
        public bool IsShowDetails { get; set; }
        public SheduleModel ItemSelected = new SheduleModel();
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
                    new BreadcrumbModel() { Text = "Nhắc nợ & liệu trình" }
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
                    StartDate = _dateTimeService!.GetCurrentVietnamTime();
                    StartTime = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, 23,0,0);
                    await _progressService!.SetPercent(0.4);
                    await getDataRemiderByMonth();
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

        private async Task getDataRemiderByMonth()
        {
            ListSchedulers = new List<SheduleModel>();
            SearchModel pSearch = new SearchModel();
            pSearch.FromDate = new DateTime(StartDate.Year, StartDate.Month - 1, 23); // lấy tháng trươc liền kề ngày 23
            pSearch.ToDate = new DateTime(StartDate.Year, StartDate.Month, 1).AddMonths(1).AddDays(7); // lấy tháng này + 1 tháng và 7 ngày tiếp
            ListSchedulers = await _documentService!.GetDataReminderByMonthAsync(pSearch);
        }

        #endregion

        #region Protected Functions
        protected void OnItemRender(SchedulerItemRenderEventArgs args)
        {
            try
            {
                var oItem = (args.Item as SheduleModel);
                if (oItem == null) return;
                switch (oItem.Type)
                {
                    case "DebtReminder": // nhắc nợ
                        args.Class = "bg-red text-red-fg";
                        break;
                    case "TreatmentReminder": // nhắc liệu trình
                        args.Class = "bg-teal text-teal-fg";
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "IndexController", "IndexController");
                ShowError(ex.Message);
            }
        }

        protected void OnItemDoubleClick(SchedulerItemDoubleClickEventArgs args)
        {
            try
            {
                ItemSelected = new SheduleModel();
                ItemSelected = (args.Item as SheduleModel)!;
                ItemSelected.GuestsPay = ItemSelected.TotalDebtAmount;
                ItemSelected.Remark = string.Empty;
                IsShowDetails = true;
                
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "IndexController", "OnItemDoubleClick");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// xem chi tiết khách hàng
        /// </summary>
        /// <param name="pCusNo"></param>
        protected void ReviewCustomerInfoHandler(string? pCusNo)
        {
            try
            {
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pCusNo", $"{pCusNo}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navigationManager!.NavigateTo($"/customer-details?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "IndexController", "ReviewCustomerInfoHandler");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// xem chi tiết khách hàng
        /// </summary>
        /// <param name="pCusNo"></param>
        protected void ReviewDocInfoHandler(int pDocEntry)
        {
            try
            {
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pDocEntry", $"{pDocEntry}"},
                    { "pIsCreate", $"{false}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navigationManager!.NavigateTo($"/create-ticket?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "IndexController", "ReviewCustomerInfoHandler");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// lưu thông tin thanh toán
        /// </summary>
        protected async void SavePaymentHandler()
        {
            try
            {
                if(ItemSelected.GuestsPay <=0)
                {
                    ShowWarning("Vui lòng nhập số tiền khách trả!");
                    return;
                }
                string messageDept = "";
                if (ItemSelected.GuestsPay < ItemSelected.TotalDebtAmount)
                {
                    messageDept = $"Vẫn còn nợ {string.Format(DefaultConstants.FORMAT_GRID_CURRENCY, (ItemSelected.TotalDebtAmount - ItemSelected.GuestsPay))}đ." +
                        $" Số tiền sẽ được lưu vào công nợ của khách hàng [{ItemSelected.FullName}].";
                }
                bool isConfirm = await _rDialogs!.ConfirmAsync($"{messageDept} Bạn có chắc muốn thanh toán đơn hàng này?", "Thông báo");
                if (!isConfirm) return;
                await ShowLoader();
                CustomerDebtsModel oItem = new CustomerDebtsModel();
                oItem.CusNo = ItemSelected.CusNo;
                oItem.DocEntry = ItemSelected.DocEntry;
                oItem.TotalDebtAmount = ItemSelected.TotalDebtAmount - ItemSelected.GuestsPay;
                oItem.GuestsPay = ItemSelected.GuestsPay;
                oItem.Remark = ItemSelected.Remark;
                // call api 
                bool isSuccess = await _documentService!.UpdateCustomerDebtsAsync(JsonConvert.SerializeObject(oItem), pUserId);
                if (isSuccess)
                {
                    await getDataRemiderByMonth();
                    IsShowDetails = false;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "IndexController", "SavePaymentHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await InvokeAsync(StateHasChanged);
                await ShowLoader(false);
            }
        }
        #endregion

    }
}
