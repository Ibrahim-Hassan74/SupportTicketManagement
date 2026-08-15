using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SupportTicketManagement.Core.Domain.IdentityEntities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.ServiceContracts;
using SupportTicketManagement.Core.Services;
using System.Security.Claims;
using Xunit;

namespace SupportTicketManagement.ServiceTests
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(_userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _jwtServiceMock = new Mock<IJwtService>();

            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(roleStoreMock.Object, null, null, null, null);

            _configurationMock = new Mock<IConfiguration>();

            _authenticationService = new AuthenticationService(_userManagerMock.Object, _signInManagerMock.Object, _httpContextAccessorMock.Object, _jwtServiceMock.Object, _roleManagerMock.Object, _configurationMock.Object);
        }

        #region RegisterAsync

        [Fact]
        public async Task RegisterAsync_ShouldRegisterUser_WhenRequestIsValid()
        {
            // Arrange
            var request = new RegisterDTO { Email = "test@test.com", Password = "Password123!", ConfirmPassword = "Password123!", UserName = "TestUser", Phone = "1234567890" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password)).ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(m => m.RoleExistsAsync(UserRole.Customer.ToString())).ReturnsAsync(true);

            // Act
            var result = await _authenticationService.RegisterAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once);
            _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Customer.ToString()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnConflict_WhenEmailAlreadyExists()
        {
            // Arrange
            var request = new RegisterDTO { Email = "test@test.com", Password = "Password123!", ConfirmPassword = "Password123!", UserName = "Test", Phone = "1234567890" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(new ApplicationUser());

            // Act
            var result = await _authenticationService.RegisterAsync(request);

            // Assert
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnBadRequest_WhenCreateAsyncFails()
        {
            // Arrange
            var request = new RegisterDTO { Email = "new@test.com", Password = "WeakPassword!", ConfirmPassword = "WeakPassword!", UserName = "Test", Phone = "1234567890" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Weak" }));

            // Act
            var result = await _authenticationService.RegisterAsync(request);

            // Assert
            result.StatusCode.Should().Be(400);
            ((ApiErrorResponse)result).Errors.Should().Contain("Weak");
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnBadRequest_WhenDTOIsNull()
        {
            // Act
            var result = await _authenticationService.RegisterAsync(null!);

            // Assert
            result.StatusCode.Should().Be(400);
        }

        #endregion

        #region LoginAsync

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var request = new LoginDTO { Email = "test@test.com", Password = "Password123!", RememberMe = false };
            var user = new ApplicationUser { Email = "test@test.com", IsDeleted = false };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, request.Password, request.RememberMe, true)).ReturnsAsync(SignInResult.Success);
            _jwtServiceMock.Setup(m => m.CreateJwtToken(user, request.RememberMe)).ReturnsAsync(new ApiSuccessResponse { Token = "token123", RefreshToken = "refresh123" });

            // Act
            var result = await _authenticationService.LoginAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<ApiSuccessResponse>();
            var successResult = (ApiSuccessResponse)result;
            successResult.Token.Should().Be("token123");
            successResult.RefreshToken.Should().Be("refresh123");
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNotFound_WhenEmailDoesNotExist()
        {
            // Arrange
            var request = new LoginDTO { Email = "not@exist.com", Password = "Password123!" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authenticationService.LoginAsync(request);

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnForbidden_WhenUserIsDeactivated()
        {
            // Arrange
            var request = new LoginDTO { Email = "test@test.com", Password = "Password123!" };
            var user = new ApplicationUser { Email = "test@test.com", IsDeleted = true };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);

            // Act
            var result = await _authenticationService.LoginAsync(request);

            // Assert
            result.StatusCode.Should().Be(403);
            result.Message.Should().Contain("deactivated");
        }

        [Fact]
        public async Task LoginAsync_ShouldReturn423_WhenAccountIsLockedOut()
        {
            // Arrange
            var request = new LoginDTO { Email = "test@test.com", Password = "Password123!" };
            var user = new ApplicationUser { Email = "test@test.com", IsDeleted = false };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, request.Password, false, true)).ReturnsAsync(SignInResult.LockedOut);

            // Act
            var result = await _authenticationService.LoginAsync(request);

            // Assert
            result.StatusCode.Should().Be(423);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturn401_WhenPasswordIsWrong()
        {
            // Arrange
            var request = new LoginDTO { Email = "test@test.com", Password = "WrongPassword1!" };
            var user = new ApplicationUser { Email = "test@test.com", IsDeleted = false };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, request.Password, false, true)).ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _authenticationService.LoginAsync(request);

            // Assert
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnForbidden_WhenLoginNotAllowed()
        {
            // Arrange
            var request = new LoginDTO { Email = "test@test.com", Password = "Password123!" };
            var user = new ApplicationUser { Email = "test@test.com", IsDeleted = false };
            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, request.Password, false, true)).ReturnsAsync(SignInResult.NotAllowed);

            // Act
            var result = await _authenticationService.LoginAsync(request);

            // Assert
            result.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnBadRequest_WhenDTOIsNull()
        {
            // Act
            var result = await _authenticationService.LoginAsync(null!);

            // Assert
            result.StatusCode.Should().Be(400);
        }

        #endregion

        #region LogoutAsync

        [Fact]
        public async Task LogoutAsync_ShouldClearRefreshToken_WhenEmailProvided()
        {
            // Arrange
            var email = "test@test.com";
            var user = new ApplicationUser { Email = email, RefreshToken = "some_token" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);

            // Act
            var result = await _authenticationService.LogoutAsync(email);

            // Assert
            result.Success.Should().BeTrue();
            user.RefreshToken.Should().BeNull();
            _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
            _signInManagerMock.Verify(m => m.SignOutAsync(), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_ShouldOnlySignOut_WhenEmailIsNull()
        {
            // Act
            await _authenticationService.LogoutAsync(null);

            // Assert
            _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _signInManagerMock.Verify(m => m.SignOutAsync(), Times.Once);
        }

        #endregion

        #region RefreshTokenAsync

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNewToken_WhenRefreshTokenIsValid()
        {
            // Arrange
            var model = new TokenModel { Token = "jwt", RefreshToken = "refresh" };
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "test@test.com") };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            
            _jwtServiceMock.Setup(m => m.GetPrincipalFromJwtToken(model.Token)).Returns(principal);
            
            var user = new ApplicationUser { Email = "test@test.com", RefreshToken = "refresh", RefreshTokenExpirationDateTime = DateTimeOffset.UtcNow.AddDays(1) };
            _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

            var newAuthResponse = new ApiSuccessResponse { Token = "new_jwt", RefreshToken = "new_refresh" };
            _jwtServiceMock.Setup(m => m.CreateJwtToken(user, false)).ReturnsAsync(newAuthResponse);

            // Act
            var result = await _authenticationService.RefreshTokenAsync(model);

            // Assert
            result.Success.Should().BeTrue();
            var successResult = (ApiSuccessResponse)result;
            successResult.Token.Should().Be("new_jwt");
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnFailure_WhenTokenIsInvalid()
        {
            // Arrange
            var model = new TokenModel { Token = "bad", RefreshToken = "refresh" };
            _jwtServiceMock.Setup(m => m.GetPrincipalFromJwtToken(model.Token)).Throws(new SecurityTokenException("Invalid"));

            // Act
            var result = await _authenticationService.RefreshTokenAsync(model);

            // Assert
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnForbidden_WhenUserIsDeactivated()
        {
            // Arrange
            var model = new TokenModel { Token = "jwt", RefreshToken = "refresh" };
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, "test@test.com") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            _jwtServiceMock.Setup(m => m.GetPrincipalFromJwtToken(model.Token)).Returns(principal);

            var user = new ApplicationUser { Email = "test@test.com", IsDeleted = true };
            _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

            // Act
            var result = await _authenticationService.RefreshTokenAsync(model);

            // Assert
            result.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnFailure_WhenRefreshTokenExpired()
        {
            // Arrange
            var model = new TokenModel { Token = "jwt", RefreshToken = "refresh" };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Email, "test@test.com") }));
            _jwtServiceMock.Setup(m => m.GetPrincipalFromJwtToken(model.Token)).Returns(principal);

            var user = new ApplicationUser { Email = "test@test.com", RefreshToken = "refresh", RefreshTokenExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(-5) };
            _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

            // Act
            var result = await _authenticationService.RefreshTokenAsync(model);

            // Assert
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnFailure_WhenRefreshTokenDoesNotMatch()
        {
            // Arrange
            var model = new TokenModel { Token = "jwt", RefreshToken = "refresh" };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Email, "test@test.com") }));
            _jwtServiceMock.Setup(m => m.GetPrincipalFromJwtToken(model.Token)).Returns(principal);

            var user = new ApplicationUser { Email = "test@test.com", RefreshToken = "different", RefreshTokenExpirationDateTime = DateTimeOffset.UtcNow.AddDays(1) };
            _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

            // Act
            var result = await _authenticationService.RefreshTokenAsync(model);

            // Assert
            result.StatusCode.Should().Be(400);
        }

        #endregion

        #region GetUserByIdAsync

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var user = new ApplicationUser { Id = Guid.Parse(userId), Email = "t@t.com", DisplayName = "Name" };
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            // Act
            var result = await _authenticationService.GetUserByIdAsync(userId);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authenticationService.GetUserByIdAsync(Guid.NewGuid().ToString());

            // Assert
            result.StatusCode.Should().Be(404);
        }

        #endregion
    }
}
