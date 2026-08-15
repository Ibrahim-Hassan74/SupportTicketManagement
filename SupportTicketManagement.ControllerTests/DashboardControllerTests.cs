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
    public class DashboardControllerTests
    {
        private readonly Mock<IDashboardService> _dashboardServiceMock;
        private readonly DashboardController _dashboardController;

        public DashboardControllerTests()
        {
            _dashboardServiceMock = new Mock<IDashboardService>();
            _dashboardController = new DashboardController(_dashboardServiceMock.Object);
        }

        private void SetUserRoleAndId(Guid userId, UserRole role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            }, "mock"));

            _dashboardController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetStats_ShouldReturnOk_WhenUserIsAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Admin);

            var fakeResponse = ApiResponseFactory.Success("Success", new DashboardStatsResponse { TotalTickets = 10 });

            _dashboardServiceMock.Setup(s => s.GetStatsAsync("Admin"))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _dashboardController.GetStats();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var responseData = okResult.Value.Should().BeOfType<DashboardStatsResponse>().Subject;
            responseData.TotalTickets.Should().Be(10);
        }

        [Fact]
        public async Task GetAgentWorkload_ShouldReturnOk_WhenUserIsAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Admin);

            var fakeResponse = ApiResponseFactory.Success<IEnumerable<AgentWorkloadResponse>>("Success", new List<AgentWorkloadResponse>());

            _dashboardServiceMock.Setup(s => s.GetAgentWorkloadAsync("Admin"))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _dashboardController.GetAgentWorkload();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var responseData = okResult.Value.Should().BeAssignableTo<IEnumerable<AgentWorkloadResponse>>().Subject;
        }
    }
}
