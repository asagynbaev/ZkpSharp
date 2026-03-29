using ZkpSharp.Core;
using ZkpSharp.Security;
using ZkpSharp.Integration.Stellar;
using StellarDotnetSdk;

namespace ZkpSharp.Tests.Integration.Stellar
{
    /// <summary>
    /// Integration tests for Stellar blockchain ZKP verification.
    /// These tests demonstrate how to use ZkpSharp with Soroban smart contracts.
    /// </summary>
    /// <remarks>
    /// On-chain contract checks live in <see cref="StellarTestnetSmokeTests"/> (SkippableFact, requires
    /// <c>ZKP_CONTRACT_ID</c>, <c>ZKP_SOURCE_ACCOUNT</c>, <c>ZKP_HMAC_KEY</c>).
    /// </remarks>
    public class StellarTests
    {
        // Test configuration - replace with your values or use environment variables
        private const string TestServerUrl = "https://horizon-testnet.stellar.org";
        private const string TestSorobanRpcUrl = "https://soroban-testnet.stellar.org";
        
        private string GetHmacKey()
        {
            // Try to get from environment variable
            var hmacKey = Environment.GetEnvironmentVariable("ZKP_HMAC_KEY");
            
            // Fallback to test key (DO NOT use in production!)
            return hmacKey ?? "V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc=";
        }

        [Fact]
        public void VerifyBalanceProof_InsufficientBalance_ShouldFail()
        {
            var hmacKey = GetHmacKey();
            var zkp = new Zkp(new ProofProvider(hmacKey));

            const double balance = 500.0;
            const double requestedAmount = 1000.0;

            Assert.Throws<ZkpSharp.Exceptions.InsufficientBalanceException>(
                () => zkp.ProveBalance(balance, requestedAmount));
        }

        [Fact]
        public async Task GetAccountBalance_ValidAccount_ShouldReturnBalance()
        {
            // Arrange
            var blockchain = new StellarBlockchain(TestServerUrl, TestSorobanRpcUrl);
            
            // Use a known testnet account with balance
            // This is a public testnet faucet account (for testing only)
            var testAccountId = "GAIH3ULLFQ4DGSECF2AR555KZ4KNDGEKN4AFI4SU2M7B43MGK3QJZNSR";

            // Act
            var balance = await blockchain.GetAccountBalance(testAccountId);

            // Assert
            Assert.True(balance >= 0, "Balance should be non-negative.");
        }

        [Fact]
        public async Task GetAccountBalance_InvalidAccount_ShouldThrow()
        {
            // Arrange
            var blockchain = new StellarBlockchain(TestServerUrl, TestSorobanRpcUrl);
            var invalidAccountId = "GINVALIDACCOUNTID";

            // Act & Assert - StellarDotnetSdk throws FormatException for invalid account IDs
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await blockchain.GetAccountBalance(invalidAccountId));
        }

        [Fact]
        public void SorobanHelper_EncodeDecodeBytes_ShouldRoundTrip()
        {
            // Arrange
            var testBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Act
            var scVal = SorobanHelper.EncodeBytesAsScVal(testBytes);
            var decodedBytes = SorobanHelper.DecodeBytesFromScVal(scVal);

            // Assert
            Assert.Equal(testBytes, decodedBytes);
        }

        [Fact]
        public void SorobanHelper_EncodeDecodeString_ShouldRoundTrip()
        {
            // Arrange
            var testString = "Hello, Soroban!";

            // Act
            var scVal = SorobanHelper.EncodeStringAsScVal(testString);
            var decodedString = SorobanHelper.DecodeStringFromScVal(scVal);

            // Assert
            Assert.Equal(testString, decodedString);
        }

        [Fact]
        public void SorobanHelper_EncodeBool_ShouldCreateValidScVal()
        {
            // Arrange & Act
            var trueScVal = SorobanHelper.EncodeBoolAsScVal(true);
            var falseScVal = SorobanHelper.EncodeBoolAsScVal(false);

            // Assert
            Assert.True(SorobanHelper.DecodeBoolFromScVal(trueScVal));
            Assert.False(SorobanHelper.DecodeBoolFromScVal(falseScVal));
        }

        [Fact]
        public void SorobanHelper_ConvertProofToBytes_ValidBase64_ShouldSucceed()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var proofProvider = new ProofProvider(hmacKey);
            var proof = proofProvider.GenerateHMAC("test-data");

