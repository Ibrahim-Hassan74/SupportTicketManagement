using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportTicketManagement.Core.Enums;

namespace SupportTicketManagement.Core
{
    public static class CoreServiceRegistration
    {
        public static IServiceCollection ConfigureCore(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(UserRole.Admin), policy =>
                    policy.RequireRole(nameof(UserRole.Admin)));

                options.AddPolicy(nameof(UserRole.SupportAgent), policy =>
                    policy.RequireRole(nameof(UserRole.SupportAgent)));

                options.AddPolicy(nameof(UserRole.Customer), policy =>
                    policy.RequireRole(nameof(UserRole.Customer)));

                options.AddPolicy("NotAuthorized", policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        return !context.User.Identity.IsAuthenticated;
                    });
                });
            });

            return services;
        }
    }
}
