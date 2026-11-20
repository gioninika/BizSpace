using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class UserService(IGenericRepository<UserAccount> users, IAuthService authService) : IUserService
{
    private readonly PasswordHasher<UserAccount> _passwordHasher = new();

    public async Task<List<UserDto>> GetAllAsync() =>
        (await users.Query.AsNoTracking().OrderBy(x => x.Id).ToListAsync()).Select(AuthService.ToDto).ToList();

    public async Task<UserDto> CreateStoreManagerAsync(CreateStoreManagerRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.Query.AnyAsync(x => x.Email == email))
            throw new BusinessRuleException("An account with this email already exists.");

        var manager = new UserAccount
        {
            DisplayName = request.DisplayName.Trim(),
            Email = email,
            Role = UserRoles.StoreManager,
            EmailConfirmed = false
        };
        manager.PasswordHash = _passwordHasher.HashPassword(manager, request.Password);
        await users.AddAsync(manager);
        await users.SaveAsync();
        await authService.SendVerificationEmailAsync(manager.Id);
        return AuthService.ToDto(manager);
    }

    public async Task<UserDto> GetByIdAsync(int id, int callerId, bool isAdmin)
    {
        if (!isAdmin && id != callerId)
            throw new ForbiddenException("You can view only your own profile.");
        var user = await users.Query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("User was not found.");
        return AuthService.ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(int id, int callerId, bool isAdmin, UpdateUserRequest request)
    {
        if (!isAdmin && id != callerId)
            throw new ForbiddenException("You can update only your own profile.");
        var user = await users.Query.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("User was not found.");
        var emailChanged = false;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (!string.Equals(user.Email, email, StringComparison.Ordinal))
            {
                if (await users.Query.AnyAsync(x => x.Id != id && x.Email == email))
                    throw new BusinessRuleException("An account with this email already exists.");
                user.Email = email;
                user.EmailConfirmed = false;
                user.EmailCodeHash = null;
                user.EmailCodePurpose = null;
                user.EmailCodeExpiresAtUtc = null;
                user.EmailCodeSentAtUtc = null;
                user.EmailCodeFailedAttempts = 0;
                emailChanged = true;
            }
        }
        user.DisplayName = request.DisplayName.Trim();
        await users.SaveAsync();
        if (emailChanged)
            await authService.SendVerificationEmailAsync(user.Id);
        return AuthService.ToDto(user);
    }
}
