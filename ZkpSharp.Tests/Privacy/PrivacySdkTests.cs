using Xunit;
using ZkpSharp.Attestations;

namespace ZkpSharp.Tests.Privacy
{
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
