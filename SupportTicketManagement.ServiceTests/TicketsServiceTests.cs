using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.Domain.IdentityEntities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.Services;
using System.Linq.Expressions;
using Xunit;

namespace SupportTicketManagement.ServiceTests
{
    public class TicketsServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITicketsRepository> _ticketsRepositoryMock;
        private readonly Mock<ITicketActivityRepository> _ticketActivityRepositoryMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly TicketsService _ticketsService;

        public TicketsServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _ticketsRepositoryMock = new Mock<ITicketsRepository>();
            _ticketActivityRepositoryMock = new Mock<ITicketActivityRepository>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            _unitOfWorkMock.Setup(u => u.TicketsRepository).Returns(_ticketsRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.TicketActivityRepository).Returns(_ticketActivityRepositoryMock.Object);

            _ticketsService = new TicketsService(_unitOfWorkMock.Object, _userManagerMock.Object, new Mock<ILogger<TicketsService>>().Object);
        }

        #region CreateTicketAsync

        [Fact]
        public async Task CreateTicketAsync_ShouldCreateTicket_WhenRequestIsValid()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var request = new CreateTicketRequest
            {
                Title = "Test Ticket",
                Description = "Test Description",
                Priority = TicketPriority.High
            };
            var customer = new ApplicationUser { Id = customerId, DisplayName = "John Doe" };

            _userManagerMock.Setup(m => m.FindByIdAsync(customerId.ToString())).ReturnsAsync(customer);

            var createdTicket = new Ticket
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = TicketStatus.Open,
                CustomerId = customerId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _ticketsRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Ticket>())).ReturnsAsync(createdTicket);

            // Act
            var result = await _ticketsService.CreateTicketAsync(request, customerId);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Should().BeOfType<ApiResponseWithData<TicketResponse>>();

            var dataResult = (ApiResponseWithData<TicketResponse>)result;
            dataResult.Data.Should().NotBeNull();
            dataResult.Data.Title.Should().Be(request.Title);
            dataResult.Data.Status.Should().Be("Open");
            dataResult.Data.CustomerId.Should().Be(customerId);
            dataResult.Data.CustomerName.Should().Be("John Doe");

            _ticketsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.Created)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateTicketAsync_ShouldReturnNotFound_WhenCustomerDoesNotExist()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var request = new CreateTicketRequest { Title = "T", Description = "D", Priority = TicketPriority.Low };
            _userManagerMock.Setup(m => m.FindByIdAsync(customerId.ToString())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _ticketsService.CreateTicketAsync(request, customerId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            _ticketsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateTicketAsync_ShouldSetDefaultStatusToOpen()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var request = new CreateTicketRequest { Title = "T", Description = "D", Priority = TicketPriority.Low };
            var customer = new ApplicationUser { Id = customerId, DisplayName = "John" };
            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(customer);
            _ticketsRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Ticket>())).ReturnsAsync(new Ticket { Id = Guid.NewGuid() });

            // Act
            await _ticketsService.CreateTicketAsync(request, customerId);

            // Assert
            _ticketsRepositoryMock.Verify(r => r.AddAsync(It.Is<Ticket>(t => t.Status == TicketStatus.Open)), Times.Once);
        }

        [Fact]
        public async Task CreateTicketAsync_ShouldCreateCreationActivity()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var request = new CreateTicketRequest { Title = "T", Description = "D", Priority = TicketPriority.Low };
            var customer = new ApplicationUser { Id = customerId, DisplayName = "John" };
            var ticketId = Guid.NewGuid();
            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(customer);
            _ticketsRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Ticket>())).ReturnsAsync(new Ticket { Id = ticketId });

            // Act
            await _ticketsService.CreateTicketAsync(request, customerId);

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.Created && a.TicketId == ticketId && a.UserId == customerId)), Times.Once);
        }

        #endregion

        #region GetTicketByIdForAdminAsync

        [Fact]
        public async Task GetTicketByIdForAdminAsync_ShouldReturnTicket_WhenTicketExists()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, Title = "Test" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<Expression<Func<Ticket, object>>>(), It.IsAny<Expression<Func<Ticket, object>>>()))
                .ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.GetTicketByIdForAdminAsync(ticketId);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Should().BeOfType<ApiResponseWithData<TicketResponse>>();
            var dataResult = (ApiResponseWithData<TicketResponse>)result;
            dataResult.Data.Id.Should().Be(ticketId);
        }

        [Fact]
        public async Task GetTicketByIdForAdminAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<Expression<Func<Ticket, object>>>(), It.IsAny<Expression<Func<Ticket, object>>>()))
                .ReturnsAsync((Ticket?)null);

            // Act
            var result = await _ticketsService.GetTicketByIdForAdminAsync(ticketId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region GetTicketByIdForAgentAsync

        [Fact]
        public async Task GetTicketByIdForAgentAsync_ShouldReturnTicket_WhenAgentIsAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, AssignedAgentId = agentId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<Expression<Func<Ticket, object>>>(), It.IsAny<Expression<Func<Ticket, object>>>()))
                .ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.GetTicketByIdForAgentAsync(ticketId, agentId);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetTicketByIdForAgentAsync_ShouldReturnNotFound_WhenAgentIsNotAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var otherAgentId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, AssignedAgentId = otherAgentId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<Expression<Func<Ticket, object>>>(), It.IsAny<Expression<Func<Ticket, object>>>()))
                .ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.GetTicketByIdForAgentAsync(ticketId, agentId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region GetTicketByIdForCustomerAsync

        [Fact]
        public async Task GetTicketByIdForCustomerAsync_ShouldReturnTicket_WhenCustomerOwnsTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, CustomerId = customerId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<Expression<Func<Ticket, object>>>(), It.IsAny<Expression<Func<Ticket, object>>>()))
                .ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.GetTicketByIdForCustomerAsync(ticketId, customerId);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetTicketByIdForCustomerAsync_ShouldReturnNotFound_WhenCustomerDoesNotOwnTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var otherCustomerId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, CustomerId = otherCustomerId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<Expression<Func<Ticket, object>>>(), It.IsAny<Expression<Func<Ticket, object>>>()))
                .ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.GetTicketByIdForCustomerAsync(ticketId, customerId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        #endregion

        #region UpdateTicketAsync

        [Fact]
        public async Task UpdateTicketAsync_ShouldUpdateTicket_WhenTicketExists()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketRequest { Title = "New Title", Description = "New Desc" };
            var adminId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, Title = "Old", Description = "Old" };

            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketAsync(ticketId, request, adminId);

            // Assert
            result.Success.Should().BeTrue();
            _ticketsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ticket>(t => t.Title == request.Title && t.Description == request.Description)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketRequest { Title = "T", Description = "D" };
            var adminId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _ticketsService.UpdateTicketAsync(ticketId, request, adminId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            _ticketsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Never);
        }

        #endregion

        #region UpdateTicketPriorityAsync

        [Fact]
        public async Task UpdateTicketPriorityAsync_ShouldUpdatePriority_WhenTicketExists()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketPriorityRequest { Priority = TicketPriority.Critical };
            var adminId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, Priority = TicketPriority.Low };

            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketPriorityAsync(ticketId, request, adminId);

            // Assert
            result.Success.Should().BeTrue();
            _ticketsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ticket>(t => t.Priority == TicketPriority.Critical)), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketPriorityAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketPriorityRequest { Priority = TicketPriority.High };
            var adminId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _ticketsService.UpdateTicketPriorityAsync(ticketId, request, adminId);

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateTicketPriorityAsync_ShouldCreateActivity_WhenPriorityChanges()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketPriorityRequest { Priority = TicketPriority.Critical };
            var adminId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, Priority = TicketPriority.Low };

            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            await _ticketsService.UpdateTicketPriorityAsync(ticketId, request, adminId);

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.PriorityChanged && a.OldValue == "Low" && a.NewValue == "Critical")), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketPriorityAsync_ShouldNotCreateActivity_WhenPriorityIsSame()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketPriorityRequest { Priority = TicketPriority.High };
            var adminId = Guid.NewGuid();
            var ticket = new Ticket { Id = ticketId, Priority = TicketPriority.High };

            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            await _ticketsService.UpdateTicketPriorityAsync(ticketId, request, adminId);

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TicketActivity>()), Times.Never);
        }

        #endregion

        #region AssignAgentAsync

        [Fact]
        public async Task AssignAgentAsync_ShouldAssignAgent_WhenRequestIsValid()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var request = new AssignTicketRequest { AgentId = agentId };
            
            var ticket = new Ticket { Id = ticketId };
            var agent = new ApplicationUser { Id = agentId, DisplayName = "Agent Bob" };

            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);
            _userManagerMock.Setup(m => m.FindByIdAsync(agentId.ToString())).ReturnsAsync(agent);
            _userManagerMock.Setup(m => m.IsInRoleAsync(agent, UserRole.SupportAgent.ToString())).ReturnsAsync(true);

            // Act
            var result = await _ticketsService.AssignAgentAsync(ticketId, request, adminId);

            // Assert
            result.Success.Should().BeTrue();
            _ticketsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ticket>(t => t.AssignedAgentId == request.AgentId)), Times.Once);
        }

        [Fact]
        public async Task AssignAgentAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new AssignTicketRequest { AgentId = agentId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _ticketsService.AssignAgentAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task AssignAgentAsync_ShouldReturnNotFound_WhenAgentDoesNotExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new AssignTicketRequest { AgentId = agentId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket());
            _userManagerMock.Setup(m => m.FindByIdAsync(agentId.ToString())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _ticketsService.AssignAgentAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task AssignAgentAsync_ShouldReturnBadRequest_WhenUserIsNotAgent()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new AssignTicketRequest { AgentId = agentId };
            var agent = new ApplicationUser { Id = agentId };

            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket());
            _userManagerMock.Setup(m => m.FindByIdAsync(agentId.ToString())).ReturnsAsync(agent);
            _userManagerMock.Setup(m => m.IsInRoleAsync(agent, UserRole.SupportAgent.ToString())).ReturnsAsync(false);

            // Act
            var result = await _ticketsService.AssignAgentAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("not a Support Agent");
        }

        [Fact]
        public async Task AssignAgentAsync_ShouldCreateActivity_WhenAgentIsAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new AssignTicketRequest { AgentId = agentId };
            var agent = new ApplicationUser { Id = agentId };

            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket());
            _userManagerMock.Setup(m => m.FindByIdAsync(agentId.ToString())).ReturnsAsync(agent);
            _userManagerMock.Setup(m => m.IsInRoleAsync(agent, UserRole.SupportAgent.ToString())).ReturnsAsync(true);

            // Act
            await _ticketsService.AssignAgentAsync(ticketId, request, Guid.NewGuid());

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.AgentAssigned && a.NewValue == agentId.ToString())), Times.Once);
        }

        #endregion

        #region UpdateTicketStatusByAdminAsync

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldChangeStatus_WhenTransitionIsValid()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.InProgress };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Open };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldReject_WhenOpenToResolved()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Resolved };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Open };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Cannot resolve an open ticket. Must be In Progress first.");
        }

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldReject_WhenClosedToNonOpen()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.InProgress };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Closed };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("A closed ticket can only be reopened");
        }

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.InProgress };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldSetResolvedAt_WhenStatusBecomesResolved()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Resolved };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.InProgress };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            _ticketsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ticket>(t => t.ResolvedAt != null)), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldSetClosedAt_WhenStatusBecomesClosed()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Closed };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Resolved };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            _ticketsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ticket>(t => t.ClosedAt != null)), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldCreateActivity_WhenStatusChanges()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.InProgress };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Open };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.StatusChanged && a.OldValue == "Open" && a.NewValue == "InProgress")), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketStatusByAdminAsync_ShouldCreateClosedActivity_WhenStatusBecomesClosed()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Closed };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Resolved };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.Closed)), Times.Once);
        }

        #endregion

        #region UpdateTicketStatusByAgentAsync

        [Fact]
        public async Task UpdateTicketStatusByAgentAsync_ShouldReturnNotFound_WhenAgentNotAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var otherAgentId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Resolved };
            var ticket = new Ticket { Id = ticketId, AssignedAgentId = otherAgentId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAgentAsync(ticketId, request, agentId);

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateTicketStatusByAgentAsync_ShouldAllowResolve_WhenAgentIsAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Resolved };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.InProgress, AssignedAgentId = agentId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAgentAsync(ticketId, request, agentId);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateTicketStatusByAgentAsync_ShouldRejectClose_BecauseAgentCannotClose()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Closed };
            var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Resolved, AssignedAgentId = agentId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAgentAsync(ticketId, request, agentId);

            // Assert
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("You are not authorized to close tickets.");
        }

        #endregion

        #region UpdateTicketStatusByCustomerAsync

        [Fact]
        public async Task UpdateTicketStatusByCustomerAsync_ShouldReturnNotFound_WhenCustomerDoesNotOwnTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Closed };
            var ticket = new Ticket { Id = ticketId, CustomerId = Guid.NewGuid() };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByCustomerAsync(ticketId, request, customerId);

            // Assert
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateTicketStatusByCustomerAsync_ShouldRejectInProgress()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.InProgress };
            var ticket = new Ticket { Id = ticketId, CustomerId = customerId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByCustomerAsync(ticketId, request, customerId);

            // Assert
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Customers cannot set status to InProgress or Resolved.");
        }

        [Fact]
        public async Task UpdateTicketStatusByCustomerAsync_ShouldRejectResolved()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Resolved };
            var ticket = new Ticket { Id = ticketId, CustomerId = customerId };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByCustomerAsync(ticketId, request, customerId);

            // Assert
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UpdateTicketStatusByCustomerAsync_ShouldAllowClose_WhenCustomerOwnsTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Closed };
            var ticket = new Ticket { Id = ticketId, CustomerId = customerId, Status = TicketStatus.Resolved };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByCustomerAsync(ticketId, request, customerId);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateTicketStatusByCustomerAsync_ShouldAllowReopen_WhenTicketIsClosed()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = TicketStatus.Open };
            var ticket = new Ticket { Id = ticketId, CustomerId = customerId, Status = TicketStatus.Closed };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByCustomerAsync(ticketId, request, customerId);

            // Assert
            result.Success.Should().BeTrue();
        }

        #endregion

        #region Status Transition Matrix

        [Theory]
        [InlineData(TicketStatus.Open, TicketStatus.InProgress)]
        [InlineData(TicketStatus.Open, TicketStatus.Closed)]
        [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
        [InlineData(TicketStatus.InProgress, TicketStatus.Open)]
        [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
        [InlineData(TicketStatus.Resolved, TicketStatus.Open)]
        [InlineData(TicketStatus.Closed, TicketStatus.Open)]
        public async Task StatusTransition_ShouldSucceed_ForValidTransitions(TicketStatus current, TicketStatus target)
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = target };
            var ticket = new Ticket { Id = ticketId, Status = current };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.Success.Should().BeTrue();
        }

        [Theory]
        [InlineData(TicketStatus.Open, TicketStatus.Resolved)]
        [InlineData(TicketStatus.Closed, TicketStatus.InProgress)]
        [InlineData(TicketStatus.Closed, TicketStatus.Resolved)]
        public async Task StatusTransition_ShouldFail_ForInvalidTransitions(TicketStatus current, TicketStatus target)
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketStatusRequest { Status = target };
            var ticket = new Ticket { Id = ticketId, Status = current };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);

            // Act
            var result = await _ticketsService.UpdateTicketStatusByAdminAsync(ticketId, request, Guid.NewGuid());

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        #endregion
    }
}
