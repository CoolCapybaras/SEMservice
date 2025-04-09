using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IUserManager
{
    Task<string> RegisterAsync(string email, string password);
    Task<string> LoginAsync(string email, string password);
    Task<bool> LogoutAsync();
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}