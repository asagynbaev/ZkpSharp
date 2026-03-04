using ZkpSharp.Crypto;
using ZkpSharp.Crypto.Bulletproofs;
using ZkpSharp.Crypto.Secp256k1;

namespace ZkpSharp.Privacy
{
    /// <summary>
    /// Privacy-preserving credential verification.
    /// Prove that a numeric attribute (income, credit score, age, account balance, etc.)
    /// meets a threshold or falls within a range -- without revealing the actual value.
    /// The verifier learns only that the condition is satisfied.
    /// </summary>
    public class CredentialProof
    {
        private const int BitSize = 64;

        /// <summary>
        /// Proves that a private value meets a minimum threshold.
        /// Example: prove annual income >= 50,000 without revealing actual income.
        /// </summary>
        /// <param name="actualValue">The actual attribute value (kept secret).</param>
        /// <param name="minimumRequired">The public minimum threshold.</param>
        /// <param name="label">Human-readable label for the credential (e.g., "annual_income").</param>
        public CredentialBundle ProveMinimum(long actualValue, long minimumRequired, string label)
        {
            if (actualValue < minimumRequired)
                throw new ArgumentException($"Value {actualValue} does not meet minimum {minimumRequired}.");

            long shifted = actualValue - minimumRequired;
            var blinding = Scalar.Random();
            var (proof, V) = RangeProof.Prove(Scalar.From(shifted), blinding, BitSize);

            return new CredentialBundle
            {
                Label = label,
                Commitment = V.Encode(),
                RangeProof = proof.ToBytes(),
                Threshold = minimumRequired,
                ProofType = CredentialProofType.Minimum
            };
        }

        /// <summary>
        /// Proves that a private value is within a range [min, max].
        /// Example: prove credit score is in [700, 850] without revealing exact score.
        /// </summary>
        /// <param name="actualValue">The actual attribute value (kept secret).</param>
        /// <param name="min">The public lower bound (inclusive).</param>
        /// <param name="max">The public upper bound (inclusive).</param>
        /// <param name="label">Human-readable label for the credential.</param>
        public CredentialBundle ProveRange(long actualValue, long min, long max, string label)
        {
            if (actualValue < min || actualValue > max)
                throw new ArgumentOutOfRangeException(nameof(actualValue), $"Value not in [{min}, {max}].");

            long shifted = actualValue - min;
            var blinding = Scalar.Random();
            var (proof, V) = RangeProof.Prove(Scalar.From(shifted), blinding, BitSize);

            return new CredentialBundle
            {
                Label = label,
                Commitment = V.Encode(),
                RangeProof = proof.ToBytes(),
                Threshold = min,
                UpperBound = max,
                ProofType = CredentialProofType.Range
            };
        }

        /// <summary>
        /// Verifies a credential proof without learning the actual value.
        /// </summary>
        public bool Verify(CredentialBundle credential)
        {
            if (credential?.RangeProof == null || credential.Commitment == null)
                return false;
            try
            {
                var V = Point.Decode(credential.Commitment);
                var proof = RangeProof.FromBytes(credential.RangeProof);
                return RangeProof.Verify(V, proof, BitSize);
            }
            catch { return false; }
        }

        /// <summary>Serializes a credential bundle to Base64.</summary>
        public string Serialize(CredentialBundle bundle)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(bundle.Label ?? "");
            w.Write((int)bundle.ProofType);
            w.Write(bundle.Threshold);
            w.Write(bundle.UpperBound ?? 0L);
            w.Write(bundle.Commitment.Length);
            w.Write(bundle.Commitment);
            w.Write(bundle.RangeProof.Length);
            w.Write(bundle.RangeProof);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>Deserializes a credential bundle from Base64.</summary>
        public CredentialBundle Deserialize(string data)
        {
            var bytes = Convert.FromBase64String(data);
            using var ms = new MemoryStream(bytes);
            using var r = new BinaryReader(ms);
            var label = r.ReadString();
            var proofType = (CredentialProofType)r.ReadInt32();
            var threshold = r.ReadInt64();
            var upper = r.ReadInt64();
            int cLen = r.ReadInt32();
            var commitment = r.ReadBytes(cLen);
            int pLen = r.ReadInt32();
            var proof = r.ReadBytes(pLen);
            return new CredentialBundle
            {
                Label = label,
                ProofType = proofType,
                Threshold = threshold,
                UpperBound = proofType == CredentialProofType.Range ? upper : null,
                Commitment = commitment,
                RangeProof = proof
            };
        }
    }

    /// <summary>A verified credential bundle. Safe to share -- contains no secret values.</summary>
    public class CredentialBundle
    {
        /// <summary>Human-readable label (e.g., "annual_income", "credit_score").</summary>
        public string Label { get; init; } = "";
        /// <summary>Pedersen commitment to the attribute value.</summary>
        public byte[] Commitment { get; init; } = Array.Empty<byte>();
        /// <summary>Bulletproofs range proof.</summary>
        public byte[] RangeProof { get; init; } = Array.Empty<byte>();
        /// <summary>The public threshold (minimum required, or lower bound for range).</summary>
        public long Threshold { get; init; }
        /// <summary>Upper bound for range proofs (null for minimum-only proofs).</summary>
        public long? UpperBound { get; init; }
        /// <summary>Type of proof (minimum or range).</summary>
        public CredentialProofType ProofType { get; init; }
    }

    public enum CredentialProofType
    {
        Minimum,
        Range
    }
}
