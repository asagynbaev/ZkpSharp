using ZkpSharp.Crypto;
using ZkpSharp.Crypto.Bulletproofs;
using ZkpSharp.Crypto.Secp256k1;

namespace ZkpSharp.Privacy
{
    /// <summary>
    /// Anonymous voting with cryptographic guarantees.
    /// Each voter commits to a binary vote (yes/no) using a Pedersen commitment
    /// and proves the vote is valid (0 or 1) via a Bulletproofs range proof.
    /// Individual votes remain hidden. The tally is computed by collecting
    /// ballot openings from voters -- no single party can see individual votes
    /// during the voting phase.
    /// </summary>
    public class PrivateVoting
    {
        private const int BitSize = 64;

        /// <summary>
        /// Casts a private vote. Returns a public Ballot (commitment + validity proof)
        /// and a secret BallotSecret that the voter retains until tallying.
        /// </summary>
        /// <param name="voteYes">true = yes, false = no.</param>
        public (Ballot ballot, BallotSecret secret) CastVote(bool voteYes)
        {
            long value = voteYes ? 1L : 0L;
            var blinding = Scalar.Random();
            var (proof, V) = RangeProof.Prove(Scalar.From(value), blinding, BitSize);

            var ballot = new Ballot
            {
                Commitment = V.Encode(),
                ValidityProof = proof.ToBytes()
            };

            var secret = new BallotSecret
            {
                Vote = voteYes,
                BlindingFactor = blinding.ToBytes()
            };

            return (ballot, secret);
        }

        /// <summary>
        /// Verifies that a ballot contains a valid vote (0 or 1) without
        /// learning which way the voter voted.
        /// </summary>
        public bool VerifyBallot(Ballot ballot)
        {
            if (ballot?.ValidityProof == null || ballot.Commitment == null)
                return false;
            try
            {
                var V = Point.Decode(ballot.Commitment);
                var proof = RangeProof.FromBytes(ballot.ValidityProof);
                return RangeProof.Verify(V, proof, BitSize);
            }
            catch { return false; }
        }

        /// <summary>
        /// Opens a ballot to reveal the vote. Verifies the opening matches
        /// the original commitment. Returns null if the opening is invalid.
        /// </summary>
        public bool? OpenBallot(Ballot ballot, BallotSecret secret)
        {
            if (ballot?.Commitment == null || secret == null)
                return null;
            try
            {
                long value = secret.Vote ? 1L : 0L;
                var blinding = Scalar.FromBytes(secret.BlindingFactor);
                var expected = PedersenCommitment.Commit(Scalar.From(value), blinding);

                if (!expected.Encode().SequenceEqual(ballot.Commitment))
                    return null;

                return secret.Vote;
            }
            catch { return null; }
        }

        /// <summary>
        /// Tallies votes by collecting ballot openings.
        /// Verifies each opening against its commitment before counting.
        /// Returns (yesCount, noCount) or null if any opening is invalid.
        /// </summary>
        public TallyResult? Tally(Ballot[] ballots, BallotSecret[] secrets)
        {
            if (ballots.Length != secrets.Length) return null;

            int yes = 0, no = 0;

            for (int i = 0; i < ballots.Length; i++)
            {
                var vote = OpenBallot(ballots[i], secrets[i]);
                if (!vote.HasValue) return null;
                if (vote.Value) yes++; else no++;
            }

            return new TallyResult { YesCount = yes, NoCount = no, TotalCount = ballots.Length };
        }
    }

    /// <summary>Public ballot: commitment + validity proof. Safe to publish.</summary>
    public class Ballot
    {
        /// <summary>Pedersen commitment to the vote value (0 or 1).</summary>
        public byte[] Commitment { get; init; } = Array.Empty<byte>();
        /// <summary>Range proof proving the vote is valid (0 or 1).</summary>
        public byte[] ValidityProof { get; init; } = Array.Empty<byte>();
    }

    /// <summary>Secret ballot opening. Voter retains this until tallying.</summary>
    public class BallotSecret
    {
        /// <summary>The actual vote (true = yes, false = no).</summary>
        public bool Vote { get; init; }
        /// <summary>The blinding factor used in the commitment.</summary>
        public byte[] BlindingFactor { get; init; } = Array.Empty<byte>();
    }

    /// <summary>Result of tallying all ballots.</summary>
    public class TallyResult
    {
        public int YesCount { get; init; }
        public int NoCount { get; init; }
        public int TotalCount { get; init; }
    }
}
