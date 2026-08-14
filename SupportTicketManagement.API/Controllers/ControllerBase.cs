using Microsoft.AspNetCore.Mvc;

namespace SupportTicketManagement.API.Controllers
{
    /// <summary>
    /// Base controller for the Support Ticket Management API, providing common functionality for all controllers.
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CustomControllerBase : ControllerBase
    {
        /// <summary>
        /// Constructor for CustomControllerBase, initializing the unit of work.
        /// </summary>

        public CustomControllerBase()
        {
            // This constructor can be used to initialize common services or properties
            // that all derived controllers might need.
        }
    }
}
