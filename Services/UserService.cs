using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using Domain;
using Domain.DTO;
using SEM.Domain.Models;
using SEM.Domain.Interfaces;

namespace SEM.Services;

public class UserManager : IUserManager
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IConfiguration _configuration;

        public UserManager(IUserRepository userRepository, IConfiguration configuration, IUserProfileRepository userProfileRepository)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _userProfileRepository = userProfileRepository;
        }

        public async Task<ServiceResult<AuthResponse>> RegisterAsync(string email, string password)
        {
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
                return ServiceResult<AuthResponse>.Fail("User already exists");

            string hashedPassword = HashPassword(password);

            var newUser = new User
            {
                Email = email,
                PasswordHash = hashedPassword,
                FirstName = "User" // Имя по умолчанию
            };

            var auth = await CreateAuthForUserAsync(newUser);
            await _userRepository.AddUserAsync(newUser);
            return ServiceResult<AuthResponse>.Ok(auth);
        }

        public async Task<ServiceResult<AuthResponse>> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.PasswordHash != HashPassword(password))
                return ServiceResult<AuthResponse>.Fail("Invalid email or password");

            var auth = await CreateAuthForUserAsync(user);
            return ServiceResult<AuthResponse>.Ok(auth);
        }

        public async Task<ServiceResult<bool>> LogoutAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Ok(true);
        }
        
        public async Task<ServiceResult<AuthResponse>> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return ServiceResult<AuthResponse>.Fail("Refresh token is required");

            var hash = ComputeSha256Hash(refreshToken);
            var dbToken = await _userRepository.GetByHashAsync(hash);
            if (dbToken == null || !dbToken.IsActive)
                return ServiceResult<AuthResponse>.Fail("Invalid refresh token");

            // Проверяем существование пользователя
            var user = await _userProfileRepository.GetByIdAsync(dbToken.UserId);
            if (user == null)
            {
                // на всякий — помечаем токен как отозванный
                await _userRepository.RevokeAsync(dbToken);
                return ServiceResult<AuthResponse>.Fail("User not found");
            }

            // Инвалидируем старый токен
            await _userRepository.RevokeAsync(dbToken);

            // Создаём новые токены
            var newPlain = GenerateRefreshToken();
            var newHash = ComputeSha256Hash(newPlain);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var newDbToken = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newHash,
                ExpiresAt = expiresAt,
            };

            await _userRepository.AddAsync(newDbToken);

            var access = GenerateJwtToken(user);

            var response = new AuthResponse(access, newPlain, expiresAt);
            return ServiceResult<AuthResponse>.Ok(response);
        }
        
        private async Task<AuthResponse> CreateAuthForUserAsync(User user)
        {
            var access = GenerateJwtToken(user);

            var plainRefresh = GenerateRefreshToken();
            var hash = ComputeSha256Hash(plainRefresh);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var dbToken = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = hash,
                ExpiresAt = expiresAt,
            };

            await _userRepository.AddAsync(dbToken);

            return new AuthResponse(access, plainRefresh, expiresAt);
        }


        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return false;

            string token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            await _userRepository.UpdateUserAsync(user);

            return SendResetEmail(user.Email, token);
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _userRepository.GetByResetTokenAsync(token);
            if (user == null)
                return false;

            user.PasswordHash = HashPassword(newPassword);
            user.ResetToken = null;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: now.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool SendResetEmail(string email, string token)
        {
            try
            {
                var smtpClient = new SmtpClient(_configuration["Smtp:Host"])
                {
                    Port = int.Parse(_configuration["Smtp:Port"]),
                    Credentials = new NetworkCredential(
                        _configuration["Smtp:User"],
                        _configuration["Smtp:Password"]
                    ),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["Smtp:User"]),
                    Subject = "Сброс пароля",
                    Body = $"Для сброса пароля перейдите по ссылке: {_configuration["App:BaseUrl"]}/reset-password?token={token}",
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);
                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        
        public static string GenerateRefreshToken(int size = 64)
        {
            var data = new byte[size];
            RandomNumberGenerator.Fill(data);
            return Base64UrlEncode(data);
        }

        public static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string ComputeSha256Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Base64UrlEncode(hash);
        }
    }