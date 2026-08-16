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
    public class TimeEntriesServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITicketsRepository> _ticketsRepositoryMock;
        private readonly Mock<ITimeEntryRepository> _timeEntryRepositoryMock;
        private readonly Mock<ITicketActivityRepository> _ticketActivityRepositoryMock;
        private readonly TimeEntriesService _timeEntriesService;

        public TimeEntriesServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _ticketsRepositoryMock = new Mock<ITicketsRepository>();
            _timeEntryRepositoryMock = new Mock<ITimeEntryRepository>();
            _ticketActivityRepositoryMock = new Mock<ITicketActivityRepository>();

            _unitOfWorkMock.Setup(u => u.TicketsRepository).Returns(_ticketsRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.TimeEntryRepository).Returns(_timeEntryRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.TicketActivityRepository).Returns(_ticketActivityRepositoryMock.Object);

            _timeEntriesService = new TimeEntriesService(_unitOfWorkMock.Object, new Mock<ILogger<TimeEntriesService>>().Object);
        }

        #region GetTimeEntriesAsync

        [Fact]
        public async Task GetTimeEntriesAsync_ShouldReturnEntries_WhenAdminAccesses()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId });

            var timeEntries = new List<TimeEntry>
            {
                new TimeEntry { Id = Guid.NewGuid(), DurationMinutes = 30, WorkDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) },
                new TimeEntry { Id = Guid.NewGuid(), DurationMinutes = 60, WorkDate = DateOnly.FromDateTime(DateTime.UtcNow) }
            };

            _timeEntryRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TimeEntry, bool>>>(), It.IsAny<Expression<Func<TimeEntry, object>>>()))
                .ReturnsAsync(timeEntries);

            // Act
            var result = await _timeEntriesService.GetTimeEntriesAsync(ticketId, userId, "Admin");

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<ApiResponseWithData<TicketTimeEntriesResponse>>();
            var dataResult = (ApiResponseWithData<TicketTimeEntriesResponse>)result;
            dataResult.Data.TotalDurationMinutes.Should().Be(90);
            dataResult.Data.Entries.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTimeEntriesAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _timeEntriesService.GetTimeEntriesAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin");

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetTimeEntriesAsync_ShouldReturnNotFound_WhenCustomerAccesses()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, CustomerId = Guid.NewGuid() });

            // Act
            var result = await _timeEntriesService.GetTimeEntriesAsync(ticketId, Guid.NewGuid(), "Customer");

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetTimeEntriesAsync_ShouldReturnNotFound_WhenAgentNotAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = Guid.NewGuid() });

            // Act
            var result = await _timeEntriesService.GetTimeEntriesAsync(ticketId, agentId, "SupportAgent");

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetTimeEntriesAsync_ShouldReturnEntries_WhenAssignedAgentAccesses()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = agentId });
            _timeEntryRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TimeEntry, bool>>>(), It.IsAny<Expression<Func<TimeEntry, object>>>()))
                .ReturnsAsync(new List<TimeEntry>());

            // Act
            var result = await _timeEntriesService.GetTimeEntriesAsync(ticketId, agentId, "SupportAgent");

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetTimeEntriesAsync_ShouldCalculateTotalDurationCorrectly()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = agentId });

            var timeEntries = new List<TimeEntry>
            {
                new TimeEntry { DurationMinutes = 30 },
                new TimeEntry { DurationMinutes = 60 },
                new TimeEntry { DurationMinutes = 90 }
            };

            _timeEntryRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TimeEntry, bool>>>(), It.IsAny<Expression<Func<TimeEntry, object>>>()))
                .ReturnsAsync(timeEntries);

            // Act
            var result = await _timeEntriesService.GetTimeEntriesAsync(ticketId, agentId, "SupportAgent");

            // Assert
            var dataResult = (ApiResponseWithData<TicketTimeEntriesResponse>)result;
            dataResult.Data.TotalDurationMinutes.Should().Be(180);
        }

        #endregion

        #region AddTimeEntryAsync

        [Fact]
        public async Task AddTimeEntryAsync_ShouldAddEntry_WhenRequestIsValid()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new CreateTimeEntryRequest { DurationMinutes = 60, WorkDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), Description = "Test" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = agentId });

            // Act
            var result = await _timeEntriesService.AddTimeEntryAsync(ticketId, request, agentId);

            // Assert
            result.Success.Should().BeTrue();
            _timeEntryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TimeEntry>()), Times.Once);
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.TimeLogged)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task AddTimeEntryAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new CreateTimeEntryRequest { DurationMinutes = 60, WorkDate = DateOnly.FromDateTime(DateTime.UtcNow), Description = "Test" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _timeEntriesService.AddTimeEntryAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task AddTimeEntryAsync_ShouldReturnNotFound_WhenAgentNotAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new CreateTimeEntryRequest { DurationMinutes = 60, WorkDate = DateOnly.FromDateTime(DateTime.UtcNow), Description = "Test" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = Guid.NewGuid() });

            // Act
            var result = await _timeEntriesService.AddTimeEntryAsync(ticketId, request, agentId);

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task AddTimeEntryAsync_ShouldReturnBadRequest_WhenWorkDateIsInFuture()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new CreateTimeEntryRequest { DurationMinutes = 60, WorkDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Description = "Test" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = agentId });

            // Act
            var result = await _timeEntriesService.AddTimeEntryAsync(ticketId, request, agentId);

            // Assert
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Work date cannot be in the future");
            _timeEntryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TimeEntry>()), Times.Never);
        }

        [Fact]
        public async Task AddTimeEntryAsync_ShouldCreateTimeLoggedActivity()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new CreateTimeEntryRequest { DurationMinutes = 45, WorkDate = DateOnly.FromDateTime(DateTime.UtcNow), Description = "Test" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = agentId });

            // Act
            await _timeEntriesService.AddTimeEntryAsync(ticketId, request, agentId);

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Description.Contains("45"))), Times.Once);
        }

        #endregion
    }
}
