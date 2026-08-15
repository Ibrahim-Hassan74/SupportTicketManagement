using SupportTicketManagement.Core.DTO;

namespace SupportTicketManagement.Core.ServiceContracts
{
    /// <summary>
    /// Provides authentication-related operations such as user registration, login, 
    /// email confirmation, and password reset.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Registers a new user with the provided registration data.
        /// </summary>
        /// <param name="registerDTO">
        /// Data required for registering a new user.
        /// </param>
        /// <returns>
        /// Returns an <see cref="ApiResponse"/> indicating whether the 
        /// registration was successful (StatusCode 200) or failed with details 
        /// (e.g., StatusCode 400 or 409 if username/email is already in use).
        /// </returns>
        Task<ApiResponse> RegisterAsync(RegisterDTO registerDTO);

        /// <summary>
        /// Authenticates a user with the provided login credentials.
        /// </summary>
        /// <param name="loginDTO">
        /// Login data including email and password.
        /// </param>
        /// <returns>
        /// Returns an <see cref="ApiResponse"/> indicating whether the 
        /// login was successful (StatusCode 200) or failed due to invalid credentials, 
        /// unconfirmed email, or locked account.
        /// </returns>
        Task<ApiResponse> LoginAsync(LoginDTO loginDTO);
        /// <summary>
        /// Logs out the user by clearing their refresh token and signing them out of the application.
        /// </summary>
        /// <param name="email">The email of the user to log out. If null, only signs out without clearing refresh token.</param>
        /// <returns>
        /// An <see cref="ApiResponse"/> indicating whether the logout operation was successful.
        /// </returns>
        Task<ApiResponse> LogoutAsync(string? email);
        /// <summary>
        /// Generates a new JWT (and refresh token) using a valid refresh token.
        /// </summary>
        /// <param name="model">The current (expired) access token and the refresh token.</param>
        /// <returns>
        /// Returns an <see cref="ApiResponse"/> with a new access token on success,
        /// or a failure response when the refresh token is invalid or expired.
        /// </returns>
        Task<ApiResponse> RefreshTokenAsync(TokenModel model);
        Task<ApiResponse> GetUserByIdAsync(string userId);
    }
}
