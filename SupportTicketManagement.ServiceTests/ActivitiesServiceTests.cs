using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.Services;
using System.Linq.Expressions;
using Xunit;

namespace SupportTicketManagement.ServiceTests
{
    public class ActivitiesServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITicketsRepository> _ticketsRepositoryMock;
        private readonly Mock<ITicketActivityRepository> _ticketActivityRepositoryMock;
        private readonly ActivitiesService _activitiesService;

        public ActivitiesServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _ticketsRepositoryMock = new Mock<ITicketsRepository>();
            _ticketActivityRepositoryMock = new Mock<ITicketActivityRepository>();

            _unitOfWorkMock.Setup(u => u.TicketsRepository).Returns(_ticketsRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.TicketActivityRepository).Returns(_ticketActivityRepositoryMock.Object);

            _activitiesService = new ActivitiesService(_unitOfWorkMock.Object, new Mock<ILogger<ActivitiesService>>().Object);
        }

        #region GetActivitiesAsync

        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnActivities_WhenAdminAccessesTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId });

            var activities = new List<TicketActivity>
            {
                new TicketActivity { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5), Type = ActivityType.Created },
                new TicketActivity { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow, Type = ActivityType.StatusChanged }
            };

            _ticketActivityRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TicketActivity, bool>>>(), It.IsAny<Expression<Func<TicketActivity, object>>>()))
                .ReturnsAsync(activities);

            // Act
            var result = await _activitiesService.GetActivitiesAsync(ticketId, userId, "Admin");

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<ApiResponseWithData<List<ActivityResponse>>>();
            var dataResult = (ApiResponseWithData<List<ActivityResponse>>)result;
            dataResult.Data.Should().HaveCount(2);
            dataResult.Data[0].Id.Should().Be(activities[0].Id);
        }

        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _activitiesService.GetActivitiesAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnNotFound_WhenAgentNotAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = Guid.NewGuid() });

            // Act
            var result = await _activitiesService.GetActivitiesAsync(ticketId, agentId, "SupportAgent");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnNotFound_WhenCustomerDoesNotOwnTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, CustomerId = Guid.NewGuid() });

            // Act
            var result = await _activitiesService.GetActivitiesAsync(ticketId, customerId, "Customer");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnEmptyList_WhenNoActivitiesExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId });
            _ticketActivityRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TicketActivity, bool>>>(), It.IsAny<Expression<Func<TicketActivity, object>>>()))
                .ReturnsAsync(new List<TicketActivity>());

            // Act
            var result = await _activitiesService.GetActivitiesAsync(ticketId, Guid.NewGuid(), "Admin");

            // Assert
            result.Success.Should().BeTrue();
            var dataResult = (ApiResponseWithData<List<ActivityResponse>>)result;
            dataResult.Data.Should().BeEmpty();
        }

        #endregion
    }
}
