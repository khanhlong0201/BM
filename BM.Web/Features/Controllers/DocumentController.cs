using Blazored.LocalStorage;
using BM.Models;
using BM.Models.Shared;
using BM.Web.Commons;
using BM.Web.Components;
using BM.Web.Features.Pages;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Data;
using Telerik.Blazor;
using Telerik.Blazor.Resources;

namespace BM.Web.Features.Controllers
{
    public class DocumentController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<DocumentController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }
        [Inject] private IWebHostEnvironment? _webHostEnvironment { get; init; }
        [Inject] public IJSRuntime? _jsRuntime { get; init; }
        [Inject] private ILocalStorageService? _localStorage { get; init; }
        #endregion

        #region Properties
        public const string DATA_CUSTOMER_EMPTY = "Chưa cập nhật";
        private const string TEMPLATE_PRINT_HO_SO_KHACH_HANG = "HtmlPrints\\HoSoKhachHang.html";
        private const string TEMPLATE_PRINT_CAM_KET = "HtmlPrints\\CamKetVaDongThuan.html";
        private const string TEMPLATE_PRINT_PHIEU_XUAT_KHO = "HtmlPrints\\LenhXuatKho.html";
        public double TotalDue { get; set; } = 0.0;
        public DocumentModel DocumentUpdate { get; set; } = new DocumentModel();
        public List<SalesOrderModel>? ListSalesOrder { get; set; } // ds đơn hàng
        public IEnumerable<ComboboxModel>? ListUsers { get; set; } // danh sách nhân viên
        public IEnumerable<IGrouping<string, ServiceModel>>? ListGroupServices { get; set; }
        [CascadingParameter]
        public DialogFactory? _rDialogs { get; set; }
        //
        public bool pIsCreate { get; set; } = false;
        public bool pIsLockPage { get; set; } = false;
        public int pDocEntry { get; set; } = 0;
        public bool IsShowOutBound { get; set; } = false;
        public EditContext? _EditOutBoundContext { get; set; }
        public OutBoundModel OutBoundUpdate { get; set; } = new OutBoundModel();
        public List<SuppliesModel>? ListSuppplies { get; set; }
        public SearchModel ItemFilter { get; set; } = new SearchModel();
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
                DocumentUpdate.VoucherNo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.BranchName = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.FullName = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.CINo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Phone1 = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Zalo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.FaceBook = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Address = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Remark = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.SkinType = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.StatusName = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.DateCreate = _dateTimeService!.GetCurrentVietnamTime();
                ListSuppplies = new List<SuppliesModel>();
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
                            if (pIsCreate && pParams.ContainsKey("pCusNo")) DocumentUpdate.CusNo = pParams["pCusNo"];
                        }
                    }
                    await _progressService!.SetPercent(0.4);
                    // lấy thông tin khách hàng
                    if(pIsCreate)
                    {
                        var oCustomer = await _masterDataService!.GetCustomerByIdAsync(DocumentUpdate.CusNo + "");
                        if (oCustomer == null) return;
                        DocumentUpdate.BranchName = oCustomer.BranchName ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.FullName = oCustomer.FullName ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.CINo = oCustomer.CINo ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.Phone1 = oCustomer.Phone1 ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.Zalo = oCustomer.Zalo ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.FaceBook = oCustomer.FaceBook ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.Address = oCustomer.Address ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.Remark = oCustomer.Remark ?? DATA_CUSTOMER_EMPTY;
                        DocumentUpdate.SkinType = oCustomer.SkinType ?? DATA_CUSTOMER_EMPTY;
                    }    
                    else
                    {
                        // Vô từ page lập chưng từ
                        await showVoucher();
                    }
                    await _progressService!.SetPercent(0.6);
                    if(!pIsLockPage)
                    {
                        // Admin thì lấy theo chi nhánh ngược lại -> lấy theo chi nhánh + theo nhân viên
                        string loadAll = pIsAdmin ? nameof(EnumTable.Services) : nameof(EnumTable.Drafts);
                        // lấy danh sách dịch vụ
                        var listServices = await _masterDataService!.GetDataServicesAsync(pBranchId, pUserId, pLoadAll: loadAll);
                        if (listServices != null && listServices.Any())
                        {
                            ListGroupServices = listServices.GroupBy(m => string.IsNullOrEmpty(m.PackageName) ? $"{m.EnumName}"
                                : $"{m.EnumName} - {m.PackageName}");
                        }
                    }    
                    // danh sách nhân viên
                    var listUsers = await _masterDataService!.GetDataUsersAsync();
                    if (listUsers != null && listUsers.Any()) ListUsers = listUsers.Select(m=> new ComboboxModel()
                    {
                        Code = m.EmpNo,
                        Name = $"{m.EmpNo}-{m.FullName}"
                    });
                    ListSuppplies = await _masterDataService!.GetDataSuppliesAsync(ItemFilter);
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
        private async Task showVoucher()
        {
            if (pDocEntry <= 0)
            {
                ShowWarning("Vui lòng tải lại trang hoặc liên hệ IT để được hổ trợ");
                return;
            }    
            Dictionary<string, string>? keyValues = await _documentService!.GetDocByIdAsync(pDocEntry);
            if (keyValues == null) return;
            if (keyValues.ContainsKey("oHeader"))
            {
                DocumentUpdate = JsonConvert.DeserializeObject<DocumentModel>(keyValues["oHeader"]);
                pIsLockPage = DocumentUpdate.StatusId != nameof(DocStatus.Pending); // lock page
            }    
            if (keyValues.ContainsKey("oLine"))
            {
                ListSalesOrder = new List<SalesOrderModel>();
                List<DocumentDetailModel> lstDocLine = JsonConvert.DeserializeObject<List<DocumentDetailModel>>(keyValues["oLine"]);
                for(int i = 0; i< lstDocLine.Count; i++)
                {
                    var item = lstDocLine[i];
                    SalesOrderModel oLine = new SalesOrderModel();
                    oLine.Id = item.Id;
                    oLine.LineNum = (i + 1);
                    oLine.ServiceCode = item.ServiceCode + "";
                    oLine.ServiceName = item.ServiceName + "";
                    oLine.WarrantyPeriod = item.WarrantyPeriod;
                    oLine.QtyWarranty = item.QtyWarranty;
                    oLine.Price = item.Price;
                    oLine.PriceOld = item.PriceOld; // đơn giá hiện tại
                    oLine.Qty = item.Qty;
                    oLine.ChemicalFormula = item.ChemicalFormula + "";
                    oLine.ListUserAdvise = item.ConsultUserId?.Split(",")?.ToList();
                    oLine.ListUserImplements = item.ImplementUserId?.Split(",")?.ToList();
                    oLine.StatusOutBound = item.StatusOutBound;
                    if(!string.IsNullOrEmpty(item.JServiceCall))
                    {
                        // danh sách phiếu bảo hành
                        oLine.ListServiceCalls = JsonConvert.DeserializeObject<List<ServiceCallModel>>(item.JServiceCall);
                    }    
                    ListSalesOrder.Add(oLine);
                }       
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
                    if (DocumentUpdate.DocEntry <= 0)
                    {
                        ShowWarning("Vui lòng lưu thông tin đơn hàng trước khi lập [Phiếu xuất kho]");
                        return;
                    }
                    if (ListSalesOrder == null || !ListSalesOrder.Any())
                    {
                        ShowWarning("Không có thông tin dịch vụ!");
                        return;
                    }
                    var lstItem = ListSalesOrder.Where(d => d.IsCheck).ToList();
                    if (lstItem == null || !lstItem.Any())
                    {
                        ShowWarning("Vui lòng chọn dòng để lập [Phiếu xuất kho]!");
                        return;
                    }
                    if (lstItem.Count > 1)
                    {
                        ShowWarning("Chỉ được phép chọn 1 dịch vụ để lập [Phiếu xuất kho]!");
                        return;
                    }
                    await ShowLoader();
                    var oItem = lstItem[0];
                    if (oItem.StatusOutBound + "" == "Rồi")
                    {
                        ShowWarning("Bạn đã lập phiếu xuất kho cho dịch vụ này rồi");
                        return;
                    }

                    OutBoundUpdate.ServiceCode = oItem.ServiceCode;
                    OutBoundUpdate.ServiceName = oItem.ServiceName;
                    OutBoundUpdate.ListUserImplements = oItem.ListUserImplements;
                    OutBoundUpdate.ChemicalFormula = oItem.ChemicalFormula;
                    OutBoundUpdate.StartTime = DateTime.Now;
                    OutBoundUpdate.EndTime = DateTime.Now;
                    OutBoundUpdate.BranchName = DocumentUpdate.BranchName;
                    OutBoundUpdate.FullName = DocumentUpdate.FullName;
                    OutBoundUpdate.CusNo = DocumentUpdate.CusNo;
                    OutBoundUpdate.BaseEntry = DocumentUpdate.DocEntry;
                    OutBoundUpdate.IdDraftDetail = oItem.Id;
                    OutBoundUpdate.BranchId = pBranchId;
                    OutBoundUpdate.Remark = DocumentUpdate.Remark;// đặc điểm khách hàng
                    OutBoundUpdate.HealthStatus = DocumentUpdate.HealthStatus;// tình trạng sức khỏe
                    OutBoundUpdate.ListChargeUser = oItem.ListUserImplements;
                }
                else
                {

                }
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
                await ShowLoader();
                if(ListSuppplies != null && ListSuppplies.Any()){
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
                    oItem.WarrantyPeriod = oService.WarrantyPeriod;
                    oItem.QtyWarranty = oService.QtyWarranty;
                    ListSalesOrder.Add(oItem);
                }

                // đánh lại số thứ tự
                TotalDue = 0.0;
                for (int i = 0; i < ListSalesOrder.Count(); i++)
                {
                    ListSalesOrder[i].LineNum = (i + 1);
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

        protected void RemoveItemInSOHandler(int pId)
        {
            try
            {
                var oItem = ListSalesOrder!.FirstOrDefault(m => m.LineNum == pId);
                if(oItem != null)
                {
                    ListSalesOrder!.Remove(oItem);
                    // đánh lại số thứ tự
                    for (int i = 0; i < ListSalesOrder.Count(); i++) { ListSalesOrder[i].LineNum = (i + 1); }
                    StateHasChanged();
                }    
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "AddItemToSOHandler");
                ShowError(ex.Message);
            }
        }    

        protected async void SaveDocHandler(EnumType pProcess = EnumType.Update)
        {
            try
            {
                if(pIsLockPage)
                {
                    ShowWarning("Đơn hàng này đã được thanh toán!");
                    return;
                }    
                if (string.IsNullOrEmpty(DocumentUpdate.CusNo))
                {
                    ShowWarning("Không tìm thấy thông tin khách hàng. Vui lòng tải lại trang!");
                    return;
                }
                if (ListSalesOrder == null || !ListSalesOrder.Any())
                {
                    ShowWarning("Vui lòng chọn dịch vụ!");
                    return;
                }
                string sAction = pIsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
                string sStatusId = "";
                bool isConfirm = false;
                if(pProcess == EnumType.Update)
                {
                    isConfirm = await _rDialogs!.ConfirmAsync($" Bạn có chắc muốn lưu thông tin đơn hàng ?", "Thông báo");
                    sStatusId = nameof(DocStatus.Pending);
                }  
                else
                {
                    if (DocumentUpdate.GuestsPay <= 0)
                    {
                        ShowWarning("Vui lòng điền số tiền khách trả!");
                        return;
                    }
                    string messageDept = "";
                    if (DocumentUpdate.Debt > 0)
                    {
                        messageDept = $"Có công nợ {string.Format(DefaultConstants.FORMAT_GRID_CURRENCY, DocumentUpdate.Debt)}đ. \n " +
                            $"\r Số tiền sẽ được lưu vào công nợ của khách hàng [{DocumentUpdate.FullName}].";
                    } 
                    isConfirm = await _rDialogs!.ConfirmAsync($"{messageDept} Bạn có chắc muốn hoàn tất thanh toán đơn hàng này?", "Thông báo");
                    sStatusId = nameof(DocStatus.Closed);
                }
                if (!isConfirm) return;
                await ShowLoader();
                DocumentUpdate.Total = ListSalesOrder?.Sum(m => m.Amount) ?? 0;
                DocumentUpdate.BranchId = pBranchId;
                DocumentUpdate.StatusId = sStatusId;
                List<DocumentDetailModel> lstDraftDetails = ListSalesOrder!.Select(m => new DocumentDetailModel()
                {
                    Id = m.Id,
                    ServiceCode = m.ServiceCode,
                    ServiceName = m.ServiceName,
                    Price = m.Price,
                    Qty = m.Qty,
                    LineTotal = m.Amount,
                    ActionType = nameof(EnumType.Add),
                    ChemicalFormula = m.ChemicalFormula,
                    WarrantyPeriod = m.WarrantyPeriod,
                    QtyWarranty = m.QtyWarranty,
                    ConsultUserId = m.ListUserAdvise == null || !m.ListUserAdvise.Any() ? "" : string.Join(",", m.ListUserAdvise),
                    ImplementUserId = m.ListUserImplements == null || !m.ListUserImplements.Any() ? "" : string.Join(",", m.ListUserImplements)
                }).ToList();
                bool isSuccess = await _documentService!.UpdateSalesOrder(JsonConvert.SerializeObject(DocumentUpdate)
                    , JsonConvert.SerializeObject(lstDraftDetails), sAction, pUserId);
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
                        _navigationManager!.NavigateTo($"/sales-doclist?key={key}");
                        return;
                    }
                    await showVoucher();
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
        
        /// <summary>
        /// gọi template in chứng từ
        /// </summary>
        protected async Task PrintDocHandler()
        {
            try
            {
                if(DocumentUpdate.DocEntry <=0 || ListSalesOrder == null || !ListSalesOrder.Any())
                {
                    ShowWarning("Vui lòng lưu thông tin đơn hàng trước khi In");
                    return;
                }
                //=============== xử lý đọc thông tin file html
                string sFilePath = $"{this._webHostEnvironment!.WebRootPath}\\{TEMPLATE_PRINT_HO_SO_KHACH_HANG}";
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
                    :  $"{DocumentUpdate.DateOfBirth.Value.ToString(DefaultConstants.FORMAT_DATE_TIME)}");
                sHtmlExport = sHtmlExport.Replace("{bm-CINo}", $"{DocumentUpdate.CINo}");
                sHtmlExport = sHtmlExport.Replace("{bm-Phone1}", $"{DocumentUpdate.Phone1}");
                sHtmlExport = sHtmlExport.Replace("{bm-Zalo}", $"{DocumentUpdate.Zalo}");
                sHtmlExport = sHtmlExport.Replace("{bm-FaceBook}", $"{DocumentUpdate.FaceBook}");
                sHtmlExport = sHtmlExport.Replace("{bm-Address}", $"{DocumentUpdate.Address}");
                sHtmlExport = sHtmlExport.Replace("{bm-Remark}", $"{DocumentUpdate.Remark}");
                sHtmlExport = sHtmlExport.Replace("{bm-StatusBefore}", $"{DocumentUpdate.StatusBefore}");
                sHtmlExport = sHtmlExport.Replace("{bm-SkinType}", $"{DocumentUpdate.SkinType}");
                sHtmlExport = sHtmlExport.Replace("{bm-HealthStatus}", $"{DocumentUpdate.HealthStatus}");
                sHtmlExport = sHtmlExport.Replace("{bm-NoteForAll}", $"{DocumentUpdate.NoteForAll}");
                string tblServices = "";
                for(int i=0; i < ListSalesOrder.Count;i++)
                {
                    tblServices += @$" <tr>
                        <td style=""border: 1px solid #dddddd;text-align: left;padding: 8px;""><span>{(i + 1)}</td>
                        <td style=""border: 1px solid #dddddd;text-align: left;padding: 8px;""><span>{ListSalesOrder[i].ServiceName}</span></td>
                        <td style=""border: 1px solid #dddddd;text-align: right;padding: 8px;""><span>{ListSalesOrder[i].Price.ToString(DefaultConstants.FORMAT_CURRENCY)}đ</span></td>
                        <td style=""border: 1px solid #dddddd;text-align: right;padding: 8px;""><span>{ListSalesOrder[i].Qty}</span></td>
                        <td style=""border: 1px solid #dddddd;text-align: right;padding: 8px;""><span>{ListSalesOrder[i].Amount.ToString(DefaultConstants.FORMAT_CURRENCY)}đ</span></td>
                        <td style=""border: 1px solid #dddddd;text-align: left;padding: 8px;""><span>{ListSalesOrder[i].ServiceName}</span></td>
                        <td style=""border: 1px solid #dddddd;text-align: left;padding: 8px;""><span>{ListSalesOrder[i].ServiceName}</span></td>
                        <td style=""border: 1px solid #dddddd;text-align: left;padding: 8px;max-width: 150px""><span>{ListSalesOrder[i].ChemicalFormula}</span></td>
                    </tr> ";
                }
                sHtmlExport = sHtmlExport.Replace("{bm-cus-services}", $"{tblServices}");
                double pSumTotal = ListSalesOrder?.Sum(m => m.Amount) ?? 0;
                sHtmlExport = sHtmlExport.Replace("{bm-cus-total}", $"{pSumTotal.ToString(DefaultConstants.FORMAT_CURRENCY)}đ");
                sHtmlExport = sHtmlExport.Replace("{bm-cus-payment}", $"{DocumentUpdate.GuestsPay.ToString(DefaultConstants.FORMAT_CURRENCY)}đ");
                sHtmlExport = sHtmlExport.Replace("{bm-cus-debts}", $"{DocumentUpdate.Debt.ToString(DefaultConstants.FORMAT_CURRENCY)}đ");

                //in
                await _jsRuntime!.InvokeVoidAsync("printHtml", sHtmlExport);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "PrintDocHandler");
                ShowError(ex.Message);
            }
            finally
            {
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
                if (ListSalesOrder == null || !ListSalesOrder.Any())
                {
                    ShowWarning("Không có thông tin dịch vụ!");
                    return;
                }
                var lstItem = ListSalesOrder.Where(m => m.IsCheck == true).ToList();
                if(lstItem == null || !lstItem.Any())
                {
                    ShowWarning("Vui lòng chọn dòng để in [Cam kết & đồng thuận]!");
                    return;
                }
                if(lstItem.Count > 1)
                {
                    ShowWarning("Chỉ được phép chọn 1 dịch vụ để in [Cam kết & đồng thuận]!");
                    return;
                }
                //<> Đọc danh sách tình trạng sức khỏe in Cam kết và đồng thuận </>
                var lstStateOfHealth = await _masterDataService!.GetDataEnumsAsync(nameof(EnumType.StateOfHealth));
                if(lstStateOfHealth == null || !lstStateOfHealth.Any())
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
                sHtmlExport = sHtmlExport.Replace("{bm-Remark}", $"{DocumentUpdate.Remark}");
                sHtmlExport = sHtmlExport.Replace("{bm-StatusBefore}", $"{DocumentUpdate.StatusBefore}");
                sHtmlExport = sHtmlExport.Replace("{bm-SkinType}", $"{DocumentUpdate.SkinType}");
                sHtmlExport = sHtmlExport.Replace("{bm-HealthStatus}", $"{DocumentUpdate.HealthStatus}");
                sHtmlExport = sHtmlExport.Replace("{bm-WarrantyPeriod}", $"{lstItem[0].WarrantyPeriod}");
                sHtmlExport = sHtmlExport.Replace("{bm-QtyWarranty}", $"{lstItem[0].QtyWarranty}");
                sHtmlExport = sHtmlExport.Replace("{bm-ServiceName}", $"{lstItem[0].ServiceCode} - {lstItem[0].ServiceName}");
                sHtmlExport = sHtmlExport.Replace("{bm-Amount}", $"{lstItem[0].Amount.ToString(DefaultConstants.FORMAT_CURRENCY)}");
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
                _logger!.LogError(ex, "DocumentController", "PrintCommitedDocHandler");
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
                if (DocumentUpdate.DocEntry <= 0 || ListSalesOrder == null || !ListSalesOrder.Any())
                {
                    ShowWarning("Vui lòng lưu thông tin đơn hàng trước khi In");
                    return;
                }
                if (ListSalesOrder == null || !ListSalesOrder.Any())
                {
                    ShowWarning("Không có thông tin dịch vụ, nên không thể in phiếu xuất kho!");
                    return;
                }
                var lstItem = ListSalesOrder.Where(m => m.IsCheck == true).ToList();
                if (lstItem == null || !lstItem.Any())
                {
                    ShowWarning("Vui lòng chọn dòng để in [Phiếu xuất kho]!");
                    return;
                }
                if (lstItem.Count > 1)
                {
                    ShowWarning("Chỉ được phép chọn 1 dịch vụ để in [Phiếu xuất kho]!");
                    return;
                }
                var SelectLineSaleOrder = ListSalesOrder.Where(m => m.IsCheck == true).FirstOrDefault();

                 SearchModel itemFilter = new SearchModel();
                    itemFilter.UserId = pUserId;
                    itemFilter.IsAdmin = pIsAdmin;
                    itemFilter.IdDraftDetail = SelectLineSaleOrder.Id;
                    itemFilter.FromDate =new DateTime(2023,11,11);
                    itemFilter.ToDate = DateTime.Now;
                List<OutBoundModel>  ListOutBound = await _documentService!.GetDataOutBoundsAsync(itemFilter);
                if(ListOutBound!=null && ListOutBound.Count > 0)
                {
                    OutBoundUpdate = new OutBoundModel();
                    OutBoundUpdate = ListOutBound.FirstOrDefault();
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
                    OutBoundUpdate.ListChargeUser = OutBoundUpdate.ChargeUser?.Split(",")?.ToList(); // nhân viên phục trách
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
                sHtmlExport = sHtmlExport.Replace("{bm-ServiceName}", $"{SelectLineSaleOrder.ServiceName}");
                sHtmlExport = sHtmlExport.Replace("{bm-ColorImplement}", $"{OutBoundUpdate.ColorImplement}");
                sHtmlExport = sHtmlExport.Replace("{bm-ChemicalFormula}", $"{OutBoundUpdate.ChemicalFormula}");
                sHtmlExport = sHtmlExport.Replace("{bm-AnesthesiaType}", $"{OutBoundUpdate.AnesthesiaType}");
                sHtmlExport = sHtmlExport.Replace("{bm-AnesthesiaQty}", $"{OutBoundUpdate.AnesthesiaQty}");
                sHtmlExport = sHtmlExport.Replace("{bm-AnesthesiaCount}", $"{OutBoundUpdate.AnesthesiaCount}");
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
                }else
                {
                    sHtmlExport = sHtmlExport.Replace("{bm-ListChargeUser}",null);
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
        /// tạo phiếu bảo hành
        /// </summary>
        protected async Task CreateServiceCallHandler()
        {
            try
            {

                if (ListSalesOrder == null || !ListSalesOrder.Any())
                {
                    ShowWarning("Không có thông tin dịch vụ!");
                    return;
                }
                var lstItem = ListSalesOrder.Where(m => m.IsCheck == true).ToList();
                if (lstItem == null || !lstItem.Any())
                {
                    ShowWarning("Vui lòng chọn dòng dịch vụ để lập [Phiếu bảo hành]!");
                    return;
                }
                if (lstItem.Count > 1)
                {
                    ShowWarning("Chỉ được phép chọn 1 dịch vụ để lập [Phiếu bảo hành]!");
                    return;
                }
                SalesOrderModel oItem = lstItem[0];
                if (oItem.WarrantyPeriod <= 0)
                {
                    ShowInfo($"Dịch vụ [{oItem.ServiceCode} - {oItem.ServiceName}] không có bảo hành!");
                    return;
                }

                // lấy các thông tin khách hàng bê qua
                var oHeader = new
                {
                    DocumentUpdate.VoucherNo,
                    DocumentUpdate.DocEntry,
                    DocumentUpdate.CusNo,
                    DocumentUpdate.FullName,
                    DocumentUpdate.Phone1,
                    DocumentUpdate.Address,
                    DateCreateBase = DocumentUpdate.DateCreate,
                    DocumentUpdate.DateOfBirth,
                    DocumentUpdate.CINo,
                    DocumentUpdate.Zalo,
                    DocumentUpdate.FaceBook,
                    oItem.ChemicalFormula,
                    ConsultUserId = oItem.ListUserAdvise == null || !oItem.ListUserAdvise.Any() ? "" : string.Join(", ", oItem.ListUserAdvise),
                    oItem.ServiceCode,
                    oItem.ServiceName,
                    DocumentUpdate.StatusBefore,
                    DocumentUpdate.HealthStatus,
                    DocumentUpdate.NoteForAll,
                    DocumentUpdate.SkinType,
                    oItem.WarrantyPeriod,
                    oItem.QtyWarranty,
                    BaseLine = oItem.Id
                };
                if(await _localStorage!.ContainKeyAsync(nameof(EnumTable.ServiceCalls))) await _localStorage!.RemoveItemAsync(nameof(EnumTable.ServiceCalls));
                string data = EncryptHelper.Encrypt(JsonConvert.SerializeObject(oHeader)); // mã hóa dữ liệu
                await _localStorage!.SetItemAsStringAsync(nameof(EnumTable.ServiceCalls), data);
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "pIsCreate", $"{true}" }
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navigationManager!.NavigateTo($"/service-call?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "CreateServiceCallHandler");
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
