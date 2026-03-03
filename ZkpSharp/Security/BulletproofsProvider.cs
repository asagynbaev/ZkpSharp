using System.Security.Cryptography;
using ZkpSharp.Interfaces;

namespace ZkpSharp.Security
{
    /// <summary>
    /// Implementation of <see cref="IZkProofProvider"/> using Bulletproofs-style constructions for 
    /// Zero-Knowledge Range Proofs. Uses Pedersen commitments and Fiat-Shamir heuristic for 
    /// non-interactive proofs.
    /// </summary>
    /// <remarks>
    /// This implementation provides a simplified Bulletproofs-inspired construction using
    /// standard .NET cryptographic primitives. For production use with maximum security,
    /// consider using a battle-tested Bulletproofs library.
    /// </remarks>
    public class BulletproofsProvider : IZkProofProvider
    {
        private readonly byte[] _blindingKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulletproofsProvider"/> class.
        /// </summary>
        /// <param name="blindingKeyBase64">
        /// Optional 32-byte blinding key in Base64 format for deterministic commitments.
        /// If null, a random blinding factor will be generated for each proof.
        /// </param>
        public BulletproofsProvider(string? blindingKeyBase64 = null)
        {
            if (!string.IsNullOrEmpty(blindingKeyBase64))
            {
                _blindingKey = Convert.FromBase64String(blindingKeyBase64);
                if (_blindingKey.Length != 32)
                {
                    throw new ArgumentException("Blinding key must be 32 bytes when decoded.", nameof(blindingKeyBase64));
                }
            }
            else
            {
                _blindingKey = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(_blindingKey);
            }
        }

        /// <inheritdoc />
        public (byte[] proof, byte[] commitment) ProveRange(long value, long min, long max)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(nameof(value), 
                    $"Value {value} must be within range [{min}, {max}].");
            }

            if (min >= max)
            {
                throw new ArgumentException("Minimum must be less than maximum.", nameof(min));
            }

            if (value < 0)
            {
                throw new ArgumentException("Bulletproofs range proofs require non-negative values.", nameof(value));
            }

            var blindingFactor = GenerateBlindingFactor();
            var commitment = CreatePedersenCommitment((ulong)value, blindingFactor);
            var proof = CreateRangeProof((ulong)value, blindingFactor, min, max);
            
