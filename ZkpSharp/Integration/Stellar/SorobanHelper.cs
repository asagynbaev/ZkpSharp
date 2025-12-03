using System.Text;

namespace ZkpSharp.Integration.Stellar
{
    /// <summary>
    /// Helper class for Soroban contract interactions.
    /// Provides utilities for encoding/decoding values to/from Soroban SCVal format.
    /// </summary>
    public static class SorobanHelper
    {
        /// <summary>
        /// Encodes a byte array as an SCVal (Soroban Contract Value).
        /// </summary>
        /// <param name="bytes">The bytes to encode.</param>
        /// <returns>Base64-encoded SCVal representation.</returns>
        public static string EncodeBytesAsScVal(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException("Bytes cannot be null or empty.", nameof(bytes));
            }

            // For SCVal bytes type, we prepend a type indicator and encode
            // Type 14 (0x0E) is SCValType::SCV_BYTES in Soroban
            var scVal = new byte[bytes.Length + 4];
            scVal[0] = 0x0E; // SCValType::SCV_BYTES
            scVal[1] = 0x00;
            scVal[2] = 0x00;
            scVal[3] = (byte)bytes.Length;
            Array.Copy(bytes, 0, scVal, 4, bytes.Length);

            return Convert.ToBase64String(scVal);
        }

        /// <summary>
        /// Decodes bytes from an SCVal (Soroban Contract Value).
        /// </summary>
        /// <param name="scVal">The Base64-encoded SCVal.</param>
        /// <returns>The decoded byte array.</returns>
        public static byte[] DecodeBytesFromScVal(string scVal)
        {
            if (string.IsNullOrEmpty(scVal))
            {
                throw new ArgumentException("SCVal cannot be null or empty.", nameof(scVal));
            }

            var data = Convert.FromBase64String(scVal);

            if (data.Length < 4)
            {
                throw new ArgumentException("Invalid SCVal format.", nameof(scVal));
            }

            // Extract the bytes after the header
            var length = data[3];
            var result = new byte[length];
            Array.Copy(data, 4, result, 0, length);

            return result;
        }

        /// <summary>
        /// Encodes a string as an SCVal (Soroban Contract Value).
        /// </summary>
        /// <param name="value">The string to encode.</param>
        /// <returns>Base64-encoded SCVal representation.</returns>
        public static string EncodeStringAsScVal(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("Value cannot be null.", nameof(value));
            }

            var bytes = Encoding.UTF8.GetBytes(value);

            // Type 14 (0x0E) is also used for strings in Soroban (as bytes)
            var scVal = new byte[bytes.Length + 4];
            scVal[0] = 0x0E; // SCValType::SCV_STRING (represented as bytes)
            scVal[1] = 0x00;
            scVal[2] = 0x00;
            scVal[3] = (byte)bytes.Length;
            Array.Copy(bytes, 0, scVal, 4, bytes.Length);

            return Convert.ToBase64String(scVal);
        }

        /// <summary>
        /// Decodes a string from an SCVal (Soroban Contract Value).
        /// </summary>
        /// <param name="scVal">The Base64-encoded SCVal.</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeStringFromScVal(string scVal)
        {
            var bytes = DecodeBytesFromScVal(scVal);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Encodes a boolean as an SCVal (Soroban Contract Value).
        /// </summary>
        /// <param name="value">The boolean value to encode.</param>
        /// <returns>Base64-encoded SCVal representation.</returns>
        public static string EncodeBoolAsScVal(bool value)
        {
            // Type 0 is SCV_BOOL in Soroban
            var scVal = new byte[4];
            scVal[0] = 0x00; // SCValType::SCV_BOOL
            scVal[1] = 0x00;
            scVal[2] = 0x00;
            scVal[3] = (byte)(value ? 1 : 0);

            return Convert.ToBase64String(scVal);
        }

        /// <summary>
        /// Decodes a boolean from an SCVal (Soroban Contract Value).
        /// </summary>
        /// <param name="scVal">The Base64-encoded SCVal.</param>
        /// <returns>The decoded boolean value.</returns>
        public static bool DecodeBoolFromScVal(string scVal)
        {
            if (string.IsNullOrEmpty(scVal))
            {
                throw new ArgumentException("SCVal cannot be null or empty.", nameof(scVal));
            }

            var data = Convert.FromBase64String(scVal);

            if (data.Length < 4)
            {
                throw new ArgumentException("Invalid SCVal format.", nameof(scVal));
            }

            return data[3] != 0;
        }

        /// <summary>
        /// Converts a ZKP proof (Base64-encoded HMAC-SHA256) to raw bytes.
        /// </summary>
        /// <param name="proof">The Base64-encoded proof.</param>
        /// <returns>The proof as raw bytes (32 bytes for HMAC-SHA256).</returns>
        /// <exception cref="ArgumentException">Thrown when the proof is invalid or has wrong length.</exception>
        public static byte[] ConvertProofToBytes(string proof)
        {
            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty.", nameof(proof));
            }

            byte[] proofBytes;
            try
            {
                proofBytes = Convert.FromBase64String(proof);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Proof is not valid Base64.", nameof(proof), ex);
            }

            // HMAC-SHA256 produces 32 bytes
            if (proofBytes.Length != 32)
            {
                throw new ArgumentException(
                    $"Proof must be exactly 32 bytes (HMAC-SHA256). Got {proofBytes.Length} bytes.",
                    nameof(proof));
            }

            return proofBytes;
        }

        /// <summary>
        /// Converts a ZKP salt (Base64-encoded) to raw bytes.
        /// </summary>
        /// <param name="salt">The Base64-encoded salt.</param>
        /// <returns>The salt as raw bytes (minimum 16 bytes).</returns>
        /// <exception cref="ArgumentException">Thrown when the salt is invalid or too short.</exception>
        public static byte[] ConvertSaltToBytes(string salt)
        {
            if (string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException("Salt cannot be null or empty.", nameof(salt));
            }

            byte[] saltBytes;
            try
            {
                saltBytes = Convert.FromBase64String(salt);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Salt is not valid Base64.", nameof(salt), ex);
            }

            // Salt should be at least 16 bytes for security
            if (saltBytes.Length < 16)
            {
                throw new ArgumentException(
                    $"Salt must be at least 16 bytes. Got {saltBytes.Length} bytes.",
                    nameof(salt));
            }

            return saltBytes;
        }
    }
}

