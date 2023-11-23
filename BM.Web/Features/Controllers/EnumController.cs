using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BM.Web.Features.Controllers
{
    public class EnumController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<EnumController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        #endregion
        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<BranchModel>? ListBranchs { get; set; }
        public IEnumerable<BranchModel>? SelectedBranchs { get; set; } = new List<BranchModel>();
        public BranchModel BranchUpdate { get; set; } = new BranchModel();
        public EditContext? _EditContext { get; set; }
        public bool IsShowDialog { get; set; }
        public bool IsCreate { get; set; } = true;
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
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Danh mục" }
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
                    //await getDataBranchs();
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

    }
}
