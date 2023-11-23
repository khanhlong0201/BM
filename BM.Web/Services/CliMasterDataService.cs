using Blazored.LocalStorage;
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
                _toastService.ShowInfo("Hết phiên đăng nhập!");
                return false;
            }
            var checkContent = ValidateJsonContent(httpResponse.Content);
            if (!checkContent) _toastService.ShowError("Không đúng định dạng dữ liệu!!!");
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
}
