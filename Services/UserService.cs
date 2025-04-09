using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using SEM.Domain.Models;
using SEM.Domain.Interfaces;

namespace SEM.Services;

public class UserManager : IUserManager
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public UserManager(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<string> RegisterAsync(string email, string password)
        {
            if (await _userRepository.GetByEmailAsync(email) != null)
                return "User already exists";

            string hashedPassword = HashPassword(password);

            var newUser = new User
            {
                Email = email,
                PasswordHash = hashedPassword,
                FirstName = "User" // Имя по умолчанию
            };

            await _userRepository.AddUserAsync(newUser);
            return GenerateJwtToken(newUser);
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.PasswordHash != HashPassword(password))
                return null;

            return GenerateJwtToken(user);
        }

        public async Task<bool> LogoutAsync()
        {
            // JWT-токены являются статeless, поэтому logout обычно реализуется на клиенте
            return await Task.FromResult(true);
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
                expires: now.AddHours(1),
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
    }