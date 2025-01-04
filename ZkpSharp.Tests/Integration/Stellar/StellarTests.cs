using ZkpSharp.Core;
using ZkpSharp.Security;

namespace ZkpSharp.Tests.Integration.Stellar
{
    public class StellarTests
    {
        // Replace with your HMAC key
        private string _hmacKey = "V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc=";

        // Replace with your Horizon URL
        private const string ServerUrl = "https://horizon-testnet.stellar.org";
        
        // Replace with your deployed Soroban contract ID
        private const string ContractId = "abcd1234";

        [Fact]
        public async Task VerifyProof_ValidProof_ShouldPass()
        {
            // Arrange
            // var sorobanClient = new SorobanClient(ServerUrl);
            // string proof = "exampleProof";
            // string salt = "exampleSalt";
            // string value = "exampleValue";

            // Act
            // bool result = await sorobanClient.VerifyProof(ContractId, proof, salt, value);

            // Assert
            // Assert.True(result, "Proof verification should succeed.");
        }

        [Fact]
        public async Task VerifyProof_InvalidProof_ShouldFail()
        {
            // Arrange
            // var sorobanClient = new SorobanClient(ServerUrl);
            // string proof = "invalidProof";
            // string salt = "exampleSalt";
            // string value = "exampleValue";

            // Act
            // bool result = await sorobanClient.VerifyProof(ContractId, proof, salt, value);

            // Assert
            // Assert.False(result, "Proof verification should fail.");
        }

        [Fact]
        public async Task GenerateAndVerifyBalanceProof_ShouldPass()
        {
            // var zkp = new ZKP(new ProofProvider(_hmacKey));
            // var balanceChecker = new StellarBalanceChecker("https://horizon-testnet.stellar.org");

            // string accountId = "GXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
            // double minRequiredBalance = 10.0;

            // Получение доказательства
            // var (proof, salt) = balanceChecker.GenerateBalanceProof(accountId, minRequiredBalance, zkp);

            // // Проверка доказательства
            // double currentBalance = await balanceChecker.GetAccountBalance(accountId);
            // bool isValid = zkp.VerifyBalance(proof, minRequiredBalance, salt, currentBalance);

            // Assert.True(isValid, "Balance proof verification failed.");
        }
    }
}