using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.ServiceContracts;
using SupportTicketManagement.Core.Domain.IdentityEntities;
using Microsoft.Extensions.Logging;

namespace SupportTicketManagement.Core.Services
{
    public class UsersService : IUsersService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<UsersService> _logger;

        public UsersService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ILogger<UsersService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets a paginated, filterable list of users. Admin only.
        /// </summary>
        public async Task<ApiResponse> GetUsersAsync(UserQueryRequest request)
        {
            if (request.Page < 1)
                request.Page = 1;

            if (request.PageSize < 1)
                request.PageSize = 10;

            if (request.PageSize > 100)
                request.PageSize = 100;

            var query = _userManager.Users.AsNoTracking();

            // Filter by role
            if (request.Role.HasValue)
            {
                var roleName = request.Role.Value.ToString();
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                var userIds = usersInRole.Select(u => u.Id).ToHashSet();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            // Search by display name or email
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(u =>
                    u.DisplayName.ToLower().Contains(search) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.DisplayName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Map to response DTOs (need role for each user)
            var userResponses = new List<UserResponse>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userResponses.Add(MapToUserResponse(user, roles.FirstOrDefault() ?? ""));
            }

            var paginatedResult = new PaginatedResponse<UserResponse>
            {
                Items = userResponses,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return ApiResponseFactory.Success("Users retrieved successfully.", paginatedResult);
        }

        /// <summary>
        /// Gets a single user by ID. Admin only.
        /// </summary>
        public async Task<ApiResponse> GetUserByIdAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return ApiResponseFactory.NotFound("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var response = MapToUserResponse(user, roles.FirstOrDefault() ?? "");

            return ApiResponseFactory.Success("User retrieved successfully.", response);
        }

        /// <summary>
        /// Creates a new user with the specified role. Admin only.
        /// Validates that the role is a valid UserRole enum value and creates the role if it doesn't exist.
        /// </summary>
        public async Task<ApiResponse> CreateUserAsync(CreateUserRequest request)
        {
            ValidationHelper.ModelValidation(request);

            // Validate role is a valid enum value
            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            {
                _logger.LogWarning("Failed to create user: Invalid role '{Role}'.", request.Role);
                return ApiResponseFactory.BadRequest($"Invalid role '{request.Role}'. Valid roles: {string.Join(", ", Enum.GetNames<UserRole>())}");
            }

            // Check for duplicate email
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                _logger.LogWarning("Failed to create user: A user with email {Email} already exists.", request.Email);
                return ApiResponseFactory.Conflict("A user with this email already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                DisplayName = request.DisplayName,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Failed to create user {Email} due to identity errors.", request.Email);
                var errors = createResult.Errors.Select(e => e.Description).ToList();
                return ApiResponseFactory.BadRequest("Failed to create user.", errors);
            }

            // Ensure role exists and assign
            var roleName = role.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });

            await _userManager.AddToRoleAsync(user, roleName);

            var response = MapToUserResponse(user, roleName);
            _logger.LogInformation("Admin successfully created user {UserId} with role {RoleName}.", user.Id, roleName);
            return ApiResponseFactory.Success("User created successfully.", response);
        }

        /// <summary>
        /// Updates a user's display name and active status. Admin only.
        /// Maps IsActive (DTO) → IsDeleted (entity) via inversion.
        /// </summary>
        public async Task<ApiResponse> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            ValidationHelper.ModelValidation(request);

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
            {
                _logger.LogWarning("Failed to update user: User {UserId} not found.", id);
                return ApiResponseFactory.NotFound("User not found.");
            }

            user.DisplayName = request.DisplayName;
            user.IsDeleted = !request.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update user {UserId} due to identity errors.", id);
                var errors = updateResult.Errors.Select(e => e.Description).ToList();
                return ApiResponseFactory.BadRequest("Failed to update user.", errors);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var response = MapToUserResponse(user, roles.FirstOrDefault() ?? "");

            _logger.LogInformation("Admin successfully updated user {UserId}.", user.Id);
            return ApiResponseFactory.Success("User updated successfully.", response);
        }

        /// <summary>
        /// Gets all active support agents. Used for the ticket assignment dropdown.
        /// </summary>
        public async Task<ApiResponse> GetAgentsAsync(int pageNumber, int pageSize)
        {
            var roleName = UserRole.SupportAgent.ToString();
            var agentsInRole = await _userManager.GetUsersInRoleAsync(roleName);
            var agentIds = agentsInRole.Select(a => a.Id).ToHashSet();

            var query = _userManager.Users.AsNoTracking()
                .Where(a => !a.IsDeleted && agentIds.Contains(a.Id));

            var totalCount = await query.CountAsync();

            var agents = await query
                .OrderBy(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userResponses = agents.Select(a => MapToUserResponse(a, roleName)).ToList();

            var paginatedResult = new PaginatedResponse<UserResponse>
            {
                Items = userResponses,
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ApiResponseFactory.Success("Agents retrieved successfully.", paginatedResult);
        }

        // ── Private Helpers ──────────────────────────────────────────

        private static UserResponse MapToUserResponse(ApplicationUser user, string role)
        {
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? "",
                DisplayName = user.DisplayName,
                Role = role,
                IsActive = !user.IsDeleted,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
