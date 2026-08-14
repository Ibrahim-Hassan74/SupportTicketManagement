using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.ServiceContracts;
using System.Security.Claims;

namespace SupportTicketManagement.API.Controllers
{
    /// <summary>
    /// Controller responsible for retrieving system-wide dashboard metrics.
    /// Provides endpoints for overall statistics, agent workload, and ticket trends.
    /// Restricted to administrators only.
    /// </summary>
    [ApiVersion(1.0)]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class DashboardController : CustomControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Retrieves system-wide ticket statistics.
        /// </summary>
        /// <returns>Counts for tickets in various states and average resolution time.</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStats()
        {
            var response = await _dashboardService.GetStatsAsync(GetUserRole());
            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<DashboardStatsResponse>;
            return Ok(responseData?.Data);
        }

        /// <summary>
        /// Retrieves the current workload for all agents.
        /// </summary>
        /// <returns>A list of agents with their open/in-progress ticket counts and total logged time.</returns>
        [HttpGet("agent-workload")]
        [ProducesResponseType(typeof(List<AgentWorkloadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAgentWorkload()
        {
            var response = await _dashboardService.GetAgentWorkloadAsync(GetUserRole());
            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<IEnumerable<AgentWorkloadResponse>>;
            return Ok(responseData?.Data);
        }

        /// <summary>
        /// Retrieves ticket creation and closure trends over a specified number of days.
        /// </summary>
        /// <param name="days">The number of days to look back (default is 30).</param>
        /// <returns>A chronological list of daily ticket trends.</returns>
        [HttpGet("ticket-trends")]
        [ProducesResponseType(typeof(List<TicketTrendResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTicketTrends([FromQuery] int days = 30)
        {
            var response = await _dashboardService.GetTicketTrendsAsync(days, GetUserRole());
            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<IEnumerable<TicketTrendResponse>>;
            return Ok(responseData?.Data);
        }
    }
}