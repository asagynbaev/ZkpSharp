using ZkpSharp.Core;
using ZkpSharp.Crypto;
using ZkpSharp.Crypto.Bulletproofs;
using ZkpSharp.Crypto.Secp256k1;
using ZkpSharp.Interfaces;

namespace ZkpSharp.Security
{
    /// <summary>
    /// Zero-knowledge proof provider using real Bulletproofs on secp256k1.
    /// Implements range proofs with Pedersen commitments and the inner product argument.
    /// Proofs are mathematically zero-knowledge under the discrete logarithm assumption.
    /// </summary>
    public class BulletproofsProvider : IZkProofProvider
    {
        private const int DefaultBits = 64;

        /// <summary>
        /// Number of bits for range proofs. Determines max provable value (2^n - 1).
        /// Must be a power of 2. Default: 64.
        /// </summary>
        public int BitSize { get; }

        public BulletproofsProvider(int bitSize = DefaultBits)
        {
            if (bitSize <= 0 || (bitSize & (bitSize - 1)) != 0)
                throw new ArgumentException("Bit size must be a positive power of 2.", nameof(bitSize));
            if (bitSize > Generators.DefaultN)
                throw new ArgumentException($"Bit size must be <= {Generators.DefaultN}.", nameof(bitSize));
            BitSize = bitSize;
        }

        public (byte[] proof, byte[] commitment) ProveRange(long value, long min, long max)
        {
            if (min > max)
                throw new ArgumentException("min must be <= max.");
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is not in [{min}, {max}].");

            long shifted = value - min;
            var v = Scalar.From(shifted);
            var gamma = Scalar.Random();
            var (rangeProof, V) = RangeProof.Prove(v, gamma, BitSize);

            return (rangeProof.ToBytes(), V.Encode());
        }

        public bool VerifyRange(byte[] proof, byte[] commitment, long min, long max)
        {
            if (proof == null || commitment == null || proof.Length == 0 || commitment.Length == 0)
                return false;

            try
            {
                var V = Point.Decode(commitment);
                var rangeProof = RangeProof.FromBytes(proof);
                return RangeProof.Verify(V, rangeProof, BitSize);
            }
            catch
            {
                return false;
            }
        }

        public (byte[] proof, byte[] commitment) ProveAge(DateTime birthDate, int minAge)
        {
            int age = Utilities.CalculateAge(birthDate);
            if (age < minAge)
                throw new ArgumentException($"Age {age} is below minimum {minAge}.");
            return ProveRange(age, minAge, 150);
        }

        public bool VerifyAge(byte[] proof, byte[] commitment, int minAge)
            => VerifyRange(proof, commitment, minAge, 150);

        public (byte[] proof, byte[] commitment) ProveBalance(long balance, long requiredAmount)
        {
            if (balance < requiredAmount)
                throw new ArgumentException($"Balance {balance} is below required {requiredAmount}.");
            return ProveRange(balance, requiredAmount, long.MaxValue);
        }

        public bool VerifyBalance(byte[] proof, byte[] commitment, long requiredAmount)
            => VerifyRange(proof, commitment, requiredAmount, long.MaxValue);

        public string SerializeProof(byte[] proof, byte[] commitment)
        {
            if (proof == null || commitment == null)
                throw new ArgumentNullException();
            var combined = new byte[4 + proof.Length + commitment.Length];
            BitConverter.GetBytes(proof.Length).CopyTo(combined, 0);
            proof.CopyTo(combined, 4);
            commitment.CopyTo(combined, 4 + proof.Length);
            return Convert.ToBase64String(combined);
        }

        public (byte[] proof, byte[] commitment) DeserializeProof(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
                throw new ArgumentException("Serialized proof cannot be null or empty.", nameof(serialized));
            var combined = Convert.FromBase64String(serialized);
            int proofLen = BitConverter.ToInt32(combined, 0);
            var proof = combined[4..(4 + proofLen)];
            var commitment = combined[(4 + proofLen)..];
            return (proof, commitment);
        }

    }
}
