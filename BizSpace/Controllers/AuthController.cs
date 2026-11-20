using System.Security.Claims;
using BizSpace.Dtos;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<RegistrationResponse>.Ok(result, "Check your email for a confirmation code."));
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyEmail(VerifyEmailRequest request) =>
        Ok(ApiResponse<AuthResponse>.Ok(await authService.VerifyEmailAsync(request), "Email confirmed."));

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<ActionResult<ApiResponse<object>>> ResendVerification(EmailOnlyRequest request)
    {
        await authService.ResendVerificationAsync(request);
        return Ok(ApiResponse<object>.Ok(null,
            "If the account needs confirmation, a code was sent. Wait 60 seconds before requesting another code."));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(EmailOnlyRequest request)
    {
        await authService.RequestPasswordResetAsync(request);
        return Ok(ApiResponse<object>.Ok(null,
            "If that account can reset its password, a code will be sent. Wait 60 seconds before requesting another code."));
    }

    [AllowAnonymous]
    [HttpPost("resend-password-reset")]
    public async Task<ActionResult<ApiResponse<object>>> ResendPasswordReset(EmailOnlyRequest request)
    {
        await authService.RequestPasswordResetAsync(request);
        return Ok(ApiResponse<object>.Ok(null,
            "If that account can reset its password, a code will be sent. Wait 60 seconds before requesting another code."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(null, "Password reset successfully."));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User identity was not found."));

        await authService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request) =>
        Ok(ApiResponse<AuthResponse>.Ok(await authService.LoginAsync(request), "Login completed."));
}
