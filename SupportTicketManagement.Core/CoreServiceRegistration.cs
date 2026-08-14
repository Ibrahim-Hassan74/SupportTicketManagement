using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportTicketManagement.Core
{
    public static class CoreServiceRegistration
    {
        public static IServiceCollection ConfigureCore(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