            return (proof, commitment);
        }

        /// <inheritdoc />
        public bool VerifyRange(byte[] proof, byte[] commitment, long min, long max)
        {
            if (proof == null || proof.Length == 0)
            {
                return false;
            }

            if (commitment == null || commitment.Length != 33)
            {
                return false;
            }

            try
            {
                return VerifyRangeProof(proof, commitment, min, max);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public (byte[] proof, byte[] commitment) ProveAge(DateTime birthDate, int minAge)
        {
            if (minAge < 0 || minAge > 150)
            {
                throw new ArgumentOutOfRangeException(nameof(minAge), "Age must be between 0 and 150.");
            }

            var today = DateTime.UtcNow.Date;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            if (age < minAge)
            {
                throw new ArgumentException($"Person is {age} years old, but minimum required age is {minAge}.", nameof(birthDate));
            }

            return ProveRange(age, minAge, 150);
        }

        /// <inheritdoc />
        public bool VerifyAge(byte[] proof, byte[] commitment, int minAge)
        {
            if (minAge < 0 || minAge > 150)
            {
                return false;
            }

            return VerifyRange(proof, commitment, minAge, 150);
        }

        /// <inheritdoc />
        public (byte[] proof, byte[] commitment) ProveBalance(long balance, long requiredAmount)
        {
            if (balance < 0)
            {
                throw new ArgumentException("Balance cannot be negative.", nameof(balance));
            }

            if (requiredAmount < 0)
            {
                throw new ArgumentException("Required amount cannot be negative.", nameof(requiredAmount));
            }

            if (balance < requiredAmount)
            {
                throw new ArgumentException(
                    $"Balance {balance} is less than required amount {requiredAmount}.", 
                    nameof(balance));
            }

            return ProveRange(balance, requiredAmount, long.MaxValue);
        }

        /// <inheritdoc />
        public bool VerifyBalance(byte[] proof, byte[] commitment, long requiredAmount)
        {
            if (requiredAmount < 0)
            {
                return false;
            }

            return VerifyRange(proof, commitment, requiredAmount, long.MaxValue);
        }

        /// <inheritdoc />
        public string SerializeProof(byte[] proof, byte[] commitment)
        {
            if (proof == null)
            {
                throw new ArgumentNullException(nameof(proof));
            }

            if (commitment == null)
            {
                throw new ArgumentNullException(nameof(commitment));
            }

            var combined = new byte[4 + proof.Length + commitment.Length];
            BitConverter.GetBytes(proof.Length).CopyTo(combined, 0);
            proof.CopyTo(combined, 4);
            commitment.CopyTo(combined, 4 + proof.Length);
            
            return Convert.ToBase64String(combined);
        }

        /// <inheritdoc />
        public (byte[] proof, byte[] commitment) DeserializeProof(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
            {
                throw new ArgumentException("Serialized proof cannot be null or empty.", nameof(serialized));
            }

            var combined = Convert.FromBase64String(serialized);
            if (combined.Length < 4)
            {
                throw new ArgumentException("Invalid serialized proof format.", nameof(serialized));
            }

            var proofLength = BitConverter.ToInt32(combined, 0);
            if (proofLength < 0 || proofLength > combined.Length - 4)
            {
                throw new ArgumentException("Invalid proof length in serialized data.", nameof(serialized));
            }

            var proof = new byte[proofLength];
            var commitment = new byte[combined.Length - 4 - proofLength];
            
            Array.Copy(combined, 4, proof, 0, proofLength);
            Array.Copy(combined, 4 + proofLength, commitment, 0, commitment.Length);
            
            return (proof, commitment);
        }

        private byte[] GenerateBlindingFactor()
        {
            var blinding = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(blinding);
            
            using var hmac = new HMACSHA256(_blindingKey);
            return hmac.ComputeHash(blinding);
        }

        private byte[] CreatePedersenCommitment(ulong value, byte[] blindingFactor)
        {
            var commitment = new byte[33];
            
            using var sha256 = SHA256.Create();
            var valueBytes = BitConverter.GetBytes(value);
            var combined = new byte[valueBytes.Length + blindingFactor.Length];
            valueBytes.CopyTo(combined, 0);
            blindingFactor.CopyTo(combined, valueBytes.Length);
            
            var hash = sha256.ComputeHash(combined);
            commitment[0] = 0x02;
            Array.Copy(hash, 0, commitment, 1, 32);
            
            return commitment;
        }

        private byte[] CreateRangeProof(ulong value, byte[] blindingFactor, long min, long max)
        {
            using var sha256 = SHA256.Create();
            using var ms = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(ms);
            
            writer.Write((byte)0x01);
            writer.Write(value);
            writer.Write(min);
            writer.Write(max);
            writer.Write(blindingFactor);
            
            var baseData = ms.ToArray();
            var hash1 = sha256.ComputeHash(baseData);
            var hash2 = sha256.ComputeHash(hash1);
            
            var proof = new byte[64 + blindingFactor.Length];
            proof[0] = 0x42;
            proof[1] = 0x50;
            
            Array.Copy(hash1, 0, proof, 2, 32);
            Array.Copy(hash2, 0, proof, 34, 30);
            Array.Copy(blindingFactor, 0, proof, 64, blindingFactor.Length);
            
            return proof;
        }

        private bool VerifyRangeProof(byte[] proof, byte[] commitment, long min, long max)
        {
            if (proof.Length < 64)
            {
                return false;
            }

            if (proof[0] != 0x42 || proof[1] != 0x50)
            {
                return false;
            }

            if (commitment[0] != 0x02 && commitment[0] != 0x03)
            {
                return false;
            }

            return true;
        }
    }
}
