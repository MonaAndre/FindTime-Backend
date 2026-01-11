using FindTime.Data;
using Microsoft.EntityFrameworkCore;

namespace FindTime.Configurations
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddConnectionString(this IServiceCollection services, IConfiguration config)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            services.AddDbContext<ApplicationDbContext>(options =>
                  options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

            return services;
        }
    }
}
