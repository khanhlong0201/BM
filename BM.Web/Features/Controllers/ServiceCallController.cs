using Blazored.LocalStorage;
using BM.Models;
using BM.Models.Shared;
using BM.Web.Commons;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
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
        private const string TEMPLATE_PRINT_CAM_KET = "HtmlPrints\\CamKetVaDongThuan.html";
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
                DocumentUpdate.SkinType = DATA_CUSTOMER_EMPTY;
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
                        if (pParams != null && pParams.Any())
                        {
                            if (pParams.ContainsKey("pIsCreate")) pIsCreate = Convert.ToBoolean(pParams["pIsCreate"]);
                            if (pParams.ContainsKey("pDocEntry")) pDocEntry = Convert.ToInt32(pParams["pDocEntry"]);
                        }
                    }
                    await _progressService!.SetPercent(0.4);
                    if(pIsCreate)
                    {
                        if (await _localStorage!.ContainKeyAsync(nameof(EnumTable.ServiceCalls)))
                        {
                            string sSvCall = await _localStorage!.GetItemAsStringAsync(nameof(EnumTable.ServiceCalls));
                            //await _localStorage!.RemoveItemAsync(nameof(EnumTable.ServiceCalls)); // Đọc xong rồi xóa luôn
                            ServiceCallModel oServiceCall = JsonConvert.DeserializeObject<ServiceCallModel>(EncryptHelper.Decrypt(sSvCall));
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
                            DocumentUpdate.ConsultUserId = oServiceCall.ConsultUserId;
                            DocumentUpdate.ServiceCode = oServiceCall.ServiceCode;
                            DocumentUpdate.ServiceName = oServiceCall.ServiceName;
                            DocumentUpdate.ChemicalFormula = oServiceCall.ChemicalFormula;
                            DocumentUpdate.StatusBefore = oServiceCall.StatusBefore;
                            DocumentUpdate.HealthStatus = oServiceCall.HealthStatus;
                            DocumentUpdate.NoteForAll = oServiceCall.NoteForAll;
                            DocumentUpdate.SkinType = oServiceCall.SkinType;
                            DocumentUpdate.WarrantyPeriod = oServiceCall.WarrantyPeriod;
                            DocumentUpdate.QtyWarranty = oServiceCall.QtyWarranty;

                        }
                    }
                    else
                    {
                        // Vô từ page lập chưng từ
                        await showVoucher();
                    }

                    // danh sách nhân viên
                    var listUsers = await _masterDataService!.GetDataUsersAsync();
                    if (listUsers != null && listUsers.Any()) ListUsers = listUsers.Select(m => new ComboboxModel()
                    {
                        Code = m.EmpNo,
                        Name = $"{m.EmpNo}-{m.FullName}"
                    });

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

        #region Private Functions

        private async Task showVoucher()
        {
            if (pDocEntry <= 0)
            {
                ShowWarning("Vui lòng tải lại trang hoặc liên hệ IT để được hổ trợ");
                return;
            }
            SearchModel ItemFilter = new SearchModel();
            ItemFilter.IdDraftDetail = pDocEntry;
            var oResult = await _documentService!.GetServiceCallsAsync(ItemFilter);
            if(oResult != null && oResult.Any())
            {
                ServiceCallModel oServiceCall = oResult[0];
                pIsLockPage = oServiceCall.StatusId != nameof(DocStatus.Pending); // lock page
                DocumentUpdate.VoucherNo = oServiceCall.VoucherNo;
                DocumentUpdate.DocEntry = oServiceCall.DocEntry;
                DocumentUpdate.BaseEntry = oServiceCall.BaseEntry;
                DocumentUpdate.BaseLine = oServiceCall.BaseLine;
                DocumentUpdate.VoucherNoBase = oServiceCall.VoucherNoBase;
                DocumentUpdate.StatusId = oServiceCall.StatusId;
                DocumentUpdate.StatusName = oServiceCall.StatusName;
                DocumentUpdate.DateCreate = oServiceCall.DateCreate;
                DocumentUpdate.BranchId = oServiceCall.BranchId;
                DocumentUpdate.BranchName = oServiceCall.BranchName;
                DocumentUpdate.CusNo = oServiceCall.CusNo;
                DocumentUpdate.FullName = oServiceCall.FullName;
                DocumentUpdate.Phone1 = oServiceCall.Phone1;
                DocumentUpdate.Address = oServiceCall.Address;
                DocumentUpdate.DateCreateBase = oServiceCall.DateCreateBase;
                DocumentUpdate.DateOfBirth = oServiceCall.DateOfBirth;
                DocumentUpdate.CINo = oServiceCall.CINo;
                DocumentUpdate.Zalo = oServiceCall.Zalo;
                DocumentUpdate.FaceBook = oServiceCall.FaceBook;
                DocumentUpdate.ConsultUserId = oServiceCall.ConsultUserId;
                DocumentUpdate.ServiceCode = oServiceCall.ServiceCode;
                DocumentUpdate.ServiceName = oServiceCall.ServiceName;
                DocumentUpdate.ChemicalFormula = oServiceCall.ChemicalFormula;
                DocumentUpdate.StatusBefore = oServiceCall.StatusBefore;
                DocumentUpdate.HealthStatus = oServiceCall.HealthStatus;
                DocumentUpdate.NoteForAll = oServiceCall.NoteForAll;
                DocumentUpdate.SkinType = oServiceCall.SkinType;
                DocumentUpdate.WarrantyPeriod = oServiceCall.WarrantyPeriod;
                DocumentUpdate.QtyWarranty = oServiceCall.QtyWarranty;
                ListUserImplements = oServiceCall.ImplementUserId?.Split(",")?.ToList();
            }    
        }    

        #endregion

        #region Protected Functions

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
                bool isConfirm = false;
                if (pProcess == EnumType.Update)
                {
                    isConfirm = await _rDialogs!.ConfirmAsync($" Bạn có chắc muốn lưu thông tin phiếu bảo hành ?", "Thông báo");
                    sStatusId = nameof(DocStatus.Pending);
                }    
                else
                {
                    isConfirm = await _rDialogs!.ConfirmAsync($" Bạn có chắc muốn lưu & đóng thông tin phiếu bảo hành ?", "Thông báo");
                    sStatusId = nameof(DocStatus.Closed);
                }
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
                        _navigationManager!.NavigateTo($"/service-call-list?key={key}");
                        return;
                    }
                    await showVoucher();
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceCallController", "SaveDocHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// in biên bản cam kết và đồng thuận
        /// </summary>
        protected async Task PrintCommitedDocHandler()
        {
            try
            {
                if (DocumentUpdate == null || string.IsNullOrEmpty(DocumentUpdate.ServiceCode))
                {
                    ShowWarning("Không có thông tin dịch vụ. Vui lòng tại lại trang");
                    return;
                }
                //<> Đọc danh sách tình trạng sức khỏe in Cam kết và đồng thuận </>
                var lstStateOfHealth = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.StateOfHealth));
                if (lstStateOfHealth == null || !lstStateOfHealth.Any())
                {
                    ShowWarning("Vui lòng khai báo Danh mục In tình trạng sức khỏe!");
                    return;
                }
                //=============== xử lý đọc thông tin file html
                string sFilePath = $"{this._webHostEnvironment!.WebRootPath}\\{TEMPLATE_PRINT_CAM_KET}";
                StreamReader streamReader = new StreamReader(sFilePath);
                string sHtmlExport = streamReader.ReadToEnd();
                streamReader.Close();
                streamReader.Dispose();
                //replace html
                if (string.IsNullOrWhiteSpace(sHtmlExport)) return;
                sHtmlExport = sHtmlExport.Replace("{bm-VoucherNo}", $"{DocumentUpdate.VoucherNo}");
                sHtmlExport = sHtmlExport.Replace("{bm-StatusName}", $"{DocumentUpdate.StatusName}");
                sHtmlExport = sHtmlExport.Replace("{bm-DateCreate}", $"{DocumentUpdate.DateCreate?.ToString(DefaultConstants.FORMAT_DATE_TIME)}");
                sHtmlExport = sHtmlExport.Replace("{bm-CusNo}", $"{DocumentUpdate.CusNo}");
                sHtmlExport = sHtmlExport.Replace("{bm-BranchName}", $"{DocumentUpdate.BranchName}");
                sHtmlExport = sHtmlExport.Replace("{bm-FullName}", $"{DocumentUpdate.FullName}");
                sHtmlExport = sHtmlExport.Replace("{bm-DateOfBirth}", DocumentUpdate.DateOfBirth == null ? DATA_CUSTOMER_EMPTY
                    : $"{DocumentUpdate.DateOfBirth.Value.ToString(DefaultConstants.FORMAT_DATE_TIME)}");
                sHtmlExport = sHtmlExport.Replace("{bm-CINo}", $"{DocumentUpdate.CINo}");
                sHtmlExport = sHtmlExport.Replace("{bm-Phone1}", $"{DocumentUpdate.Phone1}");
                sHtmlExport = sHtmlExport.Replace("{bm-Zalo}", $"{DocumentUpdate.Zalo}");
                sHtmlExport = sHtmlExport.Replace("{bm-FaceBook}", $"{DocumentUpdate.FaceBook}");
                sHtmlExport = sHtmlExport.Replace("{bm-Address}", $"{DocumentUpdate.Address}");
                sHtmlExport = sHtmlExport.Replace("{bm-StatusBefore}", $"{DocumentUpdate.StatusBefore}");
                sHtmlExport = sHtmlExport.Replace("{bm-SkinType}", $"{DocumentUpdate.SkinType}");
                sHtmlExport = sHtmlExport.Replace("{bm-HealthStatus}", $"{DocumentUpdate.HealthStatus}");
                sHtmlExport = sHtmlExport.Replace("{bm-WarrantyPeriod}", $"{DocumentUpdate.WarrantyPeriod}");
                sHtmlExport = sHtmlExport.Replace("{bm-QtyWarranty}", $"{DocumentUpdate.QtyWarranty}");
                sHtmlExport = sHtmlExport.Replace("{bm-ServiceName}", $"{DocumentUpdate.ServiceCode} - {DocumentUpdate.ServiceName}");
                sHtmlExport = sHtmlExport.Replace("{bm-Amount}", $"Gói trước {DocumentUpdate.Amount.ToString(DefaultConstants.FORMAT_CURRENCY)}");
                sHtmlExport = sHtmlExport.Replace("{bm-Weakness}", $"");
                sHtmlExport = sHtmlExport.Replace("{bm-Accept}", $"");
                sHtmlExport = sHtmlExport.Replace("{bm-ChemicalFormula}", $"");
                string htmlStateOfHealth = "";
                // Lặp qua danh sách với bước là 3 phần tử mỗi lần
                for (int i = 0; i < lstStateOfHealth.Count; i += 3)
                {
                    // Lấy 3 phần tử từ danh sách, bắt đầu từ vị trí i
                    var currentGroup = lstStateOfHealth.Skip(i).Take(3);
                    string htmlTd = "";

                    foreach (var oStateOfHealth in currentGroup)
                    {
                        htmlTd += @$"<td style=""border: 1px solid #dddddd;text-align: center;padding:1px;"">
                                        <span style=""font-size: 10.5px !important"">{oStateOfHealth.EnumName}</span>
                                    </td>
                                    <td style=""border: 1px solid #dddddd; width: 40px; padding: 1px; text-align: center; font-size: 11px !important"">Có <input type=""checkbox"" style=""height: 15px;width: 15px;"" /> </td>
                                    <td style=""border: 1px solid #dddddd; width: 61px; padding: 1px; text-align: center; font-size: 11px !important"">Không <input type=""checkbox"" style=""height: 15px;width: 15px;"" /></td> ";
                    }
                    htmlStateOfHealth += @$"<tr>{htmlTd}</tr> ";
                }
                sHtmlExport = sHtmlExport.Replace("{bm-lstStateOfHealth}", $"{htmlStateOfHealth}");
                //in
                await _jsRuntime!.InvokeVoidAsync("printHtml", sHtmlExport);

            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ServiceCallController", "PrintCommitedDocHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        #endregion
    }
}