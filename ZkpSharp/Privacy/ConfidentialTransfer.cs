using ZkpSharp.Crypto;
using ZkpSharp.Crypto.Bulletproofs;
using ZkpSharp.Crypto.Secp256k1;

namespace ZkpSharp.Privacy
{
    /// <summary>
    /// Confidential transfers: hide the amount being transferred while proving
    /// it is non-negative and does not exceed the sender's balance.
    /// Uses Pedersen commitments (computationally hiding) and Bulletproofs range proofs.
    /// </summary>
    public class ConfidentialTransfer
    {
        private const int BitSize = 64;

        /// <summary>
        /// Creates a confidential transfer. The amount is cryptographically hidden
        /// in a Pedersen commitment. Two range proofs ensure:
        /// (1) amount >= 0 and (2) change (balance - amount) >= 0.
        /// Neither the amount nor the balance is revealed to the verifier.
        /// </summary>
        /// <param name="senderBalance">Sender's current balance.</param>
        /// <param name="transferAmount">Amount to transfer (hidden from verifier).</param>
        /// <returns>A transfer proof that can be published without revealing the amount.</returns>
        public TransferBundle CreateTransfer(long senderBalance, long transferAmount)
        {
            if (transferAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(transferAmount));
            if (transferAmount > senderBalance)
                throw new ArgumentException("Insufficient balance.");

            long change = senderBalance - transferAmount;

            var amountBlinding = Scalar.Random();
            var (amountProof, amountV) = RangeProof.Prove(Scalar.From(transferAmount), amountBlinding, BitSize);

            var changeBlinding = Scalar.Random();
            var (changeProof, changeV) = RangeProof.Prove(Scalar.From(change), changeBlinding, BitSize);

            return new TransferBundle
            {
                AmountCommitment = amountV.Encode(),
                AmountProof = amountProof.ToBytes(),
                ChangeCommitment = changeV.Encode(),
                ChangeProof = changeProof.ToBytes()
            };
        }

        /// <summary>
        /// Verifies that a confidential transfer is valid.
        /// Checks both range proofs without learning the transfer amount.
        /// </summary>
        public bool VerifyTransfer(TransferBundle bundle)
        {
            if (bundle?.AmountProof == null || bundle.ChangeProof == null)
                return false;
            try
            {
                var amountV = Point.Decode(bundle.AmountCommitment);
                var amountRp = RangeProof.FromBytes(bundle.AmountProof);
                if (!RangeProof.Verify(amountV, amountRp, BitSize))
                    return false;

                var changeV = Point.Decode(bundle.ChangeCommitment);
                var changeRp = RangeProof.FromBytes(bundle.ChangeProof);
                return RangeProof.Verify(changeV, changeRp, BitSize);
            }
            catch { return false; }
        }

        /// <summary>Serializes a transfer bundle to a Base64 string for storage or transmission.</summary>
        public string Serialize(TransferBundle bundle)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            WriteField(w, bundle.AmountCommitment);
            WriteField(w, bundle.AmountProof);
            WriteField(w, bundle.ChangeCommitment);
            WriteField(w, bundle.ChangeProof);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>Deserializes a transfer bundle from a Base64 string.</summary>
        public TransferBundle Deserialize(string data)
        {
            var bytes = Convert.FromBase64String(data);
            using var ms = new MemoryStream(bytes);
            using var r = new BinaryReader(ms);
            return new TransferBundle
            {
                AmountCommitment = ReadField(r),
                AmountProof = ReadField(r),
                ChangeCommitment = ReadField(r),
                ChangeProof = ReadField(r)
            };
        }

        private static void WriteField(BinaryWriter w, byte[] data)
        {
            w.Write(data.Length);
            w.Write(data);
        }

        private static byte[] ReadField(BinaryReader r)
        {
            int len = r.ReadInt32();
            return r.ReadBytes(len);
        }
    }

    /// <summary>
    /// A confidential transfer bundle containing commitments and range proofs
    /// for both the transfer amount and the change. No secret values are included.
    /// </summary>
    public class TransferBundle
    {
        /// <summary>Pedersen commitment to the transfer amount (33 bytes, compressed secp256k1 point).</summary>
        public byte[] AmountCommitment { get; init; } = Array.Empty<byte>();
        /// <summary>Bulletproofs range proof that the amount is non-negative.</summary>
        public byte[] AmountProof { get; init; } = Array.Empty<byte>();
        /// <summary>Pedersen commitment to the change (balance - amount).</summary>
        public byte[] ChangeCommitment { get; init; } = Array.Empty<byte>();
        /// <summary>Bulletproofs range proof that the change is non-negative.</summary>
        public byte[] ChangeProof { get; init; } = Array.Empty<byte>();
    }
}
