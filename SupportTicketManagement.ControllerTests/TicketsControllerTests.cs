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
    public class TicketsControllerTests
    {
        private readonly Mock<ITicketsService> _ticketsServiceMock;
        private readonly TicketsController _ticketsController;

        public TicketsControllerTests()
        {
            _ticketsServiceMock = new Mock<ITicketsService>();
            _ticketsController = new TicketsController(_ticketsServiceMock.Object);
        }

        private void SetUserRoleAndId(Guid userId, UserRole role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            }, "mock"));

            _ticketsController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetTicketById_ShouldReturnOk_WhenServiceReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Admin);

            var ticketId = Guid.NewGuid();
            var fakeResponse = ApiResponseFactory.Success("Success", new TicketResponse { Id = ticketId });

            _ticketsServiceMock.Setup(s => s.GetTicketByIdForAdminAsync(ticketId))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _ticketsController.GetTicketById(ticketId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var responseData = okResult.Value.Should().BeOfType<TicketResponse>().Subject;
            responseData.Id.Should().Be(ticketId);
        }

        [Fact]
        public async Task GetTicketById_ShouldReturnNotFound_WhenServiceReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Customer);

            var ticketId = Guid.NewGuid();
            var fakeResponse = ApiResponseFactory.NotFound("Ticket not found");

            _ticketsServiceMock.Setup(s => s.GetTicketByIdForCustomerAsync(ticketId, userId))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _ticketsController.GetTicketById(ticketId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            var apiResponse = notFoundResult.Value.Should().BeOfType<ApiResponse>().Subject;
            apiResponse.Message.Should().Be("Ticket not found");
        }

        [Fact]
        public async Task CreateTicket_ShouldReturnOk_WhenServiceReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Customer);

            var request = new CreateTicketRequest { Title = "Test", Description = "Desc" };
            var fakeResponse = ApiResponseFactory.Success("Success", new TicketResponse { Title = "Test" });

            _ticketsServiceMock.Setup(s => s.CreateTicketAsync(request, userId))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _ticketsController.CreateTicket(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var responseData = okResult.Value.Should().BeOfType<TicketResponse>().Subject;
            responseData.Title.Should().Be("Test");
        }

        [Fact]
        public async Task UpdateTicket_ShouldReturnForbidden_WhenServiceReturnsForbidden()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserRoleAndId(userId, UserRole.Admin);

            var ticketId = Guid.NewGuid();
            var request = new UpdateTicketRequest { Title = "New Title" };
            var fakeResponse = ApiResponseFactory.Forbidden("Access Denied");

            _ticketsServiceMock.Setup(s => s.UpdateTicketAsync(ticketId, request, userId))
                .ReturnsAsync(fakeResponse);

            // Act
            var result = await _ticketsController.UpdateTicket(ticketId, request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(403);
            var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse>().Subject;
            apiResponse.Message.Should().Be("Access Denied");
        }
    }
}
