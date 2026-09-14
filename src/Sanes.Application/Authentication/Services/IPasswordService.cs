namespace Sanes.Application.Authentication.Services;

public interface IPasswordService
{
    string HashPassword(string password);

    bool VerifyPassword(
        string passwordHash,
        string password);
}