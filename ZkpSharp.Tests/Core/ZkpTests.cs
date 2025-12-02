using ZkpSharp.Core;
using ZkpSharp.Exceptions;
using ZkpSharp.Security;

namespace ZkpSharp.Tests.Core
{
    public class ZkpTests
    {
        private string _hmacKey = "V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc=";

        [Fact]
        public void TestProveAndVerifyAge_ValidAge_ShouldPass()
        {
            // Arrange
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var dateOfBirth = new DateTime(2000, 1, 1);  // Age 25
            var (proof, salt) = zkp.ProveAge(dateOfBirth);

            // Assert
            Assert.True(zkp.VerifyAge(proof, dateOfBirth, salt), "Proof should be valid");
        }

        [Fact]
        public void TestProveAndVerifyAge_InsufficientAge_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var dateOfBirth = new DateTime(2010, 1, 1);  // Age 15

            // Act
            var exception = Assert.Throws<InsufficientAgeException>(() => zkp.ProveAge(dateOfBirth));
            
            // Assert
            Assert.Contains("Insufficient age", exception.Message);
            Assert.Equal(18, exception.RequiredAge);
            Assert.Equal(15, exception.ActualAge);
        }

        [Fact]
        public void TestProveAndVerifyBalance_ValidBalance_ShouldPass()
        {
            // Arrange
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double userBalance = 1000.0;
            double requestedAmount = 500.0;

            // Act
            var (proof, salt) = zkp.ProveBalance(userBalance, requestedAmount);

            // Assert
            Assert.True(
                zkp.VerifyBalance(proof, requestedAmount, salt, userBalance), 
                "Proof should be valid"
            );
        }

        [Fact]
        public void TestProveAndVerifyBalance_InsufficientBalance_ShouldFail()
        {
            // Arrange
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double userBalance = 300.0;
            double requestedAmount = 500.0;

            // Act
            var exception = Assert.Throws<InsufficientBalanceException>(
                () => zkp.ProveBalance(userBalance, requestedAmount)
            );

            // Assert
            Assert.Contains("Insufficient balance", exception.Message);
            Assert.Equal(300.0, exception.Balance);
            Assert.Equal(500.0, exception.RequestedAmount);
        }

        [Fact]
        public void TestBalanceVerificationWithSalt_ValidBalance_ShouldPass()
        {
            // Arrange
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double userBalance = 1000.0;
            double requestedAmount = 500.0;

            // Act
            var (proof, salt) = zkp.ProveBalance(userBalance, requestedAmount);

            // Assert
            Assert.True(
                zkp.VerifyBalance(proof, requestedAmount, salt, userBalance), 
                "Proof should be valid"
            );
        }

        [Fact]
        public void TestBalanceVerificationWithSalt_InsufficientBalance_ShouldFail()
        {
            // Arrange
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double userBalance = 100.0;
            double requestedAmount = 150.0;

            // Act
            var exception = Assert.Throws<InsufficientBalanceException>(
                () => zkp.ProveBalance(userBalance, requestedAmount)
            );

            // Assert
            Assert.Contains("Insufficient balance", exception.Message);
            Assert.Equal(100.0, exception.Balance);
            Assert.Equal(150.0, exception.RequestedAmount);
        }

        [Fact]
        public void TestProveAndVerifyAge_InvalidSalt_ShouldFail()
        {
            // Arrange
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var dateOfBirth = new DateTime(2000, 1, 1);  // Возраст 25 лет
            var (proof, salt) = zkp.ProveAge(dateOfBirth);

            // Act
            string incorrectSalt = Guid.NewGuid().ToString();

            // Assert
            Assert.False(
                zkp.VerifyAge(proof, dateOfBirth, incorrectSalt), 
                "Proof should fail due to incorrect salt"
            );
        }

        [Fact]
        public void TestBalanceVerificationWithSalt_InvalidSalt_ShouldFail()
        {
            // Arrange
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double userBalance = 1000.0;
            double requestedAmount = 500.0;
            var (proof, salt) = zkp.ProveBalance(userBalance, requestedAmount);

            // Act
            string incorrectSalt = Guid.NewGuid().ToString();

            // Assert
            Assert.False(
                zkp.VerifyBalance(proof, requestedAmount, incorrectSalt, userBalance), 
                "Proof should fail due to incorrect salt"
            );
        }

        // TODO: Add more tests for:
        // ProveRange, VerifyRange, ProveTimestamp, VerifyTimestamp, 
        // ProveSetMembership, VerifySetMembership.
    }
}