using System.Security.Cryptography;
using System.Text;
using ZkpSharp.Interfaces;

namespace ZkpSharp.Security
{
    public class ProofProvider : IProofProvider
    {
        private readonly byte[] _hmacKey;

        public ProofProvider(string hmacSecretKeyBase64)
        {
            if (string.IsNullOrEmpty(hmacSecretKeyBase64))
            {
                throw new ArgumentException("HMAC secret key cannot be null or empty.", nameof(hmacSecretKeyBase64));
            }

            try
            {
                _hmacKey = Convert.FromBase64String(hmacSecretKeyBase64);
                if (_hmacKey.Length != 32)
                {
                    throw new ArgumentException("HMAC secret key must be 32 bytes (256 bits) when decoded.", nameof(hmacSecretKeyBase64));
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid Base64 format for HMAC secret key.", nameof(hmacSecretKeyBase64), ex);
            }
        }

        public string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
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
            if (a == null || b == null)
            {
                return a == b; // Both null -> true, one null -> false
            }

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