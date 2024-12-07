using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

public class EncryptionSettingsModel
{
    public  string? Key { get; set; }
    public  string? IV { get; set; }
}

public interface IEncryptionService
{
    Task<string> Encrypt(string plainText);
    Task<string> Decrypt(string cipherText);
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IOptions<EncryptionSettingsModel> options)
    {
        var encryptionSettings = options.Value;

        if (string.IsNullOrWhiteSpace(encryptionSettings.Key) || encryptionSettings.Key.Length != 32)
            throw new ArgumentException("Key must be 32 characters long (256 bits).", nameof(encryptionSettings.Key));
        if (string.IsNullOrWhiteSpace(encryptionSettings.IV) || encryptionSettings.IV.Length != 16)
            throw new ArgumentException("IV must be 16 characters long (128 bits).", nameof(encryptionSettings.IV));

        _key = Encoding.UTF8.GetBytes(encryptionSettings.Key);
        _iv = Encoding.UTF8.GetBytes(encryptionSettings.IV);
    }

    public async Task<string> Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) throw new ArgumentNullException(nameof(plainText));

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            using (var sw = new StreamWriter(cs))
            {
                await sw.WriteAsync(plainText);
            }
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public async Task<string> Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText)) throw new ArgumentNullException(nameof(cipherText));

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return await sr.ReadToEndAsync();
    }
}
