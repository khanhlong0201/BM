using BM.Models;
using BM.Web.Models;
using Microsoft.AspNetCore.Components;

namespace BM.Web.Features.Pages
{
    public partial class LoginPage
    {

        #region Properties
        public LoginViewModel LoginRequest { get; set; } = new LoginViewModel();
        public bool IsLoading { get; set; }
        public string ErrorMessage = "";
        public List<BranchModel>? ListBranchs { get; set; }
        #endregion

        protected async Task LoginHandler()
        {
            try
            {
                ErrorMessage = "";
                IsLoading = true;
                await Task.Delay(1000);
                await Task.Yield();
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally
            {
                await Task.Delay(200);
                IsLoading = false;
            }
        }
    }
}
