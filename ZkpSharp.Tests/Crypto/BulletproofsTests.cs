using Xunit;
using ZkpSharp.Crypto;
using ZkpSharp.Crypto.Bulletproofs;
using ZkpSharp.Crypto.Secp256k1;
using ZkpSharp.Security;

namespace ZkpSharp.Tests.Crypto
{
    public class BulletproofsTests
    {
        // Use small n for faster tests where possible
        private const int SmallN = 8;

        #region Inner Product Argument Tests

        [Fact]
        public void InnerProductProof_SmallVectors_ProveAndVerify()
        {
            int n = 4;
            var g = Generators.Gi[..n];
            var h = Generators.Hi[..n];
            var u = Scalar.Random() * Generators.G;

            var a = new Scalar[] { new(1), new(2), new(3), new(4) };
            var b = new Scalar[] { new(5), new(6), new(7), new(8) };
            var c = Scalar.InnerProduct(a, b);

            var P = Point.Infinity;
            for (int i = 0; i < n; i++)
                P = P + a[i] * g[i] + b[i] * h[i];
            P = P + c * u;

            var transcript1 = new Transcript("test_ipa");
            var proof = InnerProductProof.Create(g, h, u, a, b, transcript1);

            var transcript2 = new Transcript("test_ipa");
            bool valid = InnerProductProof.Verify(n, g, h, u, P, proof, transcript2);
            Assert.True(valid);
        }

        [Fact]
        public void InnerProductProof_TamperedProof_Fails()
        {
            int n = 4;
            var g = Generators.Gi[..n];
            var h = Generators.Hi[..n];
            var u = Scalar.Random() * Generators.G;

            var a = new Scalar[] { new(1), new(2), new(3), new(4) };
            var b = new Scalar[] { new(5), new(6), new(7), new(8) };
            var c = Scalar.InnerProduct(a, b);

            var P = Point.Infinity;
            for (int i = 0; i < n; i++)
                P = P + a[i] * g[i] + b[i] * h[i];
            P = P + c * u;

            var transcript1 = new Transcript("test_ipa");
            var proof = InnerProductProof.Create(g, h, u, a, b, transcript1);

            var tampered = new InnerProductProof(proof.Ls, proof.Rs, proof.A + Scalar.One, proof.B);

            var transcript2 = new Transcript("test_ipa");
            bool valid = InnerProductProof.Verify(n, g, h, u, P, tampered, transcript2);
            Assert.False(valid);
        }

        [Fact]
        public void InnerProductProof_SerializationRoundTrip()
        {
            int n = 4;
            var g = Generators.Gi[..n];
            var h = Generators.Hi[..n];
            var u = Scalar.Random() * Generators.G;

            var a = new Scalar[] { new(10), new(20), new(30), new(40) };
            var b = new Scalar[] { new(1), new(2), new(3), new(4) };

            var transcript = new Transcript("test_ipa_ser");
            var proof = InnerProductProof.Create(g, h, u, a, b, transcript);

            var bytes = proof.ToBytes();
            var deserialized = InnerProductProof.FromBytes(bytes);

            Assert.Equal(proof.A, deserialized.A);
            Assert.Equal(proof.B, deserialized.B);
            Assert.Equal(proof.Ls.Length, deserialized.Ls.Length);
        }

        #endregion

        #region Range Proof Tests

        [Fact]
        public void RangeProof_ValidValue_ProvesAndVerifies()
        {
            var v = Scalar.From(42);
            var gamma = Scalar.Random();
            var (proof, V) = RangeProof.Prove(v, gamma, SmallN);
            bool valid = RangeProof.Verify(V, proof, SmallN);
            Assert.True(valid);
        }

        [Fact]
        public void RangeProof_ZeroValue_Succeeds()
        {
            var v = Scalar.Zero;
            var gamma = Scalar.Random();
            var (proof, V) = RangeProof.Prove(v, gamma, SmallN);
            Assert.True(RangeProof.Verify(V, proof, SmallN));
        }

        [Fact]
        public void RangeProof_MaxValue_Succeeds()
        {
            var maxVal = Scalar.From((1L << SmallN) - 1); // 2^n - 1
            var gamma = Scalar.Random();
            var (proof, V) = RangeProof.Prove(maxVal, gamma, SmallN);
            Assert.True(RangeProof.Verify(V, proof, SmallN));
        }

        [Fact]
        public void RangeProof_OutOfRange_ThrowsOnProve()
        {
            var tooLarge = Scalar.From(1L << SmallN); // 2^n, out of range
            var gamma = Scalar.Random();
            Assert.Throws<ArgumentOutOfRangeException>(() => RangeProof.Prove(tooLarge, gamma, SmallN));
        }

