﻿using Blazored.LocalStorage;
using BM.Models;
using BM.Web.Commons;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using System.Data;

namespace BM.Web.Services;

public interface ICliDocumentService
{
    Task<bool> UpdateSalesOrder(string pJson, string pJsonDetail, string pAction, int pUserId);
    Task<List<DocumentModel>?> GetDataDocumentsAsync(SearchModel pSearch);
    Task<Dictionary<string, string>?> GetDocByIdAsync(int pDocEntry);
    Task<List<DocumentModel>?> GetDocByCusNoAsync(string pCusNo);
    Task<bool> CancleDocList(string pJsonIds, string pReasonDelete, int pUserId, string pTableName);
    Task<List<SheduleModel>?> GetDataReminderByMonthAsync(SearchModel pSearch);
    Task<List<ReportModel>?> GetDataReportAsync(RequestReportModel pSearch);
    Task<List<CustomerDebtsModel>?> GetCustomerDebtsByDocAsync(int pDocEntry, string pType, string pCusNo);
    Task<bool> UpdateCustomerDebtsAsync(string pJson, int pUserId, string pType = nameof(EnumType.DebtReminder));
    Task<bool> UpdateOutBound(string pJson, string pAction, int pUserId);
    Task<List<OutBoundModel>?> GetDataOutBoundsAsync(SearchModel pSearch);
    Task<bool> CancleOutBoundList(string pJsonIds, string pReasonDelete, int pUserId);
    Task<List<ReportModel>?> GetRevenueReportAsync(int pYear);
    Task<bool> UpdateServiceCallAsync(string pJson, string pAction, int pUserId);
    Task<List<ServiceCallModel>?> GetServiceCallsAsync(SearchModel pSearch);
    Task<bool> CheckDataExistsAsync(RequestModel pRequest);
    Task<bool> UpdatePointByCusNoAsync(string pJson, int pUserId);
    Task<bool> UpdateConfigAsync(RequestModel pRequest);
}
public class CliDocumentService : CliServiceBase, ICliDocumentService
{
    private readonly ToastService _toastService;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public CliDocumentService(IHttpClientFactory factory, ILogger<CliMasterDataService> logger
        , ToastService toastService, ILocalStorageService localStorage, AuthenticationStateProvider authenticationStateProvider)
        : base(factory, logger)
    {
        _toastService = toastService;
        _localStorage = localStorage;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>
    /// cập nhật thông tin SalesOrder
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateSalesOrder(string pJson, string pJsonDetail, string pAction, int pUserId)
    {
        try
        {
            RequestModel request = new RequestModel
            {
                Json = pJson,
                JsonDetail = pJsonDetail,
                Type = pAction,
                UserId = pUserId
            };
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_UPDATE_SALES_ORDER, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string sMessage = pAction == nameof(EnumType.Add) ? DefaultConstants.MESSAGE_INSERT : DefaultConstants.MESSAGE_UPDATE;
                    _toastService.ShowSuccess($"{sMessage} thông tin đơn hàng!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSalesOrder");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// cập nhật phiếu xuất kho
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateOutBound(string pJson, string pAction, int pUserId)
    {
        try
        {
            RequestModel request = new RequestModel
            {
                Json = pJson,
                Type = pAction,
                UserId = pUserId
            };
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_UPDATE_OUTBOUND, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string sMessage = pAction == nameof(EnumType.Add) ? DefaultConstants.MESSAGE_INSERT : DefaultConstants.MESSAGE_UPDATE;
                    _toastService.ShowSuccess($"{sMessage} thông tin Lệnh và xuất kho!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateOutBound");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// Call API lấy danh sách phiếu xuất kho
    /// </summary>
    /// <returns></returns>
    public async Task<List<OutBoundModel>?> GetDataOutBoundsAsync(SearchModel pSearch)
    {
        try
        {
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_GET_OUTBOUND, pSearch);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<OutBoundModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDataOutBoundsAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// Call API lấy danh sách đơn hàng
    /// </summary>
    /// <returns></returns>
    public async Task<List<DocumentModel>?> GetDataDocumentsAsync(SearchModel pSearch)
    {
        try
        {
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_GET_SALES_ORDER, pSearch);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<DocumentModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDataDocumentsAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// Call API lấy danh sách đơn hàng
    /// </summary>
    /// <returns></returns>
    public async Task<Dictionary<string, string>?> GetDocByIdAsync(int pDocEntry)
    {
        try
        {
            Dictionary<string, object?> pParams = new Dictionary<string, object?>()
            {
                {"pDocEntry", $"{pDocEntry}"}
            };
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_DOCUMENT_GET_DOC_BY_ID, pParams);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode)
                {
                    var dt = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                    if(dt == null || dt.Keys.Count < 1) _toastService.ShowWarning(DefaultConstants.MESSAGE_NO_DATA);
                    return dt;
                    
                }    
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDocByIdAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// Call API lấy danh sách đơn hàng theo khach hàng
    /// </summary>
    /// <returns></returns>
    public async Task<List<DocumentModel>?> GetDocByCusNoAsync(string pCusNo)
    {
        try
        {
            Dictionary<string, object?> pParams = new Dictionary<string, object?>()
            {
                {"pCusNo", $"{pCusNo}"}
            };
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_DOCUMENT_GET_DOC_BY_CUSNO, pParams);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<DocumentModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDocByCusNoAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// Call API hủy đơn hàng
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> CancleDocList(string pJsonIds, string pReasonDelete, int pUserId, string pTableName)
    {
        try
        {
            RequestModel request = new RequestModel
            {
                Json = pJsonIds,
                JsonDetail = pReasonDelete,
                UserId = pUserId,
                Type = pTableName
            };
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_CANCLE_DOC_LIST, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    _toastService.ShowSuccess($"Đã hủy danh sách đơn hàng!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancleDocList");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }
    
    /// <summary>
    /// Call API hủy phiếu xuất kho
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> CancleOutBoundList(string pJsonIds, string pReasonDelete, int pUserId)
    {
        try
        {
            RequestModel request = new RequestModel
            {
                Json = pJsonIds,
                JsonDetail = pReasonDelete,
                UserId = pUserId
            };
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_CANCLE_OUTBOUND_LIST, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    _toastService.ShowSuccess($"Đã hủy danh sách phiếu xuất kho!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancleDocList");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// laays danh sách nhắc nợ + liệu trình
    /// </summary>
    /// <param name="pSearch"></param>
    /// <returns></returns>
    public async Task<List<SheduleModel>?> GetDataReminderByMonthAsync(SearchModel pSearch)
    {
        try
        {
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_REMINDER_BY_MONTH, pSearch);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<SheduleModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDataReminderByMonthAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }


    /// <summary>
    /// Call API lấy báo cáo 
    /// </summary>
    /// <returns></returns>
    public async Task<List<ReportModel>?> GetDataReportAsync(RequestReportModel pSearch)
    {
        try
        {
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_REPORT, pSearch);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<ReportModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDataReportAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// Call API danh sách lịch sử đơn hàng
    /// </summary>
    /// <returns></returns>
    public async Task<List<CustomerDebtsModel>?> GetCustomerDebtsByDocAsync(int pDocEntry, string pType, string pCusNo)
    {
        try
        {
            Dictionary<string, object?> pParams = new Dictionary<string, object?>()
            {
                {"pDocEntry", $"{pDocEntry}"},
                {"pType", $"{pType}"},
                {"pCusNo", $"{pCusNo}"},
            };
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_DOCUMENT_CUSTOMER_DEBTS_BY_DOC, pParams);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<CustomerDebtsModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCustomerDebtsByDocAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// call api thêm mới lịch sử thanh toán và công nợ
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateCustomerDebtsAsync(string pJson, int pUserId, string pType)
    {
        try
        {
            RequestModel request = new RequestModel
            {
                Json = pJson,
                UserId = pUserId
            };
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_UPDATE_CUSTOMER_DEBTS, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string mess = pType == nameof(EnumType.DebtReminder) ? "thanh toán" : "bảo hành";
                    _toastService.ShowSuccess($"Đã lưu thông tin nhắc {mess}!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancleDocList");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// LẤY BÁO CÁO THEO CHI NHÁNH
    /// </summary>
    /// <param name="pYear"></param>
    /// <returns></returns>
    public async Task<List<ReportModel>?> GetRevenueReportAsync(int pYear)
    {
        try
        {
            Dictionary<string, object?> pParams = new Dictionary<string, object?>()
            {
                {"pYear", $"{pYear}"}
            };
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_DOCUMENT_REVENUE_REPORT, pParams);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<ReportModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRevenueReportAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// cập nhật thông tin ServiceCall
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateServiceCallAsync(string pJson, string pAction, int pUserId)
    {
        try
        {
            RequestModel request = new RequestModel
            {
                Json = pJson,
                Type = pAction,
                UserId = pUserId
            };
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_UPDATE_SERVICE_CALL, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string sMessage = pAction == nameof(EnumType.Add) ? DefaultConstants.MESSAGE_INSERT : DefaultConstants.MESSAGE_UPDATE;
                    _toastService.ShowSuccess($"{sMessage} thông tin phiếu bảo hành!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateServiceCallAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// Call API lấy danh sách phiếu bảo hành
    /// </summary>
    /// <param name="pSearch"></param>
    /// <returns></returns>
    public async Task<List<ServiceCallModel>?> GetServiceCallsAsync(SearchModel pSearch)
    {
        try
        {
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_GET_SERVICE_CALL, pSearch);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<ServiceCallModel>>(content);
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                    return null;
                }
                var oMessage = JsonConvert.DeserializeObject<ResponseModel>(content);
                _toastService.ShowError($"{oMessage?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetServiceCallsAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// Call API check dữ liệu trùng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<bool> CheckDataExistsAsync(RequestModel pRequest)
    {
        try
        {
            if(pRequest == null)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_INVALID_DATA);
                return false;
            }    
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_CHECK_EXISTS_DATA, pRequest);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode) return true;
                if(oResponse.StatusCode == StatusCodes.Status409Conflict)
                {
                    _toastService.ShowInfo($"{oResponse.Message}");
                    return false;
                }    
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckDataExistsAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// call api cập nhật điểm cho khách hàng
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdatePointByCusNoAsync(string pJson, int pUserId)
    {
        try
        {
            RequestModel request = new RequestModel
            {
                Json = pJson,
                UserId = pUserId
            };
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_UPDATE_CUSTOMER_POINT, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    _toastService.ShowSuccess($"Quy đổi điểm của khách hàng thành công!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePointByCusNoAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// CẬP NHẬT CẤU HÌNH
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<bool> UpdateConfigAsync(RequestModel request)
    {
        try
        {
            //var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_DOCUMENT_CONFIG, request);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _toastService.ShowInfo(DefaultConstants.MESSAGE_LOGIN_EXPIRED);
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                ResponseModel oResponse = JsonConvert.DeserializeObject<ResponseModel>(content)!;
                if (httpResponse.IsSuccessStatusCode)
                {
                    _toastService.ShowSuccess($"{oResponse.Message}");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePointByCusNoAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }
}