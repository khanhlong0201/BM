﻿using BM.Web.Providers;
using BM.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

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
            services.AddScoped<ICliDocumentService, CliDocumentService>();
            return services;
        }

        /// <summary>
        /// Lưu trạng thái đăng nhập
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddClientAuthorization(this IServiceCollection services)
        {
            services.AddAuthorizationCore();
            services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
            return services;
        }
    }
}
