using System.Security.Cryptography;
using System.Text;

namespace ZkpSharp.Security
{
    public interface IProofProvider
    {
        string GenerateSalt();
        string GenerateHMAC(string input);
        bool SecureEqual(string a, string b);
    }

    public class ProofProvider : IProofProvider
    {
        private readonly byte[] _hmacKey;

        public ProofProvider(string hmacSecretKeyBase64)
        {
            _hmacKey = Convert.FromBase64String(hmacSecretKeyBase64);
        }

        public string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public string GenerateHMAC(string input)
        {
            using (var hmac = new HMACSHA256(_hmacKey))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashBytes);
            }
        }

        public bool SecureEqual(string a, string b)
        {
            if (a.Length != b.Length) return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}