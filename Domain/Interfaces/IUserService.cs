using Domain;
using Domain.DTO;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IUserManager
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(string email, string password);
    Task<ServiceResult<AuthResponse>> LoginAsync(string email, string password);
    Task<ServiceResult<AuthResponse>> RefreshAsync(string refreshToken);
    Task<ServiceResult<bool>> LogoutAsync();
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}