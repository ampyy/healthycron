using Dapper;
using HealthyCron.Data.Interfaces;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthyCron.Data.Repository
{
    public class AccessKeyRepository : BaseRepository, IProjectAccessKeyRepository
    {
        public AccessKeyRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<ProjectAccessKey?> GetKeyByIdAsync(Guid id)
        {
            const string sql = "SELECT * FROM project_api_keys WHERE id = @Id";
            return await QueryFirstOrDefaultAsync<ProjectAccessKey>(sql, new { Id = id });
        }

        public async Task<IEnumerable<ProjectAccessKey>> GetKeysByProjectIdAsync(Guid projectId)
        {
            const string sql = "SELECT * FROM project_api_keys WHERE project_id = @ProjectId ORDER BY created_at DESC";
            return await QueryAsync<ProjectAccessKey>(sql, new { ProjectId = projectId });
        }

        public async Task<Guid> CreateKeyAsync(ProjectAccessKey key)
        {
            if (key.Id == Guid.Empty)
            {
                key.Id = Guid.NewGuid();
            }

            const string sql = @"
                INSERT INTO project_api_keys (id, project_id, key_type, key_hash, key_prefix, plaintext_key, created_at) 
                VALUES (@Id, @ProjectId, @KeyType, @KeyHash, @KeyPrefix, @PlaintextKey, @CreatedAt) 
                RETURNING id";
            return await ExecuteScalarAsync<Guid>(sql, key);
        }

        public async Task<bool> RevokeKeyAsync(Guid id)
        {
            const string sql = "UPDATE project_api_keys SET revoked_at = NOW() WHERE id = @Id";
            return await ExecuteAsync(sql, new { Id = id }) > 0;
        }

        public async Task<ProjectAccessKey?> GetKeyByHashAsync(string hash)
        {
            const string sql = "SELECT * FROM project_api_keys WHERE key_hash = @KeyHash AND revoked_at IS NULL";
            return await QueryFirstOrDefaultAsync<ProjectAccessKey>(sql, new { KeyHash = hash });
        }

        public async Task<ProjectAccessKey?> GetActiveKeyByTypeAsync(Guid projectId, ApiKeyType type)
        {
            const string sql = "SELECT * FROM project_api_keys WHERE project_id = @ProjectId AND key_type = @KeyType AND revoked_at IS NULL";
            return await QueryFirstOrDefaultAsync<ProjectAccessKey>(sql, new { ProjectId = projectId, KeyType = type });
        }
    }
}
