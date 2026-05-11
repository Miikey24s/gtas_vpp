using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace gtas_vpp_be.Service.Helpers
{
    public sealed class TripleDesPasswordEncoder : IPasswordEncoder
    {
        private const string DefaultKey = "ttpsolutions";
        private readonly string _key;

        public TripleDesPasswordEncoder(IOptions<PasswordEncoderOptions> options)
        {
            _key = string.IsNullOrWhiteSpace(options.Value.Key) ? DefaultKey : options.Value.Key;
        }

        public string Encrypt(string plaintext)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(plaintext);

            using (MD5 hashmd5 = MD5.Create())
            {
                keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(_key));
            }

            using TripleDES tdes = TripleDES.Create();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public string Decrypt(string ciphertext)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(ciphertext);

            using (MD5 hashmd5 = MD5.Create())
            {
                keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(_key));
            }

            using TripleDES tdes = TripleDES.Create();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }
    }
}
