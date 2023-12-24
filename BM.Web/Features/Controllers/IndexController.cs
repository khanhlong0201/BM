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

        [CascadingParameter]
        public EventCallback<List<SheduleModel>> NotifySheduler { get; set; }
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
            await base.OnAfterRenderAsync(firstRender);
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
            pSearch.BranchId = pBranchId;
            ListSchedulers = await _documentService!.GetDataReminderByMonthAsync(pSearch);

            // lấy tất cả các thông báo nhắc nở/ liệu trình ngày hiện tại
            DateTime toDay = _dateTimeService!.GetCurrentVietnamTime();
            var listShedulersToday = ListSchedulers?.Where(m => m.Start.Date == toDay.Date);
            if(listShedulersToday != null && listShedulersToday.Any()) await NotifySheduler.InvokeAsync(listShedulersToday.ToList());
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
                    case nameof(EnumType.DebtReminder): // nhắc nợ
                        args.Class = "bg-red text-red-fg";
                        break;
                    case nameof(EnumType.WarrantyReminder): // nhắc bảo hành
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
                bool isConfirm = false;
                if (ItemSelected.Type == nameof(EnumType.DebtReminder))
                {
                    if (ItemSelected.GuestsPay <= 0)
                    {
                        ShowWarning("Vui lòng nhập số tiền khách trả!");
                        return;
                    }
                    string messageDept = string.Empty;
                    if (ItemSelected.IsDelay)
                    {
                        // nếu khách hẹn lại khi khác
                        messageDept = ItemSelected.DateDelay == null ? "Nếu không chọn ngày hẹn. Lịch nhắc nợ sẽ được nhắc vào tháng sau."
                            : $" Khách hẹn lại ngày [{ItemSelected.DateDelay.Value.ToString(DefaultConstants.FORMAT_DATE)}]";
                    }
                    else if (ItemSelected.GuestsPay < ItemSelected.TotalDebtAmount)
                    {
                        messageDept = $"Vẫn còn nợ {string.Format(DefaultConstants.FORMAT_GRID_CURRENCY, (ItemSelected.TotalDebtAmount - ItemSelected.GuestsPay))}đ." +
                            $" Số tiền sẽ được lưu vào công nợ của khách hàng [{ItemSelected.FullName}].";
                    }

                    isConfirm = await _rDialogs!.ConfirmAsync($"{messageDept} Bạn có chắc muốn lưu thông tin thanh toán đơn hàng này?", "Thông báo");
                }
                else if (ItemSelected.Type == nameof(EnumType.WarrantyReminder))
                {
                    if(!ItemSelected.IsDelay)
                    {
                        // cho đồng bộ với thanh toán nợ
                        ShowWarning("Vui lòng check vào ô Khách hẹn lại!");
                        return;
                    }    
                    string messageDept = string.Empty;
                    if (ItemSelected.IsDelay)
                    {
                        // nếu khách hẹn lại khi khác
                        messageDept = ItemSelected.DateDelay == null ? "Nếu không chọn ngày hẹn. Lịch nhắc bảo hành sẽ được nhắc vào tháng sau."
                            : $" Khách hẹn lại ngày [{ItemSelected.DateDelay.Value.ToString(DefaultConstants.FORMAT_DATE)}]";
                    }
                    isConfirm = await _rDialogs!.ConfirmAsync($"{messageDept} Bạn có chắc muốn lưu thông tin nhắc bảo hành dịch vụ này?", "Thông báo");
                }    
                if (!isConfirm) return;
                await ShowLoader();
                CustomerDebtsModel oItem = new CustomerDebtsModel();
                oItem.CusNo = ItemSelected.CusNo;
                oItem.DocEntry = ItemSelected.DocEntry;
                oItem.TotalDebtAmount = ItemSelected.TotalDebtAmount - ItemSelected.GuestsPay;
                oItem.GuestsPay = ItemSelected.GuestsPay;
                oItem.Remark = ItemSelected.Remark;
                oItem.IsDelay = ItemSelected.IsDelay;
                oItem.DateDelay = ItemSelected.DateDelay;
                oItem.Type = ItemSelected.Type;
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
