using System.Security.Cryptography;
using System.Text;
using ZkpSharp.Interfaces;
using ZkpSharp.Constants;

namespace ZkpSharp.Security
{
    /// <summary>
    /// Default implementation of <see cref="IProofProvider"/> using HMAC-SHA256 for cryptographic operations.
    /// </summary>
    public class ProofProvider : IProofProvider
    {
        private readonly byte[] _hmacKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProofProvider"/> class.
        /// </summary>
        /// <param name="hmacSecretKeyBase64">The HMAC secret key in Base64 format (must be 32 bytes when decoded).</param>
        /// <exception cref="ArgumentException">Thrown when the key is null, empty, or invalid.</exception>
        public ProofProvider(string hmacSecretKeyBase64)
        {
            if (string.IsNullOrEmpty(hmacSecretKeyBase64))
            {
                throw new ArgumentException("HMAC secret key cannot be null or empty.", nameof(hmacSecretKeyBase64));
            }

            try
            {
                _hmacKey = Convert.FromBase64String(hmacSecretKeyBase64);
                if (_hmacKey.Length != ZkpConstants.HmacKeySizeBytes)
                {
                    throw new ArgumentException($"HMAC secret key must be {ZkpConstants.HmacKeySizeBytes} bytes (256 bits) when decoded.", nameof(hmacSecretKeyBase64));
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid Base64 format for HMAC secret key.", nameof(hmacSecretKeyBase64), ex);
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random salt.
        /// </summary>
        /// <returns>A base64-encoded salt string.</returns>
        public string GenerateSalt()
        {
            byte[] saltBytes = new byte[ZkpConstants.SaltSizeBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Generates an HMAC-SHA256 hash of the input string.
        /// </summary>
        /// <param name="input">The input string to hash.</param>
        /// <returns>A base64-encoded HMAC hash.</returns>
        public string GenerateHMAC(string input)
        {
            using (var hmac = new HMACSHA256(_hmacKey))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Performs a constant-time comparison of two strings to prevent timing attacks.
        /// </summary>
        /// <param name="a">The first string to compare.</param>
        /// <param name="b">The second string to compare.</param>
        /// <returns>True if the strings are equal, false otherwise.</returns>
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