            // Act
            var proofBytes = SorobanHelper.ConvertProofToBytes(proof);

            // Assert
            Assert.Equal(32, proofBytes.Length); // HMAC-SHA256 is 32 bytes
        }

        [Fact]
        public void SorobanHelper_ConvertProofToBytes_InvalidLength_ShouldThrow()
        {
            // Arrange
            var shortProof = Convert.ToBase64String(new byte[16]); // Only 16 bytes

            // Act & Assert
            Assert.Throws<ArgumentException>(() => SorobanHelper.ConvertProofToBytes(shortProof));
        }

        [Fact]
        public void SorobanHelper_ConvertSaltToBytes_ValidBase64_ShouldSucceed()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var proofProvider = new ProofProvider(hmacKey);
            var salt = proofProvider.GenerateSalt();

            // Act
            var saltBytes = SorobanHelper.ConvertSaltToBytes(salt);

            // Assert
            Assert.True(saltBytes.Length >= 16, "Salt should be at least 16 bytes");
        }

        #region SorobanTransactionBuilder Tests

        [Fact]
        public void SorobanTransactionBuilder_BuildVerifyProofTransaction_ShouldCreateXdr()
        {
            // Arrange
            var network = Network.Test();
            var builder = new SorobanTransactionBuilder(network);
            var hmacKey = GetHmacKey();
            var proofProvider = new ProofProvider(hmacKey);
            var proof = proofProvider.GenerateHMAC("test-data");
            var salt = proofProvider.GenerateSalt();
            var contractId = "CAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHK3M";

            // Act
            var xdr = builder.BuildVerifyProofTransaction(
                contractId: contractId,
                proof: proof,
                data: "test-data",
                salt: salt,
                hmacKey: hmacKey
            );

            // Assert
            Assert.False(string.IsNullOrEmpty(xdr), "XDR should not be empty");
            Assert.True(xdr.Length > 0, "XDR should have content");
            
            var decoded = Convert.FromBase64String(xdr);
            Assert.True(decoded.Length > 0, "XDR should be valid Base64");
        }

        [Fact]
        public void SorobanTransactionBuilder_BuildVerifyBalanceProofTransaction_ShouldCreateXdr()
        {
            // Arrange
            var network = Network.Test();
            var builder = new SorobanTransactionBuilder(network);
            var hmacKey = GetHmacKey();
            var proofProvider = new ProofProvider(hmacKey);
            var proof = proofProvider.GenerateHMAC("1000.0");
            var salt = proofProvider.GenerateSalt();
            var contractId = "CAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHK3M";

            // Act
            var xdr = builder.BuildVerifyBalanceProofTransaction(
                contractId: contractId,
                proof: proof,
                balanceData: "1000.0",
                requiredAmountData: "500.0",
                salt: salt,
                hmacKey: hmacKey
            );

            // Assert
            Assert.False(string.IsNullOrEmpty(xdr), "XDR should not be empty");
            var decoded = Convert.FromBase64String(xdr);
            Assert.True(decoded.Length > 0, "XDR should be valid Base64");
        }

        [Fact]
        public void SorobanTransactionBuilder_BuildVerifyZkRangeProofTransaction_ShouldCreateXdr()
        {
            // Arrange
            var network = Network.Test();
            var builder = new SorobanTransactionBuilder(network);
            var proofBytes = new byte[64];
            proofBytes[0] = 0x42;
            proofBytes[1] = 0x50;
            var commitmentBytes = new byte[33];
            commitmentBytes[0] = 0x02;
            var contractId = "CAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHK3M";

            // Act
            var xdr = builder.BuildVerifyZkRangeProofTransaction(
                contractId: contractId,
                proof: Convert.ToBase64String(proofBytes),
                commitment: Convert.ToBase64String(commitmentBytes),
                min: 0,
                max: 100
            );

            // Assert
            Assert.False(string.IsNullOrEmpty(xdr), "XDR should not be empty");
            var decoded = Convert.FromBase64String(xdr);
            Assert.True(decoded.Length > 0, "XDR should be valid Base64");
        }

        [Fact]
        public void SorobanTransactionBuilder_NullContractId_ShouldThrow()
        {
            // Arrange
            var network = Network.Test();
            var builder = new SorobanTransactionBuilder(network);
            var hmacKey = GetHmacKey();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.BuildVerifyProofTransaction(
                contractId: null!,
                proof: "proof",
                data: "data",
                salt: "salt",
                hmacKey: hmacKey
            ));
        }

        [Fact]
        public void SorobanTransactionBuilder_EmptyProof_ShouldThrow()
        {
            // Arrange
            var network = Network.Test();
            var builder = new SorobanTransactionBuilder(network);
            var hmacKey = GetHmacKey();
            var contractId = "CAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHK3M";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.BuildVerifyProofTransaction(
                contractId: contractId,
                proof: "",
                data: "data",
                salt: "salt",
                hmacKey: hmacKey
            ));
        }

        #endregion

        #region BulletproofsProvider Tests

        [Fact]
        public void BulletproofsProvider_ProveRange_ValidValue_ShouldSucceed()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            long value = 50;
            long min = 0;
            long max = 100;

            // Act
            var (proof, commitment) = provider.ProveRange(value, min, max);

            // Assert
            Assert.NotNull(proof);
            Assert.NotNull(commitment);
            Assert.True(proof.Length > 0, "Proof should have content");
            Assert.Equal(33, commitment.Length);
        }

        [Fact]
        public void BulletproofsProvider_ProveRange_ValueOutOfRange_ShouldThrow()
        {
            // Arrange
            var provider = new BulletproofsProvider();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.ProveRange(101, 0, 100));
        }

        [Fact]
        public void BulletproofsProvider_VerifyRange_ValidProof_ShouldSucceed()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            var (proof, commitment) = provider.ProveRange(50, 0, 100);

            // Act
            var isValid = provider.VerifyRange(proof, commitment, 0, 100);

            // Assert
            Assert.True(isValid, "Valid proof should verify");
        }

        [Fact]
        public void BulletproofsProvider_ProveAge_ValidAge_ShouldSucceed()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            var birthDate = new DateTime(1990, 1, 1);
            int minAge = 18;

            // Act
            var (proof, commitment) = provider.ProveAge(birthDate, minAge);

            // Assert
            Assert.NotNull(proof);
            Assert.NotNull(commitment);
            Assert.True(proof.Length > 0);
        }

        [Fact]
        public void BulletproofsProvider_ProveAge_TooYoung_ShouldThrow()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            var birthDate = DateTime.UtcNow.AddYears(-17);
            int minAge = 18;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.ProveAge(birthDate, minAge));
        }

        [Fact]
        public void BulletproofsProvider_ProveBalance_ValidBalance_ShouldSucceed()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            long balance = 1000;
            long requiredAmount = 500;

            // Act
            var (proof, commitment) = provider.ProveBalance(balance, requiredAmount);

            // Assert
            Assert.NotNull(proof);
            Assert.NotNull(commitment);
            Assert.True(proof.Length > 0);
        }

        [Fact]
        public void BulletproofsProvider_ProveBalance_InsufficientBalance_ShouldThrow()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            long balance = 500;
            long requiredAmount = 1000;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.ProveBalance(balance, requiredAmount));
        }

        [Fact]
        public void BulletproofsProvider_SerializeDeserialize_ShouldRoundTrip()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            var (proof, commitment) = provider.ProveRange(50, 0, 100);

            // Act
            var serialized = provider.SerializeProof(proof, commitment);
            var (deserializedProof, deserializedCommitment) = provider.DeserializeProof(serialized);

            // Assert
            Assert.Equal(proof, deserializedProof);
            Assert.Equal(commitment, deserializedCommitment);
        }

        [Fact]
        public void BulletproofsProvider_DeserializeProof_EmptyString_ShouldThrow()
        {
            // Arrange
            var provider = new BulletproofsProvider();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.DeserializeProof(""));
        }

        [Fact]
        public void BulletproofsProvider_VerifyRange_NullProof_ShouldReturnFalse()
        {
            // Arrange
            var provider = new BulletproofsProvider();

            // Act
            var isValid = provider.VerifyRange(null!, new byte[33], 0, 100);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void BulletproofsProvider_VerifyRange_InvalidCommitmentLength_ShouldReturnFalse()
        {
            // Arrange
            var provider = new BulletproofsProvider();
            var proof = new byte[] { 0x42, 0x50 };

            // Act
            var isValid = provider.VerifyRange(proof, new byte[10], 0, 100);

            // Assert
            Assert.False(isValid);
        }

        #endregion
    }
}