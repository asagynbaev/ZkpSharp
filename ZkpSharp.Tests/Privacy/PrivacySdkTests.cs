using Xunit;
using ZkpSharp.Privacy;

namespace ZkpSharp.Tests.Privacy
{
    public class ConfidentialTransferTests
    {
        private readonly ConfidentialTransfer _ct = new();

        [Fact]
        public void CreateAndVerify_ValidTransfer()
        {
            var bundle = _ct.CreateTransfer(senderBalance: 10000, transferAmount: 2500);

            Assert.NotEmpty(bundle.AmountCommitment);
            Assert.NotEmpty(bundle.AmountProof);
            Assert.NotEmpty(bundle.ChangeCommitment);
            Assert.NotEmpty(bundle.ChangeProof);
            Assert.True(_ct.VerifyTransfer(bundle));
        }

        [Fact]
        public void Verify_TamperedProof_Fails()
        {
            var bundle = _ct.CreateTransfer(10000, 3000);
            bundle.AmountProof[10] ^= 0xFF;
            Assert.False(_ct.VerifyTransfer(bundle));
        }

        [Fact]
        public void CreateTransfer_ExceedsBalance_Throws()
        {
            Assert.Throws<ArgumentException>(() => _ct.CreateTransfer(1000, 2000));
        }

