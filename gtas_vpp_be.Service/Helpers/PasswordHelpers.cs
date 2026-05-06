using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace gtas_vpp_be.Service.Helpers
{
    [StructLayout(LayoutKind.Auto)]
    public static class PasswordHelpers
    {
        private static readonly string _securitykey = "ttpsolutions";

        // ── BCrypt (modern) ────────────────────────────────────────

        public static string HashPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public static bool VerifyPassword(string plainPassword, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, hash);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsBcryptHash(string hash)
        {
            return !string.IsNullOrWhiteSpace(hash)
                   && (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"));
        }

        // ── TripleDES (legacy — for database compatibility) ────────

        [Obsolete("Use HashPassword/VerifyPassword instead. This is kept for legacy database migration.")]
        public static string Encrypt(string toEncrypt, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            if (useHashing)
            {
                MD5 hashmd5 = MD5.Create();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(_securitykey));
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(_securitykey);

            TripleDES tdes = TripleDES.Create();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            tdes.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        [Obsolete("Use HashPassword/VerifyPassword instead. This is kept for legacy database migration.")]
        public static string Decrypt(string cipherString, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

            if (useHashing)
            {
                MD5 hashmd5 = MD5.Create();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(_securitykey));
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(_securitykey);

            TripleDES tdes = TripleDES.Create();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            tdes.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
    }
}
