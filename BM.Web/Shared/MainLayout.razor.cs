using BM.Web.Features.Pages;
using BM.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;

namespace BM.Web.Shared;
public partial class MainLayout
{
    [Inject] AuthenticationStateProvider? _authenticationStateProvider { get; set; }
    public List<BreadcrumbModel>? ListBreadcrumbs { get; set; }
    EventCallback<List<BreadcrumbModel>> BreadcrumbsHandler =>
        EventCallback.Factory.Create(this, (Action<List<BreadcrumbModel>>)NotifyBreadcrumb);

    private void NotifyBreadcrumb(List<BreadcrumbModel> _breadcrumbs) => ListBreadcrumbs = _breadcrumbs;

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
                RoleName = isAdmin ? "Admin" : "Nhân viên";
                StateHasChanged();
            }
        }
        catch (Exception) { }
    }
}