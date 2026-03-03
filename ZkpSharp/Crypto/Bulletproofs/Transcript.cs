using System.Security.Cryptography;
using System.Text;
using ZkpSharp.Crypto.Secp256k1;

namespace ZkpSharp.Crypto.Bulletproofs
{
    /// <summary>
    /// Fiat-Shamir transcript for non-interactive zero-knowledge proofs.
    /// Absorbs protocol messages and produces deterministic challenges.
    /// </summary>
    public sealed class Transcript
    {
        private readonly MemoryStream _state;

        public Transcript(string domainSeparator)
        {
            _state = new MemoryStream();
            AppendMessage("dom-sep", Encoding.UTF8.GetBytes(domainSeparator));
        }

        public void AppendMessage(string label, byte[] message)
        {
            var labelBytes = Encoding.UTF8.GetBytes(label);
            _state.Write(labelBytes);
            _state.Write(BitConverter.GetBytes(message.Length));
            _state.Write(message);
        }

        public void AppendPoint(string label, Point point)
            => AppendMessage(label, point.Encode());

        public void AppendScalar(string label, Scalar scalar)
            => AppendMessage(label, scalar.ToBytes());

        public void AppendU64(string label, ulong value)
            => AppendMessage(label, BitConverter.GetBytes(value));

        /// <summary>
        /// Squeeze a challenge scalar from the transcript.
        /// Feeds the resulting hash back into the state for domain separation.
        /// </summary>
        public Scalar ChallengeScalar(string label)
        {
            var labelBytes = Encoding.UTF8.GetBytes(label);
            _state.Write(labelBytes);

            var hash = SHA256.HashData(_state.ToArray());
            _state.Write(hash);
            return Scalar.FromBytes(hash);
        }
    }
}
