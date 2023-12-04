using BM.Models.Shared;
using BM.Web.Models;
using BM.Web.Services;
using Microsoft.AspNetCore.Components;

namespace BM.Web.Shared;

public class BMControllerBase : ComponentBase
{
    #region Dependency Injection
    [Inject] public IProgressService? _progressService { get; init; }
    [Inject] public LoaderService? _loaderService { get; init; }
    [Inject] public ToastService? _toastService { get; init; }
    [Inject] public IDateTimeService? _dateTimeService { get; init; }
    #endregion

    #region Properties
    [CascadingParameter]
    public EventCallback<List<BreadcrumbModel>> NotifyBreadcrumb { get; set; }
    public List<BreadcrumbModel>? ListBreadcrumbs { get; set; }

    public int pUserId { get; set; } = 1;
    public bool pIsAdmin { get; set; } = true;
    public string pBranchId { get; set; } = "KT003";

    #endregion Properties

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        try { await _progressService!.Start(); }
        catch(Exception) { }
    }

    #region Public Functions
    /// <summary>
    /// loading
    /// </summary>
    /// <param name="isShow"></param>
    /// <returns></returns>
    public async Task ShowLoader(bool isShow = true)
    {
        if (isShow)
        {
            _loaderService!.ShowLoader(isShow);
            await Task.Yield();
            return;
        }
        _loaderService!.ShowLoader(isShow);
    }

    public void ShowError(string pMessage, int pCloseAfter = 5500) => _toastService!.ShowError(pMessage, pCloseAfter);
    public void ShowWarning(string pMessage, int pCloseAfter = 5500) => _toastService!.ShowWarning(pMessage, pCloseAfter);
    public void ShowInfo(string pMessage, int pCloseAfter = 5500) => _toastService!.ShowInfo(pMessage, pCloseAfter);
    public void ShowSuccess(string pMessage, int pCloseAfter = 5500) => _toastService!.ShowSuccess(pMessage, pCloseAfter);
    #endregion
}
