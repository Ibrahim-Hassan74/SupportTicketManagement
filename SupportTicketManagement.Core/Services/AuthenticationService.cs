using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SupportTicketManagement.Core.Domain.IdentityEntities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.ServiceContracts;
using System.Security.Claims;

namespace SupportTicketManagement.Core.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtService _jwtService;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IHttpContextAccessor httpContextAccessor,
            IJwtService jwtService,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger) 
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _jwtService = jwtService;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }
        /// <inheritdoc/>
        public async Task<ApiResponse> RegisterAsync(RegisterDTO registerDTO)
        {
            if (registerDTO == null)
                return ApiResponseFactory.Failure("Invalid registration data.", 400, "Registration data cannot be null.");

            ValidationHelper.ModelValidation(registerDTO);

            if (await _userManager.FindByEmailAsync(registerDTO.Email) is not null)
            {
                _logger.LogWarning("Registration failed: Email {Email} is already registered.", registerDTO.Email);
                return ApiResponseFactory.Failure("Email is already registered.", 409, "This email is already in use.");
            }

            ApplicationUser user = new ApplicationUser()
            {
                DisplayName = registerDTO.UserName,
                UserName = registerDTO.Email,
                Email = registerDTO.Email,
                PhoneNumber = registerDTO.Phone,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.Now
            };

            IdentityResult result = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Registration failed for email {Email} due to identity errors.", registerDTO.Email);
                return ApiResponseFactory.Failure("Registration failed.", 400, result.Errors.Select(e => e.Description).ToArray());
            }

            await EnsureRoleExistsAndAssignAsync(user, UserRole.Customer.ToString());

            _logger.LogInformation("User {UserId} registered successfully with email {Email}.", user.Id, user.Email);
            return ApiResponseFactory.Success("Registration successful.");
        }
        /// <inheritdoc/>
        public async Task<ApiResponse> LoginAsync(LoginDTO loginDTO)
        {
            if (loginDTO == null)
                return ApiResponseFactory.Failure("Invalid login data.", 400, "Login data cannot be null.");

            ValidationHelper.ModelValidation(loginDTO);

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email {Email}.", loginDTO.Email);
                return ApiResponseFactory.Failure("User not found.", 404, "No account found with this email.");
            }

            if (user.IsDeleted)
            {
                _logger.LogWarning("Login failed: Account deactivated for user {UserId}.", user.Id);
                return ApiResponseFactory.Failure("Account deactivated.", 403, "Your account has been deactivated. Please contact an administrator.");
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginDTO.Password, loginDTO.RememberMe, true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} logged in successfully.", user.Id);
                return await CreateSuccessLoginResponseAsync(user, loginDTO.RememberMe);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Login failed: User {UserId} is locked out.", user.Id);
                string message = "Your account is temporarily locked due to multiple failed login attempts. Please try again later.";
                return ApiResponseFactory.Failure(message, 423, message);
            }
            else if (result.IsNotAllowed)
            {
                _logger.LogWarning("Login failed: User {UserId} is not allowed to login.", user.Id);
                return ApiResponseFactory.Failure("User is not allowed to login.", 403, "User is not allowed to login.");
            }
            else
            {
                _logger.LogWarning("Login failed: Invalid login attempt for user {UserId}.", user.Id);
                return ApiResponseFactory.Failure("Invalid login attempt.", 401, "Incorrect email or password.");
            }
        }

        /// <inheritdoc/>
        public async Task<ApiResponse> RefreshTokenAsync(TokenModel model)
        {

            ValidationHelper.ModelValidation(model);

            ClaimsPrincipal? principal;

            try
            {
                principal = _jwtService.GetPrincipalFromJwtToken(model.Token);
            }
            catch (SecurityTokenException ex)
            {
                return ApiResponseFactory.Failure("Invalid token.", 400, "Access token is invalid.");
            }

            if (principal is null)
                return ApiResponseFactory.Failure("Invalid token.", 400, "Access token is invalid.");

            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return ApiResponseFactory.Failure("Invalid token.", 400, "Email claim is missing in token.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return ApiResponseFactory.Failure("User not found.", 404, "User does not exist.");

            if (user.IsDeleted)
                return ApiResponseFactory.Failure("Account deactivated.", 403, "Your account has been deactivated. Please contact an administrator.");

            if (user.RefreshToken != model.RefreshToken ||
                user.RefreshTokenExpirationDateTime <= DateTimeOffset.UtcNow)
            {
                return ApiResponseFactory.Failure("Invalid refresh token.", 400, "Refresh token is invalid or expired.");
            }


            bool rememberMe = bool.TryParse(principal.FindFirst("remember_me")?.Value, out var rm) && rm;

            var authResponse = await _jwtService.CreateJwtToken(user, rememberMe) as ApiSuccessResponse;


            // Rotate refresh token
            user.RefreshToken = authResponse?.RefreshToken;

            user.RefreshTokenExpirationDateTime = authResponse.RefreshTokenExpirationDateTime;

            await _userManager.UpdateAsync(user);

            authResponse.Success = true;
            authResponse.StatusCode = 200;
            authResponse.Message = "Token refreshed successfully.";

            return authResponse;
        }


        /// <inheritdoc/>
        public async Task<ApiResponse> LogoutAsync(string? email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpirationDateTime = DateTimeOffset.MinValue;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _signInManager.SignOutAsync();

            return ApiResponseFactory.Success("Logged out successfully.");
        }

        private async Task EnsureRoleExistsAndAssignAsync(ApplicationUser user, string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });

            await _userManager.AddToRoleAsync(user, roleName);
        }

        private async Task<ApiSuccessResponse> CreateSuccessLoginResponseAsync(ApplicationUser user, bool rememberMe)
        {
            var tokenResponse = await _jwtService.CreateJwtToken(user, rememberMe) as ApiSuccessResponse;
            user.RefreshToken = tokenResponse?.RefreshToken;
            user.RefreshTokenExpirationDateTime = tokenResponse.RefreshTokenExpirationDateTime;
            await _userManager.UpdateAsync(user);
            tokenResponse.Success = true;
            tokenResponse.Message = "Login successful.";
            tokenResponse.StatusCode = 200;
            return tokenResponse;
        }

        public async Task<ApiResponse> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponseFactory.Failure("User not found.", 404, "No user found with the provided ID.");
            var role = await _userManager.GetRolesAsync(user);
            var userResponse = new UserResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = role.FirstOrDefault() ?? "Unknown",
                IsActive = !user.IsDeleted,
                CreatedAt = user.CreatedAt
            };

            return ApiResponseFactory.Success("User retrieved successfully.", userResponse);
        }
    }
}
