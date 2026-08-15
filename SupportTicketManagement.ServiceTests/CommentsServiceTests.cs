using FluentAssertions;
using Moq;
using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.Services;
using System.Linq.Expressions;
using Xunit;

namespace SupportTicketManagement.ServiceTests
{
    public class CommentsServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITicketsRepository> _ticketsRepositoryMock;
        private readonly Mock<ITicketCommentRepository> _ticketCommentRepositoryMock;
        private readonly Mock<ITicketActivityRepository> _ticketActivityRepositoryMock;
        private readonly CommentsService _commentsService;

        public CommentsServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _ticketsRepositoryMock = new Mock<ITicketsRepository>();
            _ticketCommentRepositoryMock = new Mock<ITicketCommentRepository>();
            _ticketActivityRepositoryMock = new Mock<ITicketActivityRepository>();

            _unitOfWorkMock.Setup(u => u.TicketsRepository).Returns(_ticketsRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.TicketCommentRepository).Returns(_ticketCommentRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.TicketActivityRepository).Returns(_ticketActivityRepositoryMock.Object);

            _commentsService = new CommentsService(_unitOfWorkMock.Object);
        }

        #region GetCommentsAsync

        [Fact]
        public async Task GetCommentsAsync_ShouldReturnComments_WhenAdminAccessesTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId });

            var comments = new List<TicketComment>
            {
                new TicketComment { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) },
                new TicketComment { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow }
            };

            _ticketCommentRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TicketComment, bool>>>(), It.IsAny<Expression<Func<TicketComment, object>>>()))
                .ReturnsAsync(comments);

            // Act
            var result = await _commentsService.GetCommentsAsync(ticketId, userId, "Admin");

            // Assert
            result.Success.Should().BeTrue();
            result.Should().BeOfType<ApiResponseWithData<List<CommentResponse>>>();
            var dataResult = (ApiResponseWithData<List<CommentResponse>>)result;
            dataResult.Data.Should().HaveCount(2);
            dataResult.Data[0].Id.Should().Be(comments[0].Id);
        }

        [Fact]
        public async Task GetCommentsAsync_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            // Arrange
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Ticket?)null);

            // Act
            var result = await _commentsService.GetCommentsAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetCommentsAsync_ShouldReturnNotFound_WhenAgentNotAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = Guid.NewGuid() });

            // Act
            var result = await _commentsService.GetCommentsAsync(ticketId, agentId, "SupportAgent");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetCommentsAsync_ShouldReturnNotFound_WhenCustomerDoesNotOwnTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, CustomerId = Guid.NewGuid() });

            // Act
            var result = await _commentsService.GetCommentsAsync(ticketId, customerId, "Customer");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetCommentsAsync_ShouldReturnComments_WhenAgentIsAssigned()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, AssignedAgentId = agentId });
            _ticketCommentRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TicketComment, bool>>>(), It.IsAny<Expression<Func<TicketComment, object>>>()))
                .ReturnsAsync(new List<TicketComment>());

            // Act
            var result = await _commentsService.GetCommentsAsync(ticketId, agentId, "SupportAgent");

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetCommentsAsync_ShouldReturnComments_WhenCustomerOwnsTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, CustomerId = customerId });
            _ticketCommentRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TicketComment, bool>>>(), It.IsAny<Expression<Func<TicketComment, object>>>()))
                .ReturnsAsync(new List<TicketComment>());

            // Act
            var result = await _commentsService.GetCommentsAsync(ticketId, customerId, "Customer");

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetCommentsAsync_ShouldReturnEmptyList_WhenNoCommentsExist()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId });
            _ticketCommentRepositoryMock.Setup(r => r.GetFilteredAsync(It.IsAny<Expression<Func<TicketComment, bool>>>(), It.IsAny<Expression<Func<TicketComment, object>>>()))
                .ReturnsAsync(new List<TicketComment>());

            // Act
            var result = await _commentsService.GetCommentsAsync(ticketId, Guid.NewGuid(), "Admin");

            // Assert
            result.Success.Should().BeTrue();
            var dataResult = (ApiResponseWithData<List<CommentResponse>>)result;
            dataResult.Data.Should().BeEmpty();
        }

        #endregion

        #region AddCommentAsync

        [Fact]
        public async Task AddCommentAsync_ShouldAddComment_WhenRequestIsValid()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateCommentRequest { Content = "Test Comment" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId });

            // Act
            var result = await _commentsService.AddCommentAsync(ticketId, request, userId, "Admin");

            // Assert
            result.Success.Should().BeTrue();
            _ticketCommentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TicketComment>()), Times.Once);
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.CommentAdded)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnNotFound_WhenTicketNotAccessible()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateCommentRequest { Content = "Test Comment" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId, CustomerId = Guid.NewGuid() }); // Different customer

            // Act
            var result = await _commentsService.AddCommentAsync(ticketId, request, userId, "Customer");

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            _ticketCommentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TicketComment>()), Times.Never);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldCreateCommentActivity()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var request = new CreateCommentRequest { Content = "Test Comment" };
            _ticketsRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(new Ticket { Id = ticketId });

            // Act
            await _commentsService.AddCommentAsync(ticketId, request, Guid.NewGuid(), "Admin");

            // Assert
            _ticketActivityRepositoryMock.Verify(r => r.AddAsync(It.Is<TicketActivity>(a => a.Type == ActivityType.CommentAdded)), Times.Once);
        }

        #endregion
    }
}
