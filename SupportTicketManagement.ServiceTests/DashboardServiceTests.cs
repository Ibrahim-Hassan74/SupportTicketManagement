using FluentAssertions;
using Moq;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.Services;
using Xunit;

namespace SupportTicketManagement.ServiceTests
{
    public class DashboardServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITicketsRepository> _ticketsRepositoryMock;
        private readonly DashboardService _dashboardService;

        public DashboardServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _ticketsRepositoryMock = new Mock<ITicketsRepository>();

            _unitOfWorkMock.Setup(u => u.TicketsRepository).Returns(_ticketsRepositoryMock.Object);

            _dashboardService = new DashboardService(_unitOfWorkMock.Object);
        }

        #region GetStatsAsync

        [Fact]
        public async Task GetStatsAsync_ShouldReturnStats_WhenRoleIsAdmin()
        {
            // Arrange
            var stats = new DashboardStatsResponse { TotalTickets = 10 };
            _ticketsRepositoryMock.Setup(r => r.GetDashboardStatsAsync()).ReturnsAsync(stats);

            // Act
            var result = await _dashboardService.GetStatsAsync("Admin");

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<ApiResponseWithData<DashboardStatsResponse>>();
            var dataResult = (ApiResponseWithData<DashboardStatsResponse>)result;
            dataResult.Data.TotalTickets.Should().Be(10);
        }

        [Fact]
        public async Task GetStatsAsync_ShouldReturnForbidden_WhenRoleIsNotAdmin()
        {
            // Act
            var result = await _dashboardService.GetStatsAsync("SupportAgent");

            // Assert
            result.StatusCode.Should().Be(403);
            _ticketsRepositoryMock.Verify(r => r.GetDashboardStatsAsync(), Times.Never);
        }

        [Fact]
        public async Task GetStatsAsync_ShouldReturnForbidden_WhenRoleIsCustomer()
        {
            // Act
            var result = await _dashboardService.GetStatsAsync("Customer");

            // Assert
            result.StatusCode.Should().Be(403);
        }

        #endregion

        #region GetAgentWorkloadAsync

        [Fact]
        public async Task GetAgentWorkloadAsync_ShouldReturnWorkload_WhenRoleIsAdmin()
        {
            // Arrange
            var workload = new List<AgentWorkloadResponse>
            {
                new AgentWorkloadResponse { AgentName = "Test Agent", OpenTickets = 5 }
            };
            _ticketsRepositoryMock.Setup(r => r.GetAgentWorkloadAsync()).ReturnsAsync(workload);

            // Act
            var result = await _dashboardService.GetAgentWorkloadAsync("Admin");

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<ApiResponseWithData<IEnumerable<AgentWorkloadResponse>>>();
            var dataResult = (ApiResponseWithData<IEnumerable<AgentWorkloadResponse>>)result;
            dataResult.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAgentWorkloadAsync_ShouldReturnForbidden_WhenRoleIsNotAdmin()
        {
            // Act
            var result = await _dashboardService.GetAgentWorkloadAsync("SupportAgent");

            // Assert
            result.StatusCode.Should().Be(403);
        }

        #endregion

        #region GetTicketTrendsAsync

        [Fact]
        public async Task GetTicketTrendsAsync_ShouldReturnTrends_WhenValidRequest()
        {
            // Arrange
            var trends = new List<TicketTrendResponse>
            {
                new TicketTrendResponse { Date = DateOnly.FromDateTime(DateTime.UtcNow), OpenCount = 2 }
            };
            _ticketsRepositoryMock.Setup(r => r.GetTicketTrendsAsync(30)).ReturnsAsync(trends);

            // Act
            var result = await _dashboardService.GetTicketTrendsAsync(30, "Admin");

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<ApiResponseWithData<IEnumerable<TicketTrendResponse>>>();
            var dataResult = (ApiResponseWithData<IEnumerable<TicketTrendResponse>>)result;
            dataResult.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetTicketTrendsAsync_ShouldReturnForbidden_WhenRoleIsNotAdmin()
        {
            // Act
            var result = await _dashboardService.GetTicketTrendsAsync(30, "Customer");

            // Assert
            result.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetTicketTrendsAsync_ShouldReturnBadRequest_WhenDaysIsZero()
        {
            // Act
            var result = await _dashboardService.GetTicketTrendsAsync(0, "Admin");

            // Assert
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetTicketTrendsAsync_ShouldReturnBadRequest_WhenDaysExceeds365()
        {
            // Act
            var result = await _dashboardService.GetTicketTrendsAsync(400, "Admin");

            // Assert
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetTicketTrendsAsync_ShouldReturnBadRequest_WhenDaysIsNegative()
        {
            // Act
            var result = await _dashboardService.GetTicketTrendsAsync(-5, "Admin");

            // Assert
            result.StatusCode.Should().Be(400);
        }

        #endregion
    }
}
