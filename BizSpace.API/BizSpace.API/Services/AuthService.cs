using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class AuthService(
    IGenericRepository<UserAccount> users,
    IGenericRepository<Cart> carts,
    IJwtService jwtService,
    IEmailSender emailSender,
    IConfiguration configuration) : IAuthService
{
    private const int MaximumFailedAttempts = 5;
    private const int MaximumCodeAttempts = 5;
    private readonly PasswordHasher<UserAccount> _passwordHasher = new();
    private readonly byte[] _codeKey = ReadCodeKey(configuration);

    public async Task<RegistrationResponse> RegisterAsync(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (await users.Query.AnyAsync(x => x.Email == email))
            throw new BusinessRuleException("An account with this email already exists.");

        var user = new UserAccount
        {
            DisplayName = request.DisplayName.Trim(),
            Email = email,
            Role = UserRoles.Customer,
            EmailConfirmed = false
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        await users.AddAsync(user);
        await users.SaveAsync();
        await carts.AddAsync(new Cart { UserId = user.Id });
        await carts.SaveAsync();
        await SendCodeAsync(user, AuthCodePurposes.ConfirmEmail);

        return new RegistrationResponse { Email = email };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.Query.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        if (user.LockedOutUntilUtc > DateTime.UtcNow)
            throw new UnauthorizedException("Too many failed attempts. Try again later.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaximumFailedAttempts)
            {
                user.FailedLoginAttempts = 0;
                user.LockedOutUntilUtc = DateTime.UtcNow.AddMinutes(15);
            }
            await users.SaveAsync();
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.FailedLoginAttempts = 0;
        user.LockedOutUntilUtc = null;
        await users.SaveAsync();
        if (!user.EmailConfirmed)
            throw new UnauthorizedException("Confirm your email before logging in.");

        return jwtService.CreateToken(user);
    }

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await users.Query.FirstOrDefaultAsync(x => x.Email == NormalizeEmail(request.Email))
            ?? throw new UnauthorizedException("Invalid or expired code.");

        await ValidateCodeAsync(user, AuthCodePurposes.ConfirmEmail, request.Code);
        user.EmailConfirmed = true;
        ClearCode(user);
        await users.SaveAsync();
        return jwtService.CreateToken(user);
    }

    public async Task ResendVerificationAsync(EmailOnlyRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.Query.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null || user.EmailConfirmed)
            return;

        await SendCodeAsync(user, AuthCodePurposes.ConfirmEmail);
    }

    public async Task RequestPasswordResetAsync(EmailOnlyRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.Query.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
            return;

        await SendCodeAsync(user, AuthCodePurposes.ResetPassword);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await users.Query.FirstOrDefaultAsync(x =>
            x.Email == NormalizeEmail(request.Email))
            ?? throw new UnauthorizedException("Invalid or expired code.");

        await ValidateCodeAsync(user, AuthCodePurposes.ResetPassword, request.Code);
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        // A successful password-reset code proves access to the account's email inbox.
        user.EmailConfirmed = true;
        user.FailedLoginAttempts = 0;
        user.LockedOutUntilUtc = null;
        ClearCode(user);
        await users.SaveAsync();
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await users.Query.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new UnauthorizedException("Account was not found.");
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        ClearCode(user);
        await users.SaveAsync();
    }

    public async Task SendVerificationEmailAsync(int userId)
    {
        var user = await users.Query.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new KeyNotFoundException("Account was not found.");
        if (user.EmailConfirmed)
            return;

        await SendCodeAsync(user, AuthCodePurposes.ConfirmEmail);
    }

    internal static UserDto ToDto(UserAccount user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        Email = user.Email,
        Role = user.Role,
        CreatedAtUtc = user.CreatedAtUtc
    };

    private async Task SendCodeAsync(UserAccount user, string purpose)
    {
        var now = DateTime.UtcNow;
        if (user.EmailCodePurpose == purpose &&
            user.EmailCodeSentAtUtc is DateTime lastSent &&
            lastSent > now.AddMinutes(-1))
        {
            return;
        }

        var previousCodeHash = user.EmailCodeHash;
        var previousPurpose = user.EmailCodePurpose;
        var previousExpiresAt = user.EmailCodeExpiresAtUtc;
        var previousSentAt = user.EmailCodeSentAtUtc;
        var previousFailedAttempts = user.EmailCodeFailedAttempts;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
        user.EmailCodeHash = HashCode(code);
        user.EmailCodePurpose = purpose;
        user.EmailCodeExpiresAtUtc = now.AddMinutes(10);
        user.EmailCodeSentAtUtc = now;
        user.EmailCodeFailedAttempts = 0;
        await users.SaveAsync();

        var isConfirmation = purpose == AuthCodePurposes.ConfirmEmail;
        var subject = isConfirmation ? "Confirm your BizSpace email" : "Reset your BizSpace password";
        var instruction = isConfirmation
            ? "Use this code to confirm your email address."
            : "Use this code to reset your password.";

        try
        {
            await emailSender.SendAsync(user.Email, subject,
                $"{instruction} Your code is {code}. It expires in 10 minutes.");
        }
        catch
        {
            user.EmailCodeHash = previousCodeHash;
            user.EmailCodePurpose = previousPurpose;
            user.EmailCodeExpiresAtUtc = previousExpiresAt;
            user.EmailCodeSentAtUtc = previousSentAt;
            user.EmailCodeFailedAttempts = previousFailedAttempts;
            await users.SaveAsync();
            throw;
        }
    }

    private async Task ValidateCodeAsync(UserAccount user, string purpose, string code)
    {
        var now = DateTime.UtcNow;
        if (user.EmailCodeHash is null ||
            user.EmailCodePurpose != purpose ||
            user.EmailCodeExpiresAtUtc is null ||
            user.EmailCodeExpiresAtUtc.Value <= now ||
            user.EmailCodeFailedAttempts >= MaximumCodeAttempts)
        {
            throw new UnauthorizedException("Invalid or expired code.");
        }

        var expected = Convert.FromBase64String(user.EmailCodeHash);
        var supplied = Convert.FromBase64String(HashCode(code));
        if (CryptographicOperations.FixedTimeEquals(expected, supplied))
            return;

        user.EmailCodeFailedAttempts++;
        if (user.EmailCodeFailedAttempts >= MaximumCodeAttempts)
            user.EmailCodeExpiresAtUtc = now;
        await users.SaveAsync();
        throw new UnauthorizedException("Invalid or expired code.");
    }

    private string HashCode(string code) => Convert.ToBase64String(
        HMACSHA256.HashData(_codeKey, Encoding.UTF8.GetBytes(code)));

    private static byte[] ReadCodeKey(IConfiguration configuration)
    {
        var key = configuration["Auth:CodeHmacKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Auth:CodeHmacKey must be configured with at least 32 bytes.");
        return Encoding.UTF8.GetBytes(key);
    }

    private static void ClearCode(UserAccount user)
    {
        user.EmailCodeHash = null;
        user.EmailCodePurpose = null;
        user.EmailCodeExpiresAtUtc = null;
        user.EmailCodeFailedAttempts = 0;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
