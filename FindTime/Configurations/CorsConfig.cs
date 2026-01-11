using Microsoft.Extensions.DependencyInjection;

namespace FindTime.Configurations
{
    public static class CorsConfig
    {
        public static IServiceCollection AddCorsConfig(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowVueApp",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });
            return services;
        }
    }
}
