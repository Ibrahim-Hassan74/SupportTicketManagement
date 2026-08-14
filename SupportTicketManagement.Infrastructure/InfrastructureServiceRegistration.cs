using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportTicketManagement.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
