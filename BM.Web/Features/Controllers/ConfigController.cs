using BM.Models;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Telerik.Blazor;

namespace BM.Web.Features.Controllers
{
    public class ConfigController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<ConfigController>? _logger { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private IConfiguration? _configuration { get; init; }
        #endregion

        #region Properties
        public string? VoucherNo { get; set; }
        public string? PrivateKey { get; set; }
        public string? Reason { get; set; }

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
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Cấu hình" }
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

        #region "Protected Functions"
        protected async Task CancledDocHandler()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(VoucherNo))
                {
                    ShowWarning("Vui lòng điền mã số phiếu cần hủy lệnh");
                    return;
                }
                if (string.IsNullOrWhiteSpace(Reason))
                {
                    ShowWarning("Vui lòng điền lý do hủy lệnh");
                    return;
                }
                if (string.IsNullOrWhiteSpace(PrivateKey))
                {
                    ShowWarning("Vui lòng điền khóa bí mật");
                    return;
                }
                string strPriveKey= _configuration!.GetSection("appSettings:PrivateKey").Value + "";
                if(PrivateKey.Trim() != strPriveKey)
                {
                    ShowWarning("Khóa bí mật không hợp lệ. Vui lòng liên hệ IT để được hổ trợ");
                    return;
                }
                await ShowLoader();
                RequestModel request = new RequestModel();
                request.UserId = pUserId;
                request.Type = "CANCLEDOC";
                request.Json = VoucherNo;
                request.JsonDetail = Reason;
                bool isSuccess = await _documentService!.UpdateConfigAsync(request);
                if (isSuccess)
                {
                    VoucherNo = "";
                    Reason = "";
                    PrivateKey = "";
                }
                await Task.Delay(75);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CancledDocHandler");
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
