namespace HealthyCron.Utilities.Interface
{
    public interface IEncryptionService
    {
        string Encrypt(string plaintext);
        string Decrypt(string ciphertext);
    }
}
