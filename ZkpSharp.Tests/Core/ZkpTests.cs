using ZkpSharp.Core;
using ZkpSharp.Exceptions;
using ZkpSharp.Security;
using ZkpSharp.Interfaces;

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
            var dateOfBirth = DateTime.UtcNow.AddYears(-15);  // Age 15 (dynamically calculated)

            // Act
            var exception = Assert.Throws<InsufficientAgeException>(() => zkp.ProveAge(dateOfBirth));
            
            // Assert
            Assert.Contains("Insufficient age", exception.Message);
            Assert.Equal(18, exception.RequiredAge);
            Assert.True(exception.ActualAge < 18, "Actual age should be less than 18");
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

        #region Membership Tests

        [Fact]
        public void TestProveMembership_ValidMember_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var validValues = new[] { "gold", "silver", "bronze" };
            var value = "gold";

            var (proof, salt) = zkp.ProveMembership(value, validValues);

            Assert.True(zkp.VerifyMembership(proof, value, salt, validValues), "Membership proof should be valid");
        }

        [Fact]
        public void TestProveMembership_InvalidMember_ShouldThrow()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var validValues = new[] { "gold", "silver", "bronze" };
            var value = "platinum";

            var exception = Assert.Throws<ValueNotInSetException>(() => zkp.ProveMembership(value, validValues));
            Assert.Contains("platinum", exception.Message);
        }

        [Fact]
        public void TestVerifyMembership_WrongValue_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var validValues = new[] { "gold", "silver", "bronze" };
            var value = "gold";

            var (proof, salt) = zkp.ProveMembership(value, validValues);

            Assert.False(zkp.VerifyMembership(proof, "silver", salt, validValues), "Should fail for wrong value");
        }

        [Fact]
        public void TestVerifyMembership_WrongSalt_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var validValues = new[] { "gold", "silver", "bronze" };
            var value = "gold";

            var (proof, salt) = zkp.ProveMembership(value, validValues);

            Assert.False(zkp.VerifyMembership(proof, value, "wrongsalt", validValues), "Should fail for wrong salt");
        }

        [Fact]
        public void TestVerifyMembership_EmptySet_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var validValues = new[] { "gold" };
            var value = "gold";

            var (proof, salt) = zkp.ProveMembership(value, validValues);

            Assert.False(zkp.VerifyMembership(proof, value, salt, Array.Empty<string>()), "Should fail for empty set");
        }

        [Fact]
        public void TestProveMembership_EmptySet_ShouldThrow()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var validValues = Array.Empty<string>();

            Assert.Throws<ArgumentException>(() => zkp.ProveMembership("value", validValues));
        }

        [Fact]
        public void TestProveMembership_NullValue_ShouldThrow()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var validValues = new[] { "gold", "silver" };

            Assert.Throws<ArgumentException>(() => zkp.ProveMembership(null!, validValues));
        }

        #endregion

        #region Range Tests

        [Fact]
        public void TestProveRange_ValidValue_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = 50.0;
            double min = 0.0;
            double max = 100.0;

            var (proof, salt) = zkp.ProveRange(value, min, max);

            Assert.True(zkp.VerifyRange(proof, min, max, value, salt), "Range proof should be valid");
        }

        [Fact]
        public void TestProveRange_BoundaryMinValue_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = 0.0;
            double min = 0.0;
            double max = 100.0;

            var (proof, salt) = zkp.ProveRange(value, min, max);

            Assert.True(zkp.VerifyRange(proof, min, max, value, salt), "Boundary min value should pass");
        }

        [Fact]
        public void TestProveRange_BoundaryMaxValue_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = 100.0;
            double min = 0.0;
            double max = 100.0;

            var (proof, salt) = zkp.ProveRange(value, min, max);

            Assert.True(zkp.VerifyRange(proof, min, max, value, salt), "Boundary max value should pass");
        }

        [Fact]
        public void TestProveRange_BelowMin_ShouldThrow()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = -1.0;
            double min = 0.0;
            double max = 100.0;

            var exception = Assert.Throws<ValueOutOfRangeException>(() => zkp.ProveRange(value, min, max));
            Assert.Contains("-1", exception.Message);
        }

        [Fact]
        public void TestProveRange_AboveMax_ShouldThrow()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = 101.0;
            double min = 0.0;
            double max = 100.0;

            var exception = Assert.Throws<ValueOutOfRangeException>(() => zkp.ProveRange(value, min, max));
            Assert.Contains("101", exception.Message);
        }

        [Fact]
        public void TestVerifyRange_WrongValue_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = 50.0;
            double min = 0.0;
            double max = 100.0;

            var (proof, salt) = zkp.ProveRange(value, min, max);

            Assert.False(zkp.VerifyRange(proof, min, max, 60.0, salt), "Should fail for wrong value");
        }

        [Fact]
        public void TestVerifyRange_InvalidRange_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = 50.0;

            var (proof, salt) = zkp.ProveRange(value, 0.0, 100.0);

            Assert.False(zkp.VerifyRange(proof, 100.0, 0.0, value, salt), "Should fail for inverted range");
        }

        [Fact]
        public void TestVerifyRange_ValueOutsideNewRange_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = 50.0;

            var (proof, salt) = zkp.ProveRange(value, 0.0, 100.0);

            Assert.False(zkp.VerifyRange(proof, 60.0, 100.0, value, salt), "Should fail when value outside new range");
        }

        [Fact]
        public void TestProveRange_NegativeRange_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            double value = -50.0;
            double min = -100.0;
            double max = 0.0;

            var (proof, salt) = zkp.ProveRange(value, min, max);

            Assert.True(zkp.VerifyRange(proof, min, max, value, salt), "Negative range should work");
        }

        #endregion

        #region TimeCondition Tests

        [Fact]
        public void TestProveTimeCondition_FutureEvent_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2025, 6, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            var (proof, salt) = zkp.ProveTimeCondition(eventDate, conditionDate);

            Assert.True(zkp.VerifyTimeCondition(proof, eventDate, conditionDate, salt), "Time condition should pass");
        }

        [Fact]
        public void TestProveTimeCondition_SameDate_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var date = new DateTime(2025, 1, 1);

            var (proof, salt) = zkp.ProveTimeCondition(date, date);

            Assert.True(zkp.VerifyTimeCondition(proof, date, date, salt), "Same date should pass");
        }

        [Fact]
        public void TestProveTimeCondition_EventBeforeCondition_ShouldThrow()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2024, 1, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            var exception = Assert.Throws<ArgumentException>(() => zkp.ProveTimeCondition(eventDate, conditionDate));
            Assert.Contains("time condition", exception.Message);
        }

        [Fact]
        public void TestVerifyTimeCondition_WrongEventDate_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2025, 6, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            var (proof, salt) = zkp.ProveTimeCondition(eventDate, conditionDate);

            var wrongDate = new DateTime(2025, 7, 1);
            Assert.False(zkp.VerifyTimeCondition(proof, wrongDate, conditionDate, salt), "Should fail for wrong date");
        }

        [Fact]
        public void TestVerifyTimeCondition_WrongConditionDate_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2025, 6, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            var (proof, salt) = zkp.ProveTimeCondition(eventDate, conditionDate);

            var earlierCondition = new DateTime(2024, 1, 1);
            Assert.True(zkp.VerifyTimeCondition(proof, eventDate, earlierCondition, salt), "Earlier condition should still pass");
        }

        [Fact]
        public void TestVerifyTimeCondition_LaterConditionDate_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2025, 6, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            var (proof, salt) = zkp.ProveTimeCondition(eventDate, conditionDate);

            var laterCondition = new DateTime(2025, 12, 1);
            Assert.False(zkp.VerifyTimeCondition(proof, eventDate, laterCondition, salt), "Event before later condition should fail");
        }

        [Fact]
        public void TestVerifyTimeCondition_WrongSalt_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2025, 6, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            var (proof, salt) = zkp.ProveTimeCondition(eventDate, conditionDate);

            Assert.False(zkp.VerifyTimeCondition(proof, eventDate, conditionDate, "wrongsalt"), "Should fail for wrong salt");
        }

        [Fact]
        public void TestVerifyTimeCondition_NullProof_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2025, 6, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            Assert.False(zkp.VerifyTimeCondition(null!, eventDate, conditionDate, "salt"), "Null proof should fail");
        }

        [Fact]
        public void TestVerifyTimeCondition_EmptySalt_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var eventDate = new DateTime(2025, 6, 1);
            var conditionDate = new DateTime(2025, 1, 1);

            var (proof, salt) = zkp.ProveTimeCondition(eventDate, conditionDate);

            Assert.False(zkp.VerifyTimeCondition(proof, eventDate, conditionDate, ""), "Empty salt should fail");
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void TestVerifyAge_NullProof_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var dateOfBirth = new DateTime(2000, 1, 1);

            Assert.False(zkp.VerifyAge(null!, dateOfBirth, "salt"), "Null proof should fail");
        }

        [Fact]
        public void TestVerifyAge_EmptyProof_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var dateOfBirth = new DateTime(2000, 1, 1);

            Assert.False(zkp.VerifyAge("", dateOfBirth, "salt"), "Empty proof should fail");
        }

        [Fact]
        public void TestVerifyBalance_NegativeBalance_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);

            Assert.False(zkp.VerifyBalance("proof", 100.0, "salt", -50.0), "Negative balance should fail");
        }

        [Fact]
        public void TestVerifyBalance_NegativeRequestedAmount_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);

            Assert.False(zkp.VerifyBalance("proof", -100.0, "salt", 500.0), "Negative requested amount should fail");
        }

        [Fact]
        public void TestProveBalance_ZeroBalance_ZeroRequest_ShouldPass()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);

            var (proof, salt) = zkp.ProveBalance(0.0, 0.0);

            Assert.True(zkp.VerifyBalance(proof, 0.0, salt, 0.0), "Zero balance and request should pass");
        }

        [Fact]
        public void TestProveAge_FutureDateOfBirth_ShouldThrow()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var futureDate = DateTime.UtcNow.AddYears(1);

            Assert.Throws<ArgumentException>(() => zkp.ProveAge(futureDate));
        }

        [Fact]
        public void TestVerifyAge_FutureDateOfBirth_ShouldFail()
        {
            var proofProvider = new ProofProvider(_hmacKey);
            var zkp = new Zkp(proofProvider);
            var futureDate = DateTime.UtcNow.AddYears(1);

            Assert.False(zkp.VerifyAge("someproof", futureDate, "salt"), "Future birth date should fail");
        }

        #endregion
    }
}
