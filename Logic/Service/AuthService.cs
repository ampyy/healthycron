using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using HealthyCron.Data.Interfaces;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;

namespace HealthyCron.Logic.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private const int MagicTokenExpiryMinutes = 15;
        private const int SessionExpiryDays = 30;

        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public async Task<string> RequestMagicLinkAsync(string email)
        {
            var user = await _authRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                var userId = await _authRepository.CreateUserAsync(email);
                user = await _authRepository.GetUserByIdAsync(userId);
            }

            var token = GenerateSecureToken();
            var tokenHash = HashToken(token);
            var expiresAt = DateTime.UtcNow.AddMinutes(MagicTokenExpiryMinutes);

            await _authRepository.CreateMagicTokenAsync(user!.Id, tokenHash, expiresAt);

            return token; // Send this raw token in the magic link
        }

        public async Task<string?> VerifyMagicTokenAsync(string token)
        {
            var tokenHash = HashToken(token);
            var magicToken = await _authRepository.GetMagicTokenByHashAsync(tokenHash);

            if (magicToken == null || magicToken.UsedAt != null || magicToken.ExpiresAt < DateTime.UtcNow)
            {
                return null;
            }

            await _authRepository.MarkMagicTokenAsUsedAsync(magicToken.Id);

            return await CreateSessionInternalAsync(magicToken.UserId);
        }

        public async Task<bool> RegisterPasswordAsync(string email, string password)
        {
            var user = await _authRepository.GetUserByEmailAsync(email);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            if (user == null)
            {
                await _authRepository.CreateUserAsync(email, passwordHash);
                return true;
            }

            if (user.PasswordHash != null) return false; // Already has password

            await _authRepository.UpdateUserPasswordAsync(user.Id, passwordHash);
            return true;
        }

        public async Task<string?> LoginPasswordAsync(string email, string password)
        {
            var user = await _authRepository.GetUserByEmailAsync(email);
            if (user == null || user.PasswordHash == null) return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

            return await CreateSessionInternalAsync(user.Id);
        }

        public async Task LogoutAsync(string sessionToken)
        {
            var sessionHash = HashToken(sessionToken);
            await _authRepository.DeleteSessionAsync(sessionHash);
        }

        public async Task<User?> GetUserFromSessionAsync(string sessionToken)
        {
            var sessionHash = HashToken(sessionToken);
            var session = await _authRepository.GetSessionByHashAsync(sessionHash);

            if (session == null || session.ExpiresAt < DateTime.UtcNow) return null;

            return await _authRepository.GetUserByIdAsync(session.UserId);
        }

        private async Task<string> CreateSessionInternalAsync(Guid userId)
        {
            var sessionToken = GenerateSecureToken();
            var sessionHash = HashToken(sessionToken);
            var expiresAt = DateTime.UtcNow.AddDays(SessionExpiryDays);

            await _authRepository.CreateSessionAsync(userId, sessionHash, expiresAt);

            return sessionToken;
        }

        private string GenerateSecureToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
