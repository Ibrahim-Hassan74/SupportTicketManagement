using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SupportTicketManagement.API.Controllers;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.ServiceContracts;
using System.Security.Claims;
using Xunit;

namespace SupportTicketManagement.ControllerTests
{
    public class UsersControllerTests
    {
        private readonly Mock<IUsersService> _usersServiceMock;
        private readonly UsersController _usersController;

        public UsersControllerTests()
        {
            _usersServiceMock = new Mock<IUsersService>();
            _usersController = new UsersController(_usersServiceMock.Object);
        }

        private void SetUserRoleAndId(Guid userId, UserRole role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            }, "mock"));

            _usersController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetUsers_ShouldReturnOk_WhenUserIsAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Admin);

            var request = new UserQueryRequest();
            var fakeResponse = ApiResponseFactory.Success("Success", new PaginatedResponse<UserResponse>());

            _usersServiceMock.Setup(s => s.GetUsersAsync(request))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _usersController.GetUsers(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeOfType<PaginatedResponse<UserResponse>>();
        }

        [Fact]
        public async Task CreateUser_ShouldReturnOk_WhenServiceReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Admin);

            var request = new CreateUserRequest { Email = "test@test.com" };
            var fakeResponse = ApiResponseFactory.Success("Created", new UserResponse { Email = "test@test.com" });

            _usersServiceMock.Setup(s => s.CreateUserAsync(request))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _usersController.CreateUser(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var responseData = okResult.Value.Should().BeOfType<UserResponse>().Subject;
            responseData.Email.Should().Be("test@test.com");
        }

        [Fact]
        public async Task UpdateUser_ShouldReturnBadRequest_WhenServiceReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Admin);

            var targetUserId = Guid.NewGuid();
            var request = new UpdateUserRequest { DisplayName = "Bad" };
            var fakeResponse = ApiResponseFactory.BadRequest("Invalid data");

            _usersServiceMock.Setup(s => s.UpdateUserAsync(targetUserId, request))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _usersController.UpdateUser(targetUserId, request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse>().Subject;
            apiResponse.Message.Should().Be("Invalid data");
        }
    }
}
