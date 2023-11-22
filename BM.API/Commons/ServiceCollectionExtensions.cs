using BM.API.Services;

namespace BM.API.Commons
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IMasterDataService, MasterDataService>();
            return services;
        }
    }
}
