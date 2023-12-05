﻿using BM.Models;
using BM.Models.Shared;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System;
using System.Data;

namespace BM.Web.Features.Controllers
{
    public class DocumentController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<DocumentController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private ICliDocumentService? _documentService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }
        #endregion

        #region Properties
        public double TotalDue { get; set; } = 0.0;
        public DocumentModel DocumentUpdate { get; set; } = new DocumentModel();
        public List<SalesOrderModel>? ListSalesOrder { get; set; } // ds đơn hàng
        public IEnumerable<ComboboxModel>? ListUsers { get; set; } // danh sách nhân viên
        public IEnumerable<IGrouping<string, ServiceModel>>? ListGroupServices { get; set; }
        public HConfirm? _rDialogs { get; set; }
        //
        public bool pIsCreate { get; set; } = false;
        public const string DATA_CUSTOMER_EMPTY = "Chưa cập nhật";
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
                DocumentUpdate.BranchName = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.FullName = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.CINo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Phone1 = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Zalo = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.FaceBook = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Address = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.Remark = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.SkinType = DATA_CUSTOMER_EMPTY;
                DocumentUpdate.DateCreate = _dateTimeService!.GetCurrentVietnamTime();
                // đọc giá tri câu query
                var uri = _navigationManager?.ToAbsoluteUri(_navigationManager.Uri);
                if (uri != null && QueryHelpers.ParseQuery(uri.Query).Count > 0)
                {
                    string key = uri.Query.Substring(5); // để tránh parse lỗi;    
                    Dictionary<string, string> pParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(EncryptHelper.Decrypt(key));
                    if(pParams != null && pParams.Any())
                    {
                        if (pParams.ContainsKey("pIsCreate")) pIsCreate = Convert.ToBoolean(pParams["pIsCreate"]);
                        if (pParams.ContainsKey("pDocEntry")) DocumentUpdate.DocEntry = Convert.ToInt32(pParams["pDocEntry"]);
                        if (pIsCreate && pParams.ContainsKey("pCusNo")) DocumentUpdate.CusNo = pParams["pCusNo"];
                    }    
                }    
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
                    // lấy danh sách dịch vụ
                    var listServices = await _masterDataService!.GetDataServicesAsync();
                    if(listServices != null && listServices.Any()) ListGroupServices = listServices.GroupBy(m => $"{m.EnumName}");

                    var listUsers = await _masterDataService!.GetDataUsersAsync();
                    if (listUsers != null && listUsers.Any()) ListUsers = listUsers.Select(m=> new ComboboxModel()
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
        #endregion
        #region Private Functions
        private async Task showVoucher()
        {
            Dictionary<string, string>? keyValues = await _documentService!.GetDocByIdAsync(DocumentUpdate.DocEntry);
            if (keyValues == null) return;
            if (keyValues.ContainsKey("oHeader")) DocumentUpdate = JsonConvert.DeserializeObject<DocumentModel>(keyValues["oHeader"]);
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
                    ListSalesOrder.Add(oLine);
                }       
            }    
        }
        #endregion

        #region Protected Functions
        protected void ValueChangedGuestsPayHandler(double value)
        {
            try
            {
                DocumentUpdate.GuestsPay = value;
                DocumentUpdate.Debt = (ListSalesOrder?.Sum(m => m.Amount) ?? 0) - value;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "DocumentController", "OnChangeGuestsPayHandler");
                ShowError(ex.Message);
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

        protected async void SaveDocHandler()
        {
            try
            {
                if(pIsCreate)
                {
                    if (string.IsNullOrEmpty(DocumentUpdate.CusNo))
                    {
                        ShowWarning("Không tìm thấy thông tin khách hàng!");
                        return;
                    }
                    if(ListSalesOrder == null || !ListSalesOrder.Any())
                    {
                        ShowWarning("Vui lòng chọn dịch vụ!");
                        return;
                    }
                    if(DocumentUpdate.GuestsPay <=0)
                    {
                        ShowWarning("Vui lòng điền số tiền khách trả!");
                        return;
                    }    
                    await ShowLoader();
                    DocumentUpdate.Total = ListSalesOrder?.Sum(m => m.Amount) ?? 0;
                    DocumentUpdate.BranchId = pBranchId;
                    DocumentUpdate.StatusId = nameof(DocStatus.Pending);
                    List<DocumentDetailModel> lstDraftDetails = ListSalesOrder!.Select(m => new DocumentDetailModel()
                    {
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
                        , JsonConvert.SerializeObject(lstDraftDetails), nameof(EnumType.Add), pUserId);
                }    
                else
                {

                }    
                
                await ShowLoader();
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
        #endregion
    }
}
