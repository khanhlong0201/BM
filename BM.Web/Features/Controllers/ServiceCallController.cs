using Blazored.LocalStorage;
using BM.Models;
using BM.Models.Shared;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Telerik.Blazor;

namespace BM.Web.Features.Controllers
{
    public class ServiceCallController : BMControllerBase
    {
        #region Dependency Injection

        [Inject] private ILogger<ServiceCallController>? _logger { get; init; }
        [Inject] private ILocalStorageService? _localStorage { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }
        [Inject] private IWebHostEnvironment? _webHostEnvironment { get; init; }
        [Inject] public IJSRuntime? _jsRuntime { get; init; }

        #endregion Dependency Injection

        #region Properties
        public const string DATA_CUSTOMER_EMPTY = "Chưa cập nhật";
        public bool pIsCreate { get; set; } = false;
        public int pDocEntry { get; set; } = 0;
        public bool pIsLockPage { get; set; } = false;
        public ServiceCallModel DocumentUpdate { get; set; } = new ServiceCallModel();
        public IEnumerable<ComboboxModel>? ListUsers { get; set; } // danh sách nhân viên
        public List<string>? ListUserImplements { get; set; } // nhân viên thực hiện
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
                    new BreadcrumbModel() { Text = "Đơn hàng" },
                    new BreadcrumbModel() { Text = "Lập phiếu bảo hành" }
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                DocumentUpdate.VoucherNoBase = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.CusNo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.FullName = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Phone1 = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Address = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.CINo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Zalo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.FaceBook = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.ConsultUserId = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.ServiceCode = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.ServiceName = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.ChemicalFormula = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.StatusBefore = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.HealthStatus = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.NoteForAll = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.StatusId = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.StatusName = DATA_CUSTOMER_EMPTY;
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
                    if (await _localStorage!.ContainKeyAsync(nameof(EnumTable.ServiceCall)))
                    {
                        string sSvCall = await _localStorage!.GetItemAsStringAsync(nameof(EnumTable.ServiceCall));
                        ServiceCallModel oServiceCall = JsonConvert.DeserializeObject<ServiceCallModel>(EncryptHelper.Decrypt(sSvCall));
                        pIsCreate = true;
                        DocumentUpdate.VoucherNoBase = oServiceCall.VoucherNo;
                        DocumentUpdate.BaseEntry = oServiceCall.DocEntry;
                        DocumentUpdate.BaseLine = oServiceCall.BaseLine;
                        DocumentUpdate.CusNo = oServiceCall.CusNo;
                        DocumentUpdate.FullName = oServiceCall.FullName;
                        DocumentUpdate.Phone1 = oServiceCall.Phone1;
                        DocumentUpdate.Address = oServiceCall.Address;
                        DocumentUpdate.DateCreateBase = oServiceCall.DateCreateBase;
                        DocumentUpdate.DateOfBirth = oServiceCall.DateOfBirth;
                        DocumentUpdate.CINo = oServiceCall.CINo;
                        DocumentUpdate.Zalo = oServiceCall.Zalo;
                        DocumentUpdate.FaceBook = oServiceCall.FaceBook;
                        DocumentUpdate.ConsultUserId = DocumentUpdate.ConsultUserId;
                        DocumentUpdate.ServiceCode = oServiceCall.ServiceCode;
                        DocumentUpdate.ServiceName = oServiceCall.ServiceName;
                        DocumentUpdate.ChemicalFormula = oServiceCall.ChemicalFormula;
                        DocumentUpdate.StatusBefore = oServiceCall.StatusBefore;
                        DocumentUpdate.HealthStatus = oServiceCall.HealthStatus;
                        DocumentUpdate.NoteForAll = oServiceCall.NoteForAll;

                        // danh sách nhân viên
                        var listUsers = await _masterDataService!.GetDataUsersAsync();
                        if (listUsers != null && listUsers.Any()) ListUsers = listUsers.Select(m => new ComboboxModel()
                        {
                            Code = m.EmpNo,
                            Name = $"{m.EmpNo}-{m.FullName}"
                        });
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

        #endregion Override Functions

        protected async Task SaveDocHandler(EnumType pProcess = EnumType.Update)
        {
            try
            {
                if (pIsLockPage)
                {
                    ShowWarning("Đơn hàng này đã được thanh toán!");
                    return;
                }
                if (string.IsNullOrEmpty(DocumentUpdate.CusNo))
                {
                    ShowWarning("Không tìm thấy thông tin khách hàng. Vui lòng tải lại trang!");
                    return;
                }
                if (ListUserImplements == null || !ListUserImplements.Any())
                {
                    ShowWarning("Vui lòng chọn nhân viên thực hiện!");
                    return;
                }
                string sAction = pIsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
                string sStatusId = "";
                sStatusId = nameof(DocStatus.Pending);
                bool isConfirm = await _rDialogs!.ConfirmAsync($" Bạn có chắc muốn lưu thông tin phiếu bảo hành ?", "Thông báo");
                if (!isConfirm) return;
                await ShowLoader();
                DocumentUpdate.StatusId = sStatusId;
                DocumentUpdate.ImplementUserId = string.Join(",", ListUserImplements);
                DocumentUpdate.BranchId = pBranchId;
                bool isSuccess = await _documentService!.UpdateServiceCallAsync(JsonConvert.SerializeObject(DocumentUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    if (pIsCreate)
                    {
                        // back sang link theo dõi đơn hàng
                        Dictionary<string, string> pParams = new Dictionary<string, string>
                        {
                            { "pStatusId", $"{sStatusId}"},
                        };
                        string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                        //_navigationManager!.NavigateTo($"/sales-doclist?key={key}");
                        return;
                    }
                    //await showVoucher();
                }
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

        #region Protected Functions
        #endregion
    }
}