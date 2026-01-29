using HealthyCron.Data.Interfaces;
using HealthyCron.Logic.Interfaces;
using HealthyCron.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HealthyCron.Logic.Service
{
    public class AccessKeyService : IAccessKeyService
    {
        private readonly IProjectAccessKeyRepository _accessKeyRepository;
        private const string KeyPrefix = "hc_live_";

        public AccessKeyService(IProjectAccessKeyRepository accessKeyRepository)
        {
            _accessKeyRepository = accessKeyRepository;
        }

        public async Task<(string FullKey, ProjectAccessKey KeyModel)> CreateKeyAsync(Guid projectId, ApiKeyType type)
        {
            // Revoke any existing active key of this type
            var existingKey = await _accessKeyRepository.GetActiveKeyByTypeAsync(projectId, type);
            if (existingKey != null)
            {
                await _accessKeyRepository.RevokeKeyAsync(existingKey.Id);
            }

            var typeCode = type switch
            {
                ApiKeyType.Ping => "p",
                ApiKeyType.FullAccess => "f",
                ApiKeyType.ReadAccess => "r",
                _ => "x"
            };

            var randomPart = GenerateRandomString(24);
            var prefix = $"hc_{typeCode}_";
            var fullKey = $"{prefix}{randomPart}";
            var keyHash = HashKey(fullKey);

            var keyModel = new ProjectAccessKey
            {
                ProjectId = projectId,
                KeyType = type,
                KeyPrefix = fullKey.Substring(0, 8),
                KeyHash = keyHash,
                CreatedAt = DateTime.UtcNow
            };

            var id = await _accessKeyRepository.CreateKeyAsync(keyModel);
            keyModel.Id = id;

            return (fullKey, keyModel);
        }

        public async Task<IEnumerable<ProjectAccessKey>> GetKeysByProjectIdAsync(Guid projectId)
        {
            return await _accessKeyRepository.GetKeysByProjectIdAsync(projectId);
        }

        public async Task<bool> RevokeKeyAsync(Guid keyId)
        {
            return await _accessKeyRepository.RevokeKeyAsync(keyId);
        }

        public async Task<ProjectAccessKey?> ValidateKeyAsync(string fullKey)
        {
            var hash = HashKey(fullKey);
            var key = await _accessKeyRepository.GetKeyByHashAsync(hash);

            if (key != null && key.RevokedAt == null)
            {
                return key;
            }

            return null;
        }

        public string HashKey(string fullKey)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(fullKey);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var buffer = new char[length];
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[length];
            rng.GetBytes(randomBytes);

            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[randomBytes[i] % chars.Length];
            }

            return new string(buffer);
        }
    }
}