        [Fact]
        public void CreateTransfer_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ct.CreateTransfer(1000, -1));
        }

        [Fact]
        public void CreateTransfer_ZeroAmount_Valid()
        {
            var bundle = _ct.CreateTransfer(5000, 0);
            Assert.True(_ct.VerifyTransfer(bundle));
        }

        [Fact]
        public void CreateTransfer_FullBalance_Valid()
        {
            var bundle = _ct.CreateTransfer(5000, 5000);
            Assert.True(_ct.VerifyTransfer(bundle));
        }

        [Fact]
        public void Serialize_Deserialize_RoundTrip()
        {
            var bundle = _ct.CreateTransfer(10000, 4000);
            string serialized = _ct.Serialize(bundle);
            var restored = _ct.Deserialize(serialized);
            Assert.True(_ct.VerifyTransfer(restored));
        }
    }

    public class SealedBidAuctionTests
    {
        [Fact]
        public void PlaceAndVerify_ValidBid()
        {
            var auction = new SealedBidAuction(minBid: 100, maxBid: 50000);
            var (bid, secret) = auction.PlaceBid(7500);

            Assert.True(auction.VerifyBid(bid));
            Assert.Equal(7500, secret.Amount);
        }

        [Fact]
        public void RevealBid_MatchesOriginal()
        {
            var auction = new SealedBidAuction(100, 50000);
            var (bid, secret) = auction.PlaceBid(12000);

            long? revealed = auction.RevealBid(bid, secret);
            Assert.Equal(12000, revealed);
        }

        [Fact]
        public void RevealBid_ForgedOpening_ReturnsNull()
        {
            var auction = new SealedBidAuction(100, 50000);
            var (bid, _) = auction.PlaceBid(5000);

            var fakeBid2 = auction.PlaceBid(9999);
            long? revealed = auction.RevealBid(bid, fakeBid2.secret);
            Assert.Null(revealed);
        }

        [Fact]
        public void PlaceBid_OutOfRange_Throws()
        {
            var auction = new SealedBidAuction(100, 50000);
            Assert.Throws<ArgumentOutOfRangeException>(() => auction.PlaceBid(50));
            Assert.Throws<ArgumentOutOfRangeException>(() => auction.PlaceBid(60000));
        }

        [Fact]
        public void DetermineWinner_PicksHighest()
        {
            var auction = new SealedBidAuction(100, 50000);

            var (bid1, open1) = auction.PlaceBid(5000);
            var (bid2, open2) = auction.PlaceBid(15000);
            var (bid3, open3) = auction.PlaceBid(8000);

            int winner = auction.DetermineWinner(
                new[] { bid1, bid2, bid3 },
                new[] { open1, open2, open3 });

            Assert.Equal(1, winner);
        }

        [Fact]
        public void PlaceBid_MinimumBid_Valid()
        {
            var auction = new SealedBidAuction(100, 50000);
            var (bid, secret) = auction.PlaceBid(100);
            Assert.True(auction.VerifyBid(bid));
            Assert.Equal(100, auction.RevealBid(bid, secret));
        }
    }

    public class PrivateVotingTests
    {
        private readonly PrivateVoting _voting = new();

        [Fact]
        public void CastAndVerify_YesVote()
        {
            var (ballot, _) = _voting.CastVote(true);
            Assert.True(_voting.VerifyBallot(ballot));
        }

        [Fact]
        public void CastAndVerify_NoVote()
        {
            var (ballot, _) = _voting.CastVote(false);
            Assert.True(_voting.VerifyBallot(ballot));
        }

        [Fact]
        public void OpenBallot_RevealsCorrectVote()
        {
            var (ballot, secret) = _voting.CastVote(true);
            bool? vote = _voting.OpenBallot(ballot, secret);
            Assert.True(vote);
        }

        [Fact]
        public void OpenBallot_ForgedSecret_ReturnsNull()
        {
            var (ballot1, _) = _voting.CastVote(true);
            var (_, secret2) = _voting.CastVote(false);
            Assert.Null(_voting.OpenBallot(ballot1, secret2));
        }

        [Fact]
        public void Tally_CorrectCounts()
        {
            var (b1, s1) = _voting.CastVote(true);
            var (b2, s2) = _voting.CastVote(false);
            var (b3, s3) = _voting.CastVote(true);
            var (b4, s4) = _voting.CastVote(true);

            var result = _voting.Tally(
                new[] { b1, b2, b3, b4 },
                new[] { s1, s2, s3, s4 });

            Assert.NotNull(result);
            Assert.Equal(3, result!.YesCount);
            Assert.Equal(1, result.NoCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Tally_WithInvalidBallot_ReturnsNull()
        {
            var (b1, s1) = _voting.CastVote(true);
            var (b2, _) = _voting.CastVote(false);
            var (_, s3) = _voting.CastVote(true);

            var result = _voting.Tally(
                new[] { b1, b2 },
                new[] { s1, s3 });

            Assert.Null(result);
        }
    }

    public class CredentialProofTests
    {
        private readonly CredentialProof _cp = new();

        [Fact]
        public void ProveMinimum_IncomeVerification()
        {
            var bundle = _cp.ProveMinimum(
                actualValue: 85000,
                minimumRequired: 50000,
                label: "annual_income");

            Assert.Equal("annual_income", bundle.Label);
            Assert.Equal(CredentialProofType.Minimum, bundle.ProofType);
            Assert.True(_cp.Verify(bundle));
        }

        [Fact]
        public void ProveRange_CreditScore()
        {
            var bundle = _cp.ProveRange(
                actualValue: 750,
                min: 700,
                max: 850,
                label: "credit_score");

            Assert.Equal("credit_score", bundle.Label);
            Assert.Equal(CredentialProofType.Range, bundle.ProofType);
            Assert.True(_cp.Verify(bundle));
        }

        [Fact]
        public void ProveMinimum_BelowThreshold_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                _cp.ProveMinimum(30000, 50000, "annual_income"));
        }

        [Fact]
        public void ProveRange_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _cp.ProveRange(650, 700, 850, "credit_score"));
        }

        [Fact]
        public void Verify_TamperedProof_Fails()
        {
            var bundle = _cp.ProveMinimum(100000, 50000, "income");
            bundle.RangeProof[5] ^= 0xFF;
            Assert.False(_cp.Verify(bundle));
        }

        [Fact]
        public void Serialize_Deserialize_RoundTrip()
        {
            var bundle = _cp.ProveRange(780, 700, 850, "credit_score");
            string serialized = _cp.Serialize(bundle);
            var restored = _cp.Deserialize(serialized);

            Assert.Equal("credit_score", restored.Label);
            Assert.Equal(CredentialProofType.Range, restored.ProofType);
            Assert.Equal(700, restored.Threshold);
            Assert.True(_cp.Verify(restored));
        }

        [Fact]
        public void ProveMinimum_AccountBalance()
        {
            var bundle = _cp.ProveMinimum(
                actualValue: 25000,
                minimumRequired: 10000,
                label: "account_balance");

            Assert.True(_cp.Verify(bundle));
        }
    }
}
