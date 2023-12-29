using Blazored.LocalStorage;
using BM.Models;
using BM.Models.Shared;
using BM.Web.Commons;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
        private const string TEMPLATE_PRINT_PHIEU_XUAT_KHO = "HtmlPrints\\LenhXuatKho.html";
        public bool pIsCreate { get; set; } = false;
        public int pDocEntry { get; set; } = 0;
        public int pBaseLine { get; set; } = 0;
        public int pBaseEntry { get; set; } = 0;
        public bool pIsLockPage { get; set; } = false;
        public bool pIsDoc { get; set; } = false; //longtran 2023-12-29 có phải link từ doccument
        public ServiceCallModel DocumentUpdate { get; set; } = new ServiceCallModel();
        public IEnumerable<ComboboxModel>? ListUsers { get; set; } // danh sách nhân viên
        public List<string>? ListUserImplements { get; set; } // nhân viên thực hiện
        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }
        public bool IsShowOutBound { get; set; } = false;
        public OutBoundModel OutBoundUpdate { get; set; } = new OutBoundModel();
        public List<SuppliesModel>? ListSuppplies { get; set; } // vật tư để lập phiếu xuất kho
        public EditContext? _EditOutBoundContext { get; set; }
        public SearchModel ItemFilter { get; set; } = new SearchModel();
        public string StatusOutBound { get; set; } = "Chưa";

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
                            if (pParams.ContainsKey("pBaseLine")) pBaseLine = Convert.ToInt32(pParams["pBaseLine"]);
                            if (pParams.ContainsKey("pBaseEntry")) pBaseEntry = Convert.ToInt32(pParams["pBaseEntry"]);
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
                            DocumentUpdate.ConsultUserName = oServiceCall.ConsultUserName;
                            DocumentUpdate.ServiceCode = oServiceCall.ServiceCode;
                            DocumentUpdate.ServiceName = oServiceCall.ServiceName;
                            DocumentUpdate.ChemicalFormula = oServiceCall.ChemicalFormula;
                            DocumentUpdate.StatusBefore = oServiceCall.StatusBefore;
                            DocumentUpdate.HealthStatus = oServiceCall.HealthStatus;
                            DocumentUpdate.NoteForAll = oServiceCall.NoteForAll;
                            DocumentUpdate.SkinType = oServiceCall.SkinType;
                            DocumentUpdate.WarrantyPeriod = oServiceCall.WarrantyPeriod;
                            DocumentUpdate.QtyWarranty = oServiceCall.QtyWarranty;

                            //phiếu xuất kho
                            OutBoundUpdate.ServiceCode = oServiceCall.ServiceCode + "";
                            OutBoundUpdate.ServiceName = oServiceCall.ServiceName + "";
                            OutBoundUpdate.ListUserImplements = oServiceCall.ListUserImplements;
                            OutBoundUpdate.ChemicalFormula = oServiceCall.ChemicalFormula + "";
                            OutBoundUpdate.StartTime = DateTime.Now;
                            OutBoundUpdate.EndTime = DateTime.Now;
                            OutBoundUpdate.BranchName = DocumentUpdate.BranchName;
                            OutBoundUpdate.FullName = DocumentUpdate.FullName;
                            OutBoundUpdate.CusNo = DocumentUpdate.CusNo;
                            OutBoundUpdate.BaseEntry = DocumentUpdate.BaseEntry;
                            OutBoundUpdate.IdDraftDetail = oServiceCall.BaseLine; //id của chi tiết
                            OutBoundUpdate.BranchId = pBranchId;
                            OutBoundUpdate.Remark = oServiceCall.Remark;// đặc điểm khách hàng
                            OutBoundUpdate.HealthStatus = DocumentUpdate.HealthStatus;// tình trạng sức khỏe
                            OutBoundUpdate.ListChargeUser = oServiceCall.ListUserImplements;
                            StatusOutBound = (pIsCreate || string.IsNullOrEmpty(oServiceCall.StatusOutBound)) ? "Chưa" : oServiceCall.StatusOutBound;
                            OutBoundUpdate.DateCreate = oServiceCall.DateCreateOutBound;
                            OutBoundUpdate.DateCreate = oServiceCall.DateCreateOutBound == null ? DateTime.Now : oServiceCall.DateCreateOutBound;
                        }
                    }
                    else
                    {
                        // Vô từ page lập chưng từ
                        await showVoucher();
                    }

                    // danh sách nhân viên
                    var listUsers = await _masterDataService!.GetDataUsersAsync();
                    if (listUsers != null && listUsers.Any())
                    {
                        ListUsers = listUsers.Select(m => new ComboboxModel()
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
            ItemFilter.Type = nameof(EnumTable.ServiceCalls);
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

                //phiếu xuất kho
                OutBoundUpdate.ServiceCode = oServiceCall.ServiceCode + "";
                OutBoundUpdate.ServiceName = oServiceCall.ServiceName + "";
                OutBoundUpdate.ListUserImplements = ListUserImplements;
                OutBoundUpdate.ChemicalFormula = oServiceCall.ChemicalFormula + "";
                OutBoundUpdate.StartTime = DateTime.Now;
                OutBoundUpdate.EndTime = DateTime.Now;
                OutBoundUpdate.BranchName = DocumentUpdate.BranchName;
                OutBoundUpdate.FullName = DocumentUpdate.FullName;
                OutBoundUpdate.CusNo = DocumentUpdate.CusNo;
                OutBoundUpdate.BaseEntry = DocumentUpdate.BaseEntry;
                OutBoundUpdate.IdDraftDetail = oServiceCall.BaseLine; //id của chi tiết
                OutBoundUpdate.BranchId = pBranchId;
                OutBoundUpdate.Remark = oServiceCall.Remark;// đặc điểm khách hàng
                OutBoundUpdate.HealthStatus = DocumentUpdate.HealthStatus;// tình trạng sức khỏe
                OutBoundUpdate.ListChargeUser = oServiceCall.ImplementUserId?.Split(",")?.ToList();
                OutBoundUpdate.DateCreate = oServiceCall.DateCreateOutBound == null ? DateTime.Now : oServiceCall.DateCreateOutBound;
                StatusOutBound = (pIsCreate || string.IsNullOrEmpty(oServiceCall.StatusOutBound)) ? "Chưa": oServiceCall.StatusOutBound;

                if(pBaseLine == 0) pBaseLine = OutBoundUpdate.IdDraftDetail.Value;
                if (pDocEntry == 0) pDocEntry = DocumentUpdate.DocEntry;

                //await InvokeAsync(StateHasChanged);
            }    
        }

        #endregion

        #region Protected Functions
        protected async void ShowOutBoundHandler(EnumType pAction = EnumType.Add, OutBoundModel? pOutBound = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                    if (pDocEntry <= 0)
                    {
                        ShowWarning("Bạn phải lưu phiếu bảo hành trước khi muốn lập phiếu xuất kho");
                        return;
                    }
                    if (StatusOutBound == "Rồi")
                    {
                        ShowWarning("Bạn đã lập phiếu xuất kho cho bảo hành dịch vụ này rồi");
                        return;
                    }
                    await ShowLoader();
                }
                else
                {

                }
                ItemFilter.Type = nameof(SuppliesKind.Popular);
                ListSuppplies = await _masterDataService!.GetDataSuppliesAsync(ItemFilter);
                IsShowOutBound = true;
                _EditOutBoundContext = new EditContext(OutBoundUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "ShowOutBound");
                ShowError(ex.Message);
            }
            finally
            {
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }
        protected async void SaveOutBoundHandler(EnumType pProcess = EnumType.Update)
        {
            try
            {
                string sAction = nameof(EnumType.Add);
                bool isConfirm = false;
                if (pProcess == EnumType.Update)
                {
                    isConfirm = await _rDialogs!.ConfirmAsync($"Bạn có chắc muốn lưu phiếu xuất này ?", "Thông báo");
                }
                if (!isConfirm) return;

                var listSuppliesCheck = ListSuppplies.Where(d => d.QtyInv > 0 && d.Qty >= 0).ToList(); // bắt nếu chưa nhâp số lượng xuất kho   
                if(listSuppliesCheck == null || listSuppliesCheck.Count == 0)
                {
                    ShowWarning("Bạn phải nhập số lượng xuất kho");
                    return;
                }
                await ShowLoader();
                if (ListSuppplies != null && ListSuppplies.Any())
                {
                    var CheckListSupplies = ListSuppplies.Where(d => d.Qty > d.QtyInv).FirstOrDefault();
                    if (CheckListSupplies != null)
                    {
                        ShowWarning("Số lượng xuất phải <= Tổng số lượng tồn kho");
                        await ShowLoader(false);
                        return;
                    }
                    var listSuppliesOutBound = ListSuppplies.Select(m => new SuppliesOutBoundModel()
                    {
                        SuppliesCode = m.SuppliesCode,
                        SuppliesName = m.SuppliesName,
                        EnumId = m.EnumId,
                        EnumName = m.EnumName,
                        Qty = m.Qty,
                        QtyInv = m.QtyInv
                    });
                    OutBoundUpdate.Type = nameof(OutBoundType.ByWarranty);//theo bảo hành
                    OutBoundUpdate.ChargeUser = OutBoundUpdate.ListChargeUser == null || !OutBoundUpdate.ListChargeUser.Any() ? "" : string.Join(",", OutBoundUpdate.ListChargeUser);
                    OutBoundUpdate.SuppliesQtyList = JsonConvert.SerializeObject(listSuppliesOutBound);
                }
                bool isSuccess = await _documentService!.UpdateOutBound(JsonConvert.SerializeObject(OutBoundUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    IsShowOutBound = false;
                     await showVoucher();
                    return;
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
        protected async Task SaveDocHandler(EnumType pProcess = EnumType.Update)
        {
            try
            {
                if (pIsLockPage)
                {
                    ShowWarning("Đơn hàng này đã được đóng!");
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
        /// <summary>
        /// in biên phiếu xuất kho
        /// </summary>
        protected async Task PrintOutBound()
        {
            try
            {
                SearchModel itemFilter = new SearchModel();
                itemFilter.UserId = pUserId;
                itemFilter.IsAdmin = pIsAdmin;
                itemFilter.IdDraftDetail = pBaseLine; // chi tiết
                itemFilter.FromDate = new DateTime(2023, 11, 11);
                itemFilter.ToDate = DateTime.Now;
                itemFilter.Type = nameof(OutBoundType.ByWarranty);
                List<OutBoundModel>? ListOutBound = await _documentService!.GetDataOutBoundsAsync(itemFilter);
                if (ListOutBound != null && ListOutBound.Count > 0)
                {
                    OutBoundUpdate = new OutBoundModel();
                    OutBoundUpdate = ListOutBound.First();
                    ListSuppplies = new List<SuppliesModel>();
                    if (OutBoundUpdate.SuppliesQtyList != null)
                    {
                        List<SuppliesModel> lstSuppplies = JsonConvert.DeserializeObject<List<SuppliesModel>>(OutBoundUpdate.SuppliesQtyList);
                        for (int i = 0; i < lstSuppplies.Count; i++)
                        {
                            var item = lstSuppplies[i];
                            SuppliesModel oLine = new SuppliesModel();
                            oLine.SuppliesCode = item.SuppliesCode;
                            oLine.SuppliesName = item.SuppliesName;
                            oLine.EnumId = item.EnumId;
                            oLine.EnumName = item.EnumName;
                            oLine.Qty = item.Qty;
                            oLine.QtyInv = item.QtyInv;
                            ListSuppplies.Add(oLine);
                        }
                    }
                    OutBoundUpdate.ListUserImplements = OutBoundUpdate.ImplementUserId?.Split(",")?.ToList(); // nhân viên thực hiện
                    OutBoundUpdate.ListChargeUser = OutBoundUpdate.ChargeUserName?.Split(",")?.ToList(); // nhân viên phục trách
                }
                //=============== xử lý đọc thông tin file html
                string sFilePath = $"{this._webHostEnvironment!.WebRootPath}\\{TEMPLATE_PRINT_PHIEU_XUAT_KHO}";
                StreamReader streamReader = new StreamReader(sFilePath);
                string sHtmlExport = streamReader.ReadToEnd();
                streamReader.Close();
                streamReader.Dispose();

                //replace html
                if (string.IsNullOrWhiteSpace(sHtmlExport)) return;
                sHtmlExport = sHtmlExport.Replace("{bm-VoucherNo}", $"{OutBoundUpdate.VoucherNo}");
                sHtmlExport = sHtmlExport.Replace("{bm-DateCreate}", $"{OutBoundUpdate.DateCreate?.ToString(DefaultConstants.FORMAT_DATE_TIME)}");
                sHtmlExport = sHtmlExport.Replace("{bm-CusNo}", $"{DocumentUpdate.CusNo}");
                sHtmlExport = sHtmlExport.Replace("{bm-FullName}", $"{DocumentUpdate.FullName}");
                sHtmlExport = sHtmlExport.Replace("{bm-BranchName}", $"{DocumentUpdate.BranchName}");
                sHtmlExport = sHtmlExport.Replace("{bm-ServiceName}", $"{DocumentUpdate.ServiceName}");
                sHtmlExport = sHtmlExport.Replace("{bm-ColorImplement}", $"{OutBoundUpdate.ColorImplement}");
                sHtmlExport = sHtmlExport.Replace("{bm-ChemicalFormula}", $"{OutBoundUpdate.ChemicalFormula}");
                //sHtmlExport = sHtmlExport.Replace("{bm-AnesthesiaType}", $"{OutBoundUpdate.AnesthesiaType}");
                //sHtmlExport = sHtmlExport.Replace("{bm-AnesthesiaQty}", $"{OutBoundUpdate.AnesthesiaQty}");
                //sHtmlExport = sHtmlExport.Replace("{bm-AnesthesiaCount}", $"{OutBoundUpdate.AnesthesiaCount}");
                sHtmlExport = sHtmlExport.Replace("{bm-DarkTestColor}", $"{OutBoundUpdate.DarkTestColor}");
                sHtmlExport = sHtmlExport.Replace("{bm-CoadingColor}", $"{OutBoundUpdate.CoadingColor}");
                sHtmlExport = sHtmlExport.Replace("{bm-LibColor}", $"{OutBoundUpdate.LibColor}");
                sHtmlExport = sHtmlExport.Replace("{bm-StartTime}", $"{OutBoundUpdate.StartTime?.ToString(DefaultConstants.FORMAT_DATE_TIME)}");
                sHtmlExport = sHtmlExport.Replace("{bm-EndTime}", $"{OutBoundUpdate.EndTime?.ToString(DefaultConstants.FORMAT_DATE_TIME)}");
                sHtmlExport = sHtmlExport.Replace("{bm-Problems}", $"{OutBoundUpdate.Problems}");
                sHtmlExport = sHtmlExport.Replace("{bm-Remark}", $"{DocumentUpdate.Remark}");
                sHtmlExport = sHtmlExport.Replace("{bm-HealthStatus}", $"{DocumentUpdate.HealthStatus}");
                sHtmlExport = sHtmlExport.Replace("{bm-UserNameCreate}", $"{OutBoundUpdate.UserNameCreate}");
                if (OutBoundUpdate.ListChargeUser != null)
                {
                    sHtmlExport = sHtmlExport.Replace("{bm-ListChargeUser}", $"{string.Join(", ", OutBoundUpdate.ListChargeUser)}");
                }
                else
                {
                    sHtmlExport = sHtmlExport.Replace("{bm-ListChargeUser}", null);
                }
                string tblOutbounds = "";
                for (int i = 0; i < ListSuppplies.Count; i++)
                {
                    tblOutbounds += @$" <tr>
                        <td style=""border: 1px solid #dddddd;text-align: left;padding: 8px;""><span>{ListSuppplies[i].SuppliesName}</span></td>
                        <td style=""border: 1px solid #dddddd;text-align: right;padding: 8px;""><span>{ListSuppplies[i].Qty}</span></td>
                    </tr> ";
                }
                sHtmlExport = sHtmlExport.Replace("{bm-Outbounds}", $"{tblOutbounds}");
                //in
                await _jsRuntime!.InvokeVoidAsync("printHtml", sHtmlExport);

            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "PrintOutBound");
                ShowError(ex.Message);
            }
            finally
            {
                await InvokeAsync(StateHasChanged);
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
        /// xem chi tiết Đơn hàng
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
        #endregion
    }
}