using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.ServiceContracts;
using SupportTicketManagement.Core.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SupportTicketManagement.Core
{
    public static class CoreServiceRegistration
    {
        public static IServiceCollection ConfigureCore(this IServiceCollection services, IConfiguration configuration)
        {
            // JWT
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    //ClockSkew = TimeSpan.Zero, // Prevents the default 5-minute clock drift tolerance when validating token expiration
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudiences = configuration.GetSection("Jwt:Audiences").Get<List<string>>(),
                    RoleClaimType = ClaimTypes.Role,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(ApiResponseFactory.Unauthorized("You are not authorized."));
                        return context.Response.WriteAsync(result);
                    }
                };
            });

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

            services.AddScoped<IJwtService, JwtService>();

            services.AddScoped<IAuthenticationService, AuthenticationService>();

            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<ITicketsService, TicketsService>();
            services.AddScoped<ICommentsService, CommentsService>();
            services.AddScoped<IActivitiesService, ActivitiesService>();
            services.AddScoped<ITimeEntriesService, TimeEntriesService>();

            return services;
        }
    }
}
