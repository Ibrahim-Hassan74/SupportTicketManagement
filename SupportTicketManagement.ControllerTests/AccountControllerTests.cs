using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SupportTicketManagement.API.Controllers;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.ServiceContracts;
using Xunit;

namespace SupportTicketManagement.ControllerTests
{
    public class AccountControllerTests
    {
        private readonly Mock<IAuthenticationService> _authServiceMock;
        private readonly AccountController _accountController;

        public AccountControllerTests()
        {
            _authServiceMock = new Mock<IAuthenticationService>();
            _accountController = new AccountController(_authServiceMock.Object);
        }

        [Fact]
        public async Task PostLogin_ShouldReturnOkWithToken_WhenCredentialsAreValid()
        {
            // Arrange
            var request = new LoginDTO { Email = "test@test.com", Password = "Password123!" };
            var fakeResponse = new ApiSuccessResponse { Token = "jwt-token", Email = "test@test.com", Success = true, StatusCode = 200 };

            _authServiceMock.Setup(s => s.LoginAsync(request))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _accountController.PostLogin(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(200);
            var apiResponse = objectResult.Value.Should().BeOfType<ApiSuccessResponse>().Subject;
            apiResponse.Success.Should().BeTrue();
        }

        [Fact]
        public async Task PostLogin_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var request = new LoginDTO { Email = "test@test.com", Password = "WrongPassword1!" };
            var fakeResponse = ApiResponseFactory.Unauthorized("Invalid credentials");

            _authServiceMock.Setup(s => s.LoginAsync(request))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _accountController.PostLogin(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(401);
            var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse>().Subject;
            apiResponse.Message.Should().Be("Invalid credentials");
        }

        [Fact]
        public async Task PostRegister_ShouldReturnOk_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var request = new RegisterDTO { Email = "test@test.com", Password = "Password123!" };
            var fakeResponse = ApiResponseFactory.Success("Registration successful");

            _authServiceMock.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _accountController.PostRegister(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(200);
            var apiResponse = objectResult.Value.Should().BeAssignableTo<ApiResponse>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("Registration successful");
        }
    }
}
