﻿using BM.Web.Features.Pages;
using BM.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;

namespace BM.Web.Shared;
public partial class MainLayout
{
    [Inject] private NavigationManager? _navManager { get; init; }
    [Inject] AuthenticationStateProvider? _authenticationStateProvider { get; set; }
    public List<BreadcrumbModel>? ListBreadcrumbs { get; set; }
    public string PageActive { get; set; } = "trang-chu";
    EventCallback<List<BreadcrumbModel>> BreadcrumbsHandler =>
        EventCallback.Factory.Create(this, (Action<List<BreadcrumbModel>>)NotifyBreadcrumb);
 
    private async void NotifyBreadcrumb(List<BreadcrumbModel> _breadcrumbs )
    {
        ListBreadcrumbs = _breadcrumbs;
        var uri = _navManager!.ToAbsoluteUri(_navManager.Uri);
        PageActive = uri.AbsolutePath;
        await InvokeAsync(StateHasChanged);
    }

    public string FullName { get; set; } = "";
    public string RoleName { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var oUser = await ((Providers.ApiAuthenticationStateProvider)_authenticationStateProvider!).GetAuthenticationStateAsync();
            if (oUser != null)
            {
                FullName = oUser.User.Claims.FirstOrDefault(m => m.Type == "FullName")?.Value + "";
                bool isAdmin = oUser.User.Claims.FirstOrDefault(m => m.Type == "IsAdmin")?.Value?.ToUpper() == "TRUE";
                string empNo = oUser.User.Claims.FirstOrDefault(m => m.Type == "EmpNo")?.Value + "";
                RoleName = isAdmin ? $"{empNo} - Admin" : $"{empNo} - Nhân viên";
                StateHasChanged();
            }
        }
        catch (Exception) { }
    }

}