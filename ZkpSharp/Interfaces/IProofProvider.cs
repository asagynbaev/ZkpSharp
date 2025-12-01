namespace ZkpSharp.Interfaces
{
    /// <summary>
    /// Provides cryptographic operations for generating and verifying zero-knowledge proofs.
    /// </summary>
    public interface IProofProvider
    {
        /// <summary>
        /// Generates a cryptographically secure random salt.
        /// </summary>
        /// <returns>A base64-encoded salt string.</returns>
        string GenerateSalt();

        /// <summary>
        /// Generates an HMAC-SHA256 hash of the input string.
        /// </summary>
        /// <param name="input">The input string to hash.</param>
        /// <returns>A base64-encoded HMAC hash.</returns>
        string GenerateHMAC(string input);

        /// <summary>
        /// Performs a constant-time comparison of two strings to prevent timing attacks.
        /// </summary>
        /// <param name="a">The first string to compare.</param>
        /// <param name="b">The second string to compare.</param>
        /// <returns>True if the strings are equal, false otherwise.</returns>
        bool SecureEqual(string a, string b);
    }
}