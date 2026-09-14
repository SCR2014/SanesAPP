using Microsoft.AspNetCore.Identity;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Entities;

namespace Sanes.Infrastructure.Authentication.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(
                "Password is required.",
                nameof(password));

        return _passwordHasher.HashPassword(
            new AppUser(),
            password);
    }

    public bool VerifyPassword(
        string passwordHash,
        string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var result =
            _passwordHasher.VerifyHashedPassword(
                new AppUser(),
                passwordHash,
                password);

        return result is
            PasswordVerificationResult.Success or
            PasswordVerificationResult.SuccessRehashNeeded;
    }
}