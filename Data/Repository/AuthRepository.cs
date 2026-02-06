using Dapper;
using HealthyCron.Data.Interfaces;
using HealthyCron.Data.Repository;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Data.Repository
{
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        public AuthRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            const string sql = "SELECT * FROM users WHERE email = @Email";
            return await QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            const string sql = "SELECT * FROM users WHERE id = @Id";
            return await QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<Guid> CreateUserAsync(string email, string? passwordHash = null)
        {
            var id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO users (id, email, password_hash) 
                VALUES (@Id, @Email, @PasswordHash) 
                RETURNING id";
            return await ExecuteScalarAsync<Guid>(sql, new { Id = id, Email = email, PasswordHash = passwordHash });
        }

        public async Task UpdateUserPasswordAsync(Guid userId, string passwordHash)
        {
            const string sql = "UPDATE users SET password_hash = @PasswordHash, updated_at = CURRENT_TIMESTAMP WHERE id = @Id";
            await ExecuteAsync(sql, new { Id = userId, PasswordHash = passwordHash });
        }

        public async Task CreateMagicTokenAsync(Guid userId, string tokenHash, DateTime expiresAt)
        {
            const string sql = @"
                INSERT INTO auth_magic_tokens (id, user_id, token_hash, expires_at) 
                VALUES (@Id, @UserId, @TokenHash, @ExpiresAt)";
            await ExecuteAsync(sql, new { Id = Guid.NewGuid(), UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt });
        }

        public async Task<MagicToken?> GetMagicTokenByHashAsync(string tokenHash)
        {
            const string sql = "SELECT * FROM auth_magic_tokens WHERE token_hash = @TokenHash";
            return await QueryFirstOrDefaultAsync<MagicToken>(sql, new { TokenHash = tokenHash });
        }

        public async Task MarkMagicTokenAsUsedAsync(Guid tokenId)
        {
            const string sql = "UPDATE auth_magic_tokens SET used_at = CURRENT_TIMESTAMP WHERE id = @Id";
            await ExecuteAsync(sql, new { Id = tokenId });
        }

        public async Task CreateSessionAsync(Guid userId, string sessionTokenHash, DateTime expiresAt)
        {
            const string sql = @"
                INSERT INTO user_sessions (id, user_id, session_token, expires_at) 
                VALUES (@Id, @UserId, @SessionTokenHash, @ExpiresAt)";
            await ExecuteAsync(sql, new { Id = Guid.NewGuid(), UserId = userId, SessionTokenHash = sessionTokenHash, ExpiresAt = expiresAt });
        }

        public async Task<UserSession?> GetSessionByHashAsync(string sessionTokenHash)
        {
            const string sql = "SELECT * FROM user_sessions WHERE session_token = @SessionTokenHash";
            return await QueryFirstOrDefaultAsync<UserSession>(sql, new { SessionTokenHash = sessionTokenHash });
        }

        public async Task DeleteSessionAsync(string sessionTokenHash)
        {
            const string sql = "DELETE FROM user_sessions WHERE session_token = @SessionTokenHash";
            await ExecuteAsync(sql, new { SessionTokenHash = sessionTokenHash });
        }
    }
}
