﻿using BM.Models;
using BM.Web.Models;
using BM.Web.Services;
using Microsoft.AspNetCore.Components;

namespace BM.Web.Features.Pages
{
    public partial class LoginPage 
    {

        #region Properties
        [Inject] NavigationManager? _navigationManager { get; set; }
        [Inject] private ILogger<LoginPage>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; set; }
        [Inject] public ToastService? _toastService { get; init; }
        [Inject] private IProgressService? _progressService { get; init; }
        public LoginViewModel LoginRequest { get; set; } = new LoginViewModel();
        public bool IsLoading { get; set; }
        public string ErrorMessage = "";
        public List<BranchModel>? ListBranchs { get; set; } = new List<BranchModel>();
        #endregion

        protected async Task LoginHandler()
        {
            try
            {
                ErrorMessage = "";
                IsLoading = true;
                var response = await _masterDataService!.LoginAsync(LoginRequest);
                if (!string.IsNullOrWhiteSpace(response)) { ErrorMessage = response; return; }
                _navigationManager!.NavigateTo("/", forceLoad: true); // để nó Authror 
                await Task.Delay(1000);
                await Task.Yield();
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally
            {
                IsLoading = false;
            }
        }

        #region "Form Events"
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await _progressService!.Start();
                    ListBranchs = await _masterDataService!.GetDataBranchsAsync(pIsPageLogin: true);
                    await _progressService!.SetPercent(0.6);
                }
                catch (Exception ex)
                {
                    _logger!.LogError(ex, "OnAfterRenderAsync");
                 
                }
                finally
                {
                    await _progressService!.Done();
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        #endregion "Form Events"
    }
}
