using System.Security.Cryptography;
using System.Text;

namespace webMVC.Utilities
{
    public static class JwtKeyGenerator
    {
        /// <summary>
        /// Tạo JWT key ngẫu nhiên với độ dài 64 ký tự
        /// </summary>
        public static string GenerateSecureKey(int length = 64)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// Tạo JWT key từ passphrase
        /// </summary>
        public static string GenerateKeyFromPassphrase(string passphrase, int length = 64)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(passphrase));
                var key = Convert.ToBase64String(hash);
                
                // Đảm bảo độ dài tối thiểu
                while (key.Length < length)
                {
                    key += Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(key)));
                }
                
                return key.Substring(0, length);
            }
        }

        /// <summary>
        /// Tạo JWT key với format đặc biệt
        /// </summary>
        public static string GenerateFormattedKey()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomPart = GenerateSecureKey(32);
            return $"webmvc_jwt_{timestamp}_{randomPart}";
        }
    }
}
