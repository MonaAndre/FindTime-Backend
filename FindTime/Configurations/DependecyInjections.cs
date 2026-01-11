using FindTime.Services.Implementations;
using FindTime.Services.Interfaces;

namespace FindTime.Configurations
{
    public static class DependecyInjections
    {

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient();


            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<ICategoryService, CategoryService>();

            return services;

        }
    }
}
