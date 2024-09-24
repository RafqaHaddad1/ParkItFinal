using System.Security.Cryptography;
using System.Text;

namespace ParkIt.Models.Helper
{
    public class Password
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public Password(IConfiguration configuration)
        {
            string keyString = configuration["Password:Key"] ?? throw new ArgumentNullException(nameof(configuration), "Encryption key is missing.");
            string ivString = configuration["Password:IV"] ?? throw new ArgumentNullException(nameof(configuration), "Encryption IV is missing.");

            _key = Encoding.UTF8.GetBytes(keyString);
            _iv = Encoding.UTF8.GetBytes(ivString);

            if (_key.Length != 32 || _iv.Length != 16)
            {
                throw new ArgumentException("Invalid key or IV length.");
            }
        }

        public string HashPassword(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Flush();
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray()) + ":" + Guid.NewGuid().ToString();
                }
            }
        }

        public string UnHashPassword(string cipherText)
        {
            int delimiterIndex = cipherText.LastIndexOf(':');
            if (delimiterIndex == -1)
            {
                throw new ArgumentException("Invalid encrypted input encountered.");
            }

            string base64Cipher = cipherText.Substring(0, delimiterIndex);

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(Convert.FromBase64String(base64Cipher)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