        [Fact]
        public void RangeProof_TamperedCommitment_Fails()
        {
            var v = Scalar.From(50);
            var gamma = Scalar.Random();
            var (proof, V) = RangeProof.Prove(v, gamma, SmallN);

            var wrongV = PedersenCommitment.Commit(Scalar.From(51), gamma);
            Assert.False(RangeProof.Verify(wrongV, proof, SmallN));
        }

        [Fact]
        public void RangeProof_SerializationRoundTrip()
        {
            var v = Scalar.From(100);
            var gamma = Scalar.Random();
            var (proof, V) = RangeProof.Prove(v, gamma, SmallN);

            var bytes = proof.ToBytes();
            var deserialized = RangeProof.FromBytes(bytes);

            Assert.True(RangeProof.Verify(V, deserialized, SmallN));
        }

        #endregion

        #region BulletproofsProvider Tests

        [Fact]
        public void BulletproofsProvider_ProveRange_ValidValue()
        {
            var provider = new BulletproofsProvider(SmallN);
            var (proof, commitment) = provider.ProveRange(50, 0, 100);

            Assert.NotNull(proof);
            Assert.NotNull(commitment);
            Assert.Equal(33, commitment.Length);
            Assert.True(proof.Length > 100);
        }

        [Fact]
        public void BulletproofsProvider_ProveAndVerifyRange()
        {
            var provider = new BulletproofsProvider(SmallN);
            var (proof, commitment) = provider.ProveRange(50, 0, 100);
            Assert.True(provider.VerifyRange(proof, commitment, 0, 100));
        }

        [Fact]
        public void BulletproofsProvider_VerifyRange_TamperedProof_Fails()
        {
            var provider = new BulletproofsProvider(SmallN);
            var (proof, commitment) = provider.ProveRange(50, 0, 100);

            proof[10] ^= 0xFF;
            Assert.False(provider.VerifyRange(proof, commitment, 0, 100));
        }

        [Fact]
        public void BulletproofsProvider_ProveRange_OutOfRange_Throws()
        {
            var provider = new BulletproofsProvider(SmallN);
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.ProveRange(101, 0, 100));
        }

        [Fact]
        public void BulletproofsProvider_ProveAge_Valid()
        {
            var provider = new BulletproofsProvider(SmallN);
            var birthDate = DateTime.UtcNow.AddYears(-25);
            var (proof, commitment) = provider.ProveAge(birthDate, 18);
            Assert.True(provider.VerifyAge(proof, commitment, 18));
        }

        [Fact]
        public void BulletproofsProvider_ProveAge_TooYoung_Throws()
        {
            var provider = new BulletproofsProvider(SmallN);
            var birthDate = DateTime.UtcNow.AddYears(-15);
            Assert.Throws<ArgumentException>(() => provider.ProveAge(birthDate, 18));
        }

        [Fact]
        public void BulletproofsProvider_ProveBalance_Valid()
        {
            var provider = new BulletproofsProvider(SmallN);
            var (proof, commitment) = provider.ProveBalance(100, 50);
            Assert.True(provider.VerifyBalance(proof, commitment, 50));
        }

        [Fact]
        public void BulletproofsProvider_ProveBalance_Insufficient_Throws()
        {
            var provider = new BulletproofsProvider(SmallN);
            Assert.Throws<ArgumentException>(() => provider.ProveBalance(50, 100));
        }

        [Fact]
        public void BulletproofsProvider_SerializeDeserialize_RoundTrip()
        {
            var provider = new BulletproofsProvider(SmallN);
            var (proof, commitment) = provider.ProveRange(42, 0, 100);

            string serialized = provider.SerializeProof(proof, commitment);
            var (proof2, commitment2) = provider.DeserializeProof(serialized);

            Assert.Equal(proof, proof2);
            Assert.Equal(commitment, commitment2);
        }

        [Fact]
        public void BulletproofsProvider_NullProof_ReturnsFalse()
        {
            var provider = new BulletproofsProvider(SmallN);
            Assert.False(provider.VerifyRange(null!, new byte[33], 0, 100));
        }

        [Fact]
        public void BulletproofsProvider_EmptyCommitment_ReturnsFalse()
        {
            var provider = new BulletproofsProvider(SmallN);
            Assert.False(provider.VerifyRange(new byte[100], Array.Empty<byte>(), 0, 100));
        }

        #endregion
    }
}
