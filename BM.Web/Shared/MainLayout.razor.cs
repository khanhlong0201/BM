using BM.Web.Models;
using Microsoft.AspNetCore.Components;

namespace BM.Web.Shared;
public partial class MainLayout
{
    public List<BreadcrumbModel>? ListBreadcrumbs { get; set; }
    EventCallback<List<BreadcrumbModel>> BreadcrumbsHandler =>
        EventCallback.Factory.Create(this, (Action<List<BreadcrumbModel>>)NotifyBreadcrumb);

    private void NotifyBreadcrumb(List<BreadcrumbModel> _breadcrumbs) => ListBreadcrumbs = _breadcrumbs;
}