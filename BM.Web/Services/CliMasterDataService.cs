﻿using Blazored.LocalStorage;
using BM.Models;
using BM.Web.Commons;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace BM.Web.Services;

public interface ICliMasterDataService
{
    Task<List<BranchModel>?> GetDataBranchsAsync();
    Task<bool> UpdateBranchAsync(string pJson, string pAction, int pUserId);
    Task<List<UserModel>?> GetDataUsersAsync();
    Task<bool> UpdateUserAsync(string pJson, string pAction, int pUserId);
    Task<List<EnumModel>?> GetDataEnumsAsync(string pEnumType);
    Task<bool> UpdateEnumAsync(string pJson, string pAction, int pUserId);
    Task<List<CustomerModel>?> GetDataCustomersAsync();
    Task<bool> UpdateCustomerAsync(string pJson, string pAction, int pUserId);
    Task<List<ServiceModel>?> GetDataServicesAsync();
    Task<bool> UpdateServiceAsync(string pJson, string pAction, int pUserId);
}
public class CliMasterDataService : CliServiceBase, ICliMasterDataService
{
    private readonly ToastService _toastService;
    private readonly ILocalStorageService _localStorage;
    public CliMasterDataService(IHttpClientFactory factory, ILogger<CliMasterDataService> logger
        , ToastService toastService, ILocalStorageService localStorage)
        : base(factory, logger)
    {
        _toastService = toastService;
        _localStorage = localStorage;
    }

    /// <summary>
    /// Call API lấy danh sách Chi nhánh
    /// </summary>
    /// <returns></returns>
    public async Task<List<BranchModel>?> GetDataBranchsAsync()
    {
        try
        {
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_MASTERDATA_GETBRANCH);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<BranchModel>>(content);
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
            _logger.LogError(ex, "GetDataBranchsAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// cập nhật chi nhánh
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateBranchAsync(string pJson, string pAction, int pUserId)
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
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_MASTERDATA_UPDATE_BRANCH, request);
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
                    _toastService.ShowSuccess($"{sMessage} Chinh nhánh!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateBranchAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// Call API lấy danh sách user
    /// </summary>
    /// <returns></returns>
    public async Task<List<UserModel>?> GetDataUsersAsync()
    {
        try
        {
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_MASTERDATA_GET_USER);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<UserModel>>(content);
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
            _logger.LogError(ex, "GetDataBranchsAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// cập nhật nhân viên
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateUserAsync(string pJson, string pAction, int pUserId)
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
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_MASTERDATA_UPDATE_USER, request);
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
                    _toastService.ShowSuccess($"{sMessage} Nhân viên!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateBranchAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// Call API lấy danh sách Chi nhánh
    /// </summary>
    /// <returns></returns>
    public async Task<List<EnumModel>?> GetDataEnumsAsync(string pEnumType)
    {
        try
        {
            Dictionary<string, object?> pParams = new Dictionary<string, object?>()
            {
                {"pType", $"{pEnumType}"}
            };
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_MASTERDATA_GET_ENUM, pParams);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<EnumModel>>(content);
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
            _logger.LogError(ex, "GetDataEnumsAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// cập nhật danh mục
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateEnumAsync(string pJson, string pAction, int pUserId)
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
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_MASTERDATA_UPDATE_ENUM, request);
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
                    _toastService.ShowSuccess($"{sMessage} Danh mục!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateEnumAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// Call API lấy danh sách Chi nhánh
    /// </summary>
    /// <returns></returns>
    public async Task<List<CustomerModel>?> GetDataCustomersAsync()
    {
        try
        {
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_MASTERDATA_GET_CUSTOMER);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<CustomerModel>>(content);
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
            _logger.LogError(ex, "GetDataCustomersAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    /// <summary>
    /// cập nhật danh mục
    /// </summary>
    /// <param name="pJson"></param>
    /// <param name="pAction"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    public async Task<bool> UpdateCustomerAsync(string pJson, string pAction, int pUserId)
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
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_MASTERDATA_UPDATE_CUSTOMER, request);
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
                    _toastService.ShowSuccess($"{sMessage} Hồ sơ khách hàng!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCustomerAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }

    public async Task<List<ServiceModel>?> GetDataServicesAsync()
    {
        try
        {
            HttpResponseMessage httpResponse = await GetAsync(EndpointConstants.URL_MASTERDATA_GET_SERVICE);
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError(DefaultConstants.MESSAGE_INVALID_DATA);
            else
            {
                var content = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.IsSuccessStatusCode) return JsonConvert.DeserializeObject<List<ServiceModel>>(content);
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
            _logger.LogError(ex, "GetDataServicesAsync");
            _toastService.ShowError(ex.Message);
        }
        return default;
    }

    public async Task<bool> UpdateServiceAsync(string pJson, string pAction, int pUserId)
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
            HttpResponseMessage httpResponse = await PostAsync(EndpointConstants.URL_MASTERDATA_UPDATE_SERVICE, request);
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
                    _toastService.ShowSuccess($"{sMessage} Dịch vụ!");
                    return true;
                }
                _toastService.ShowError($"{oResponse.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateServiceAsync");
            _toastService.ShowError(ex.Message);
        }
        return false;
    }
}
