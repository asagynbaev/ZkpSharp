using System;
using Xunit;
using ZkpSharp.Core;
using ZkpSharp.Security;

namespace ZkpSharp.Tests
{
    public class ZKPTests
    {
        [Fact]
        public void TestProveAndVerifyAge_ValidAge_ShouldPass()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            var dateOfBirth = new DateTime(2000, 1, 1);  // Age 25
            var (proof, salt) = zkp.ProveAge(dateOfBirth);

            Assert.True(zkp.VerifyAge(proof, dateOfBirth, salt), "Proof should be valid");
        }

        [Fact]
        public void TestProveAndVerifyAge_InsufficientAge_ShouldFail()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            var dateOfBirth = new DateTime(2010, 1, 1);  // Age 15

            var exception = Assert.Throws<ArgumentException>(() => zkp.ProveAge(dateOfBirth));
            Assert.Equal("Insufficient age", exception.Message);
        }

        [Fact]
        public void TestProveAndVerifyBalance_ValidBalance_ShouldPass()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            double userBalance = 1000.0;
            double requestedAmount = 500.0;

            // Generate proof and salt for balance
            var (proof, salt) = zkp.ProveBalance(userBalance, requestedAmount);

            Assert.True(zkp.VerifyBalance(proof, requestedAmount, salt, userBalance), "Proof should be valid");
        }

        [Fact]
        public void TestProveAndVerifyBalance_InsufficientBalance_ShouldFail()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            double userBalance = 300.0;
            double requestedAmount = 500.0;

            // Test for insufficient balance
            var exception = Assert.Throws<ArgumentException>(() => zkp.ProveBalance(userBalance, requestedAmount));
            Assert.Equal("Insufficient balance", exception.Message);
        }

        [Fact]
        public void TestBalanceVerificationWithSalt_ValidBalance_ShouldPass()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            double userBalance = 1000.0;
            double requestedAmount = 500.0;
            var (proof, salt) = zkp.ProveBalance(userBalance, requestedAmount);

            Assert.True(zkp.VerifyBalance(proof, requestedAmount, salt, userBalance), "Proof should be valid");
        }

        [Fact]
        public void TestBalanceVerificationWithSalt_InsufficientBalance_ShouldFail()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            double userBalance = 100.0;
            double requestedAmount = 150.0;

            var exception = Assert.Throws<ArgumentException>(() => zkp.ProveBalance(userBalance, requestedAmount));
            Assert.Equal("Insufficient balance", exception.Message);
        }

        [Fact]
        public void TestProveAndVerifyAge_InvalidSalt_ShouldFail()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            var dateOfBirth = new DateTime(2000, 1, 1);  // Возраст 25 лет
            var (proof, salt) = zkp.ProveAge(dateOfBirth);

            // Test for invalid salt
            string incorrectSalt = Guid.NewGuid().ToString(); 
            Assert.False(zkp.VerifyAge(proof, dateOfBirth, incorrectSalt), "Proof should fail due to incorrect salt");
        }

        [Fact]
        public void TestBalanceVerificationWithSalt_InvalidSalt_ShouldFail()
        {
            var proofProvider = new ProofProvider("hmacSecretKeyBase64");
            var zkp = new ZKP(proofProvider);
            double userBalance = 1000.0;
            double requestedAmount = 500.0;
            var (proof, salt) = zkp.ProveBalance(userBalance, requestedAmount);

            string incorrectSalt = Guid.NewGuid().ToString();
            Assert.False(zkp.VerifyBalance(proof, requestedAmount, incorrectSalt, userBalance), "Proof should fail due to incorrect salt");
        }

        // TODO: Add more tests for:
        // ProveRange, VerifyRange, ProveTimestamp, VerifyTimestamp, ProveSetMembership, VerifySetMembership.
    }
}