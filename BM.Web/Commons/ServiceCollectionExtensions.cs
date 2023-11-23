using BM.Web.Services;

namespace BM.Web.Commons
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRegisterComponents(this IServiceCollection services)
        {
            services.AddTransient<IProgressService, ProgressService>();
            services.AddScoped<LoaderService>();
            services.AddScoped<ToastService>();
            return services;
        }

        public static IServiceCollection AddClientScopeService(this IServiceCollection services)
        {
            services.AddScoped<ICliMasterDataService, CliMasterDataService>();
            return services;
        }
    }
}
