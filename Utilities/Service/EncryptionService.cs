using HealthyCron.Models.Configuration;
using HealthyCron.Utilities.Interface;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace HealthyCron.Utilities.Service
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService(EncryptionSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Key))
            {
                throw new ArgumentException("Encryption key is missing in configuration (Encryption:Key)");
            }

            _key = Convert.FromBase64String(settings.Key);

            if (_key.Length != 32)
            {
                throw new ArgumentException($"Encryption key must be 32 bytes (256 bits). Current length: {_key.Length} bytes. Ensure you provide a valid 32-byte Base64 string.");
            }
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                throw new ArgumentNullException(nameof(plaintext));

            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

            RandomNumberGenerator.Fill(nonce);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine nonce + tag + ciphertext
            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext))
                throw new ArgumentNullException(nameof(ciphertext));

            var combined = Convert.FromBase64String(ciphertext);

            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var encryptedData = new byte[combined.Length - nonceSize - tagSize];

            Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(combined, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(combined, nonceSize + tagSize, encryptedData, 0, encryptedData.Length);

            using var aes = new AesGcm(_key, tagSize);
            var decryptedData = new byte[encryptedData.Length];

            aes.Decrypt(nonce, encryptedData, tag, decryptedData);

            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}
