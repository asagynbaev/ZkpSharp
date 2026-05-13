using ZkpSharp.Cryptography;
using ZkpSharp.Cryptography.Bulletproofs;
using ZkpSharp.Cryptography.Secp256k1;

namespace ZkpSharp.Attestations
{
    /// <summary>
    /// Predicate proof over a committed attestation value.
    /// Proves a numeric attribute (income, score, age, balance) meets a threshold or lies
    /// within a range without revealing the actual value. Built on Bulletproofs over secp256k1.
    /// </summary>
    public class CredentialProof
    {
        private const int BitSize = 64;

        /// <summary>
        /// Prove that <paramref name="actualValue"/> ≥ <paramref name="minimumRequired"/>.
        /// </summary>
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
                ProofType = CredentialProofType.Minimum,
            };
        }

        /// <summary>
        /// Prove that <paramref name="actualValue"/> ∈ [<paramref name="min"/>, <paramref name="max"/>].
        /// </summary>
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
                ProofType = CredentialProofType.Range,
            };
        }

        /// <summary>
        /// Verify a credential bundle without learning the underlying value.
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
                RangeProof = proof,
            };
        }
    }

    /// <summary>A verifiable credential bundle. Contains no secret values — safe to share.</summary>
    public class CredentialBundle
    {
        public string Label { get; init; } = "";
        public byte[] Commitment { get; init; } = Array.Empty<byte>();
        public byte[] RangeProof { get; init; } = Array.Empty<byte>();
        public long Threshold { get; init; }
        public long? UpperBound { get; init; }
        public CredentialProofType ProofType { get; init; }
    }

    public enum CredentialProofType
    {
        Minimum,
        Range,
    }
}
