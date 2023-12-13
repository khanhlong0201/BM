using Blazored.LocalStorage;
using BM.Models;
using BM.Web.Components;
using BM.Web.Models;
using BM.Web.Services;
using BM.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Telerik.Blazor;
using Telerik.Blazor.Components;

namespace BM.Web.Features.Controllers
{
    public class IndexController : BMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<BranchController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] NavigationManager? _navigationManager { get; set; }
        #endregion

        #region Properties
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        public SchedulerView CurrView { get; set; } = SchedulerView.Month;
        public IEnumerable<SheduleModel> ListSchedulers = new List<SheduleModel>();
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
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
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
                    StartDate = _dateTimeService!.GetCurrentVietnamTime();
                    StartTime = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, 23,0,0);
                    ListSchedulers = new List<SheduleModel>()
                    {
                        new SheduleModel()
                        {
                            Id = 1,
                            Start = DateTime.Now,
                            End = DateTime.Now,
                            Title = "Trả nợ Trả nợ chi Trang ơi",
                            Description = "Trả nợ chi Trang ơi",
                            IsAllDay = true,
                        },
                        new SheduleModel()
                        {
                            Id = 2,
                            Start = DateTime.Now,
                            End = DateTime.Now,
                            Title = "Trả nợ 2",
                            Description = "Trả nợ chi Trang ơi 2",
                            IsAllDay = true,
                        },
                        new SheduleModel()
                        {
                            Id = 2,
                            Start = DateTime.Now,
                            End = DateTime.Now,
                            Title = "Trả nợ 2",
                            Description = "Trả nợ chi Trang ơi 2",
                            IsAllDay = true,
                        }
                    };
                    await _progressService!.SetPercent(0.4);
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

    
    }

    public class SheduleModel
    {
        public int Id { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }
        public bool IsAllDay { get; set; }
    }
}
