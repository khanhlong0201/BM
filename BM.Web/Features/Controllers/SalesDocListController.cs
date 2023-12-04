using BM.Models;
using BM.Models.Shared;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Telerik.Blazor.Components;

namespace BM.Web.Features.Controllers
{
    public class SalesDocListController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<SalesDocListController>? _logger { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<DocumentModel>? ListDocuments { get; set; }
        public IEnumerable<DocumentModel>? SelectedDocuments { get; set; } = new List<DocumentModel>();
        public List<ComboboxModel>? ListStatus { get; set; }
        public string pStatusId { get; set; } = nameof(DocStatus.Pending);
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
                    new BreadcrumbModel() { Text = "Theo dõi đơn hàng" },
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                ListStatus = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(DocStatus.Pending), Name = "Chờ xử lý"},
                    new ComboboxModel() {Code = nameof(DocStatus.Closed), Name = "Đã thanh toán"},
                };
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
                    await getDataDocuments();
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
        private async Task getDataDocuments()
        {
            ListDocuments = new List<DocumentModel>();
            SelectedDocuments = new List<DocumentModel>();
            ListDocuments = await _documentService!.GetDataDocumentsAsync(pUserId, pIsAdmin);
        }

        #endregion

        #region Protected Functions
        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataDocuments();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async void OnRowDoubleClickHandler(GridRowClickEventArgs args)
        {
            try
            {
                DocumentModel? oItem = (args.Item as DocumentModel);
                if (oItem == null) return;
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pDocEntry", $"{oItem.DocEntry}"},
                    { "pIsCreate", $"{false}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navManager!.NavigateTo($"/create-ticket?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "SalesDocListController", "OnRowDoubleClickHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }
        #endregion
    }
}
