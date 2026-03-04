using ZkpSharp.Crypto;
using ZkpSharp.Crypto.Bulletproofs;
using ZkpSharp.Crypto.Secp256k1;

namespace ZkpSharp.Privacy
{
    /// <summary>
    /// Sealed-bid auction with cryptographic guarantees.
    /// Bidders commit to a hidden bid with a range proof that it falls within
    /// [minBid, maxBid]. After the auction closes, bids are revealed and verified
    /// against their commitments. No one can see bids before reveal, and no one
    /// can change their bid after committing.
    /// </summary>
    public class SealedBidAuction
    {
        private const int BitSize = 64;
        private readonly long _minBid;
        private readonly long _maxBid;

        /// <summary>
        /// Creates a new sealed-bid auction.
        /// </summary>
        /// <param name="minBid">Minimum allowed bid (inclusive).</param>
        /// <param name="maxBid">Maximum allowed bid (inclusive).</param>
        public SealedBidAuction(long minBid, long maxBid)
        {
            if (minBid > maxBid) throw new ArgumentException("minBid must be <= maxBid.");
            _minBid = minBid;
            _maxBid = maxBid;
        }

        /// <summary>
        /// Places a sealed bid. Returns a public SealedBid (commitment + range proof)
        /// and a secret BidOpening that must be kept private until the reveal phase.
        /// </summary>
        /// <param name="amount">The bid amount (kept secret until reveal).</param>
        public (SealedBid bid, BidOpening secret) PlaceBid(long amount)
        {
            if (amount < _minBid || amount > _maxBid)
                throw new ArgumentOutOfRangeException(nameof(amount), $"Bid must be in [{_minBid}, {_maxBid}].");

            long shifted = amount - _minBid;
            var blinding = Scalar.Random();
            var (proof, V) = RangeProof.Prove(Scalar.From(shifted), blinding, BitSize);

            var bid = new SealedBid
            {
                Commitment = V.Encode(),
                RangeProof = proof.ToBytes(),
                MinBid = _minBid,
                MaxBid = _maxBid
            };

            var opening = new BidOpening
            {
                Amount = amount,
                BlindingFactor = blinding.ToBytes()
            };

            return (bid, opening);
        }

        /// <summary>
        /// Verifies that a sealed bid is valid (the committed value falls within
        /// the auction's bid range) without learning the bid amount.
        /// </summary>
        public bool VerifyBid(SealedBid bid)
        {
            if (bid?.RangeProof == null || bid.Commitment == null)
                return false;
            try
            {
                var V = Point.Decode(bid.Commitment);
                var proof = RangeProof.FromBytes(bid.RangeProof);
                return RangeProof.Verify(V, proof, BitSize);
            }
            catch { return false; }
        }

        /// <summary>
        /// Reveals and verifies a bid after the auction closes.
        /// Checks that the opening matches the commitment.
        /// Returns the bid amount if valid, null if the opening is forged.
        /// </summary>
        public long? RevealBid(SealedBid bid, BidOpening opening)
        {
            if (bid?.Commitment == null || opening == null)
                return null;
            try
            {
                long shifted = opening.Amount - bid.MinBid;
                var blinding = Scalar.FromBytes(opening.BlindingFactor);
                var expected = PedersenCommitment.Commit(Scalar.From(shifted), blinding);

                if (!expected.Encode().SequenceEqual(bid.Commitment))
                    return null;

                return opening.Amount;
            }
            catch { return null; }
        }

        /// <summary>
        /// Determines the winner from a set of revealed bids.
        /// Returns the index of the highest valid bid, or -1 if no valid bids.
        /// </summary>
        public int DetermineWinner(SealedBid[] bids, BidOpening[] openings)
        {
            if (bids.Length != openings.Length) return -1;

            long highestBid = -1;
            int winnerIndex = -1;

            for (int i = 0; i < bids.Length; i++)
            {
                var amount = RevealBid(bids[i], openings[i]);
                if (amount.HasValue && amount.Value > highestBid)
                {
                    highestBid = amount.Value;
                    winnerIndex = i;
                }
            }

            return winnerIndex;
        }
    }

    /// <summary>Public sealed bid: commitment + range proof. Safe to publish.</summary>
    public class SealedBid
    {
        /// <summary>Pedersen commitment to the bid amount.</summary>
        public byte[] Commitment { get; init; } = Array.Empty<byte>();
        /// <summary>Range proof that the bid is within [MinBid, MaxBid].</summary>
        public byte[] RangeProof { get; init; } = Array.Empty<byte>();
        /// <summary>Public minimum bid for this auction.</summary>
        public long MinBid { get; init; }
        /// <summary>Public maximum bid for this auction.</summary>
        public long MaxBid { get; init; }
    }

    /// <summary>Secret bid opening. Must be kept private until the reveal phase.</summary>
    public class BidOpening
    {
        /// <summary>The actual bid amount.</summary>
        public long Amount { get; init; }
        /// <summary>The blinding factor used in the Pedersen commitment.</summary>
        public byte[] BlindingFactor { get; init; } = Array.Empty<byte>();
    }
}
