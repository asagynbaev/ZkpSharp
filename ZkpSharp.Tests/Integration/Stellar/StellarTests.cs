using ZkpSharp.Core;
using ZkpSharp.Security;
using ZkpSharp.Integration.Stellar;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;

namespace ZkpSharp.Tests.Integration.Stellar
{
    /// <summary>
    /// Integration tests for Stellar blockchain ZKP verification.
    /// These tests demonstrate how to use ZkpSharp with Soroban smart contracts.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: To run these tests, you need to:
    /// 1. Deploy the ZKP verifier contract to Stellar testnet
    /// 2. Set the ZKP_HMAC_KEY environment variable
    /// 3. Set the ZKP_CONTRACT_ID environment variable with your deployed contract ID
    /// 4. Ensure you have a funded Stellar testnet account
    /// 
    /// These tests are marked with [Fact(Skip = ...)] to prevent them from running
    /// without proper configuration. Remove the Skip parameter when ready to test.
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

        private string GetContractId()
        {
            // Get contract ID from environment variable
            var contractId = Environment.GetEnvironmentVariable("ZKP_CONTRACT_ID");
            
            if (string.IsNullOrEmpty(contractId))
            {
                throw new InvalidOperationException(
                    "Contract ID not configured. Set ZKP_CONTRACT_ID environment variable with your deployed contract ID.");
            }
            
            return contractId;
        }

        [Fact(Skip = "Requires deployed Soroban contract and configuration")]
        public async Task VerifyProof_ValidProof_ShouldPass()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var contractId = GetContractId();
            var zkp = new Zkp(new ProofProvider(hmacKey));
            var blockchain = new StellarBlockchain(TestServerUrl, TestSorobanRpcUrl);

            // Generate a proof
            var testData = "test-value-123";
            var (proof, salt) = zkp.ProveMembership(testData, new[] { testData, "other-value" });

            // Act
            var result = await blockchain.VerifyProof(contractId, proof, salt, testData);

            // Assert
            Assert.True(result, "Valid proof should be verified successfully.");
        }

        [Fact(Skip = "Requires deployed Soroban contract and configuration")]
        public async Task VerifyProof_InvalidProof_ShouldFail()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var contractId = GetContractId();
            var zkp = new Zkp(new ProofProvider(hmacKey));
            var blockchain = new StellarBlockchain(TestServerUrl, TestSorobanRpcUrl);

            // Generate a proof for one value
            var testData = "test-value-123";
            var (proof, salt) = zkp.ProveMembership(testData, new[] { testData });

            // Act - try to verify with different data (should fail)
            var differentData = "different-value-456";
            var result = await blockchain.VerifyProof(contractId, proof, salt, differentData);

            // Assert
            Assert.False(result, "Invalid proof should fail verification.");
        }

        [Fact(Skip = "Requires deployed Soroban contract and configuration")]
        public async Task VerifyBalanceProof_ValidBalance_ShouldPass()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var contractId = GetContractId();
            var zkp = new Zkp(new ProofProvider(hmacKey));
            var blockchain = new StellarBlockchain(TestServerUrl, TestSorobanRpcUrl);

            // Generate a balance proof
            double balance = 1000.0;
            double requestedAmount = 500.0;
            var (proof, salt) = zkp.ProveBalance(balance, requestedAmount);

            // Act
            var result = await blockchain.VerifyBalanceProof(
                contractId,
                proof,
                balance,
                requestedAmount,
                salt);

            // Assert
            Assert.True(result, "Valid balance proof should be verified successfully.");
        }

        [Fact(Skip = "Requires deployed Soroban contract and configuration")]
        public async Task VerifyBalanceProof_InsufficientBalance_ShouldFail()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var contractId = GetContractId();
            var zkp = new Zkp(new ProofProvider(hmacKey));
            var blockchain = new StellarBlockchain(TestServerUrl, TestSorobanRpcUrl);

            // Generate a balance proof with insufficient balance
            double balance = 500.0;
            double requestedAmount = 1000.0; // More than balance

            // Act & Assert
            await Assert.ThrowsAsync<ZkpSharp.Exceptions.InsufficientBalanceException>(
                async () => zkp.ProveBalance(balance, requestedAmount));
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

        [Fact(Skip = "SorobanTransactionBuilder requires complex Soroban XDR implementation")]
        public void SorobanTransactionBuilder_BuildVerifyProofTransaction_ShouldCreateValidXdr()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var keypair = KeyPair.Random();
            var sourceAccount = new StellarDotnetSdk.Accounts.Account(keypair.AccountId, 0);
            var network = Network.Test();
            var contractId = "CAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHK3M"; // Example contract ID

            // NOTE: SorobanTransactionBuilder is not implemented yet because it requires
            // complex Soroban XDR encoding that is not fully supported in current .NET SDK.
            // Use hybrid approach with Stellar JS SDK for on-chain verification.
            
            // Act & Assert - skipped
            Assert.True(true);
        }

        /// <summary>
        /// Example of end-to-end workflow: Generate proof off-chain, verify on-chain.
        /// </summary>
        [Fact(Skip = "Requires deployed Soroban contract and configuration")]
        public async Task EndToEndWorkflow_ProofGeneration_AndVerification()
        {
            // Arrange
            var hmacKey = GetHmacKey();
            var contractId = GetContractId();
            var zkp = new Zkp(new ProofProvider(hmacKey));
            var blockchain = new StellarBlockchain(TestServerUrl, TestSorobanRpcUrl);

            // Step 1: User proves they are over 18 (off-chain)
            var dateOfBirth = new DateTime(1990, 1, 1);
            var (ageProof, ageSalt) = zkp.ProveAge(dateOfBirth);

            Console.WriteLine($"Generated age proof: {ageProof}");
            Console.WriteLine($"Salt: {ageSalt}");

            // Step 2: Verify the proof locally (off-chain)
            var isValidOffChain = zkp.VerifyAge(ageProof, dateOfBirth, ageSalt);
            Assert.True(isValidOffChain, "Off-chain verification should pass");

            // Step 3: Verify the proof on Stellar blockchain (on-chain)
            var dobString = dateOfBirth.ToString("yyyy-MM-dd");
            var isValidOnChain = await blockchain.VerifyProof(contractId, ageProof, ageSalt, dobString);

            // Assert
            Assert.True(isValidOnChain, "On-chain verification should pass");
            Console.WriteLine("✅ End-to-end workflow completed successfully!");
        }
    }
}