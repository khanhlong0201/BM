using BM.Web.Models;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace BM.Web.Features.Controllers
{
    public class ServiceCallController : BMControllerBase
    {
        #region Dependency Injection

        [Inject] private ILogger<ServiceCallController>? _logger { get; init; }

        #endregion Dependency Injection

        #region Properties

        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }

        #endregion Properties

        #region Override Functions

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Chi tiết đơn hàng" },
                    new BreadcrumbModel() { Text = "Lập phiếu bảo hành" }
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

        #endregion Override Functions
    }
}