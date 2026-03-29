using System.Globalization;
using System.Text;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Responses;
using ZkpSharp.Interfaces;

namespace ZkpSharp.Integration.Stellar
{
    /// <summary>
    /// Production-ready implementation of IBlockchain for Stellar network integration.
    /// Provides full support for Soroban smart contract interactions and ZKP verification.
    /// </summary>
    public class StellarBlockchain : IBlockchain
    {
        private readonly string _serverUrl;
        private readonly string _sorobanRpcUrl;
        private readonly Network _network;
        private readonly string? _hmacKey;
        private SorobanRpcClient? _rpcClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="StellarBlockchain"/> class.
        /// </summary>
        /// <param name="serverUrl">The Horizon API server URL.</param>
        /// <param name="sorobanRpcUrl">Optional Soroban RPC URL. If not provided, will be inferred from server URL.</param>
        /// <param name="network">Optional network configuration. If not provided, will be inferred from server URL.</param>
        /// <param name="hmacKey">Optional HMAC key for proof verification (Base64 encoded, 32 bytes).</param>
        public StellarBlockchain(string serverUrl, string? sorobanRpcUrl = null, Network? network = null, string? hmacKey = null)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                throw new ArgumentException("Server URL cannot be null or empty.", nameof(serverUrl));
            }
            _serverUrl = serverUrl;
            
            // Default Soroban RPC URL based on server URL
            _sorobanRpcUrl = sorobanRpcUrl ?? GetDefaultSorobanRpcUrl(serverUrl);
            
            // Default network based on server URL
            _network = network ?? GetDefaultNetwork(serverUrl);
            
            // Store HMAC key for verification
            _hmacKey = hmacKey;
        }

        private string GetDefaultSorobanRpcUrl(string horizonUrl)
        {
            if (horizonUrl.Contains("testnet"))
            {
                return "https://soroban-testnet.stellar.org";
            }
            else if (horizonUrl.Contains("mainnet") || !horizonUrl.Contains("test"))
            {
                return "https://soroban-rpc.mainnet.stellar.org";
            }
            else
            {
                return "https://soroban-testnet.stellar.org";
            }
        }

        private Network GetDefaultNetwork(string horizonUrl)
        {
            if (horizonUrl.Contains("testnet"))
            {
                return Network.Test();
            }
            else if (horizonUrl.Contains("mainnet") || !horizonUrl.Contains("test"))
            {
                return Network.Public();
            }
            else
            {
                return Network.Test();
            }
        }

        private SorobanRpcClient GetRpcClient()
        {
            return _rpcClient ??= new SorobanRpcClient(_sorobanRpcUrl, _serverUrl);
        }

        /// <summary>
        /// Resolves the account used as the transaction source for Soroban <c>simulateTransaction</c>.
        /// </summary>
        /// <remarks>
        /// Set environment variable <c>ZKP_SOURCE_ACCOUNT</c> to a funded account (G...) on the same network as <see cref="_serverUrl"/>.
        /// Alternatively, call <see cref="VerifyProofWithSourceAccount"/> (or other <c>*WithSourceAccount</c> methods) with an explicit id.
        /// </remarks>
        private static string GetSimulationSourceAccountIdOrThrow()
        {
            var id = Environment.GetEnvironmentVariable("ZKP_SOURCE_ACCOUNT");
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException(
                    "Soroban RPC simulation requires a funded source account. " +
                    "Set environment variable ZKP_SOURCE_ACCOUNT to a G... account id on this network, " +
                    "or use VerifyProofWithSourceAccount / VerifyBalanceProofWithSourceAccount / VerifyZk*WithSourceAccount.");
            }

            return id.Trim();
        }

        /// <summary>
        /// Verifies a zero-knowledge proof on the blockchain using a Soroban smart contract.
        /// </summary>
        /// <param name="contractId">The smart contract address or ID.</param>
        /// <param name="proof">The proof to verify (Base64 encoded HMAC-SHA256, 32 bytes).</param>
        /// <param name="salt">The salt used to generate the proof (Base64 encoded, min 16 bytes).</param>
        /// <param name="value">The value that was proven.</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        /// <remarks>
        /// Builds a full transaction envelope and simulates it via RPC (does not submit).
        /// Requires <c>ZKP_SOURCE_ACCOUNT</c> unless you use <see cref="VerifyProofWithSourceAccount"/>.
        /// </remarks>
        public Task<bool> VerifyProof(string contractId, string proof, string salt, string value)
        {
            var source = GetSimulationSourceAccountIdOrThrow();
            return VerifyProofWithSourceAccount(source, contractId, proof, salt, value);
        }

        /// <summary>
        /// Verifies a zero-knowledge proof on the blockchain with a specific source account.
        /// </summary>
        /// <param name="sourceAccountId">The source account ID for the transaction.</param>
        /// <param name="contractId">The smart contract address or ID.</param>
        /// <param name="proof">The proof to verify (Base64 encoded HMAC-SHA256, 32 bytes).</param>
        /// <param name="salt">The salt used to generate the proof (Base64 encoded, min 16 bytes).</param>
        /// <param name="value">The value that was proven.</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        public async Task<bool> VerifyProofWithSourceAccount(
            string sourceAccountId,
            string contractId, 
            string proof, 
            string salt, 
            string value)
        {
            if (string.IsNullOrEmpty(sourceAccountId))
            {
                throw new ArgumentException("Source account ID cannot be null or empty.", nameof(sourceAccountId));
            }

            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty.", nameof(proof));
            }

            if (string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException("Salt cannot be null or empty.", nameof(salt));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            }

            var hmacKey = GetHmacKeyForVerification();

            // Get source account from Horizon
            Server server = new(_serverUrl);
            AccountResponse sourceAccount = await server.Accounts.Account(sourceAccountId);

            var transactionBuilder = new SorobanTransactionBuilder(_network);
            var transactionXdr = transactionBuilder.BuildVerifyProofTransactionWithAccount(
                sourceAccount: sourceAccount,
                contractId: contractId,
                proof: proof,
                data: value,
                salt: salt,
                hmacKey: hmacKey
            );

            var rpcClient = GetRpcClient();
            return await rpcClient.InvokeContractWithTransactionXdrAsync(transactionXdr);
        }

        /// <summary>
        /// Verifies a balance proof on the blockchain.
        /// </summary>
        /// <param name="contractId">The smart contract address or ID.</param>
        /// <param name="proof">The proof to verify (Base64 encoded).</param>
        /// <param name="balance">The balance value.</param>
        /// <param name="requiredAmount">The required amount.</param>
        /// <param name="salt">The salt used to generate the proof (Base64 encoded).</param>
        /// <returns>True if the proof is valid and balance is sufficient, false otherwise.</returns>
        /// <remarks>
        /// Requires <c>ZKP_SOURCE_ACCOUNT</c> unless you use <see cref="VerifyBalanceProofWithSourceAccount"/>.
        /// </remarks>
        public Task<bool> VerifyBalanceProof(
            string contractId,
            string proof,
            double balance,
            double requiredAmount,
            string salt)
        {
            var source = GetSimulationSourceAccountIdOrThrow();
            return VerifyBalanceProofWithSourceAccount(source, contractId, proof, balance, requiredAmount, salt);
        }

        /// <summary>
        /// Verifies a balance proof on the blockchain with a specific source account.
        /// </summary>
        /// <param name="sourceAccountId">The source account ID for the transaction.</param>
        /// <param name="contractId">The smart contract address or ID.</param>
        /// <param name="proof">The proof to verify (Base64 encoded).</param>
        /// <param name="balance">The balance value.</param>
        /// <param name="requiredAmount">The required amount.</param>
        /// <param name="salt">The salt used to generate the proof (Base64 encoded).</param>
        /// <returns>True if the proof is valid and balance is sufficient, false otherwise.</returns>
        public async Task<bool> VerifyBalanceProofWithSourceAccount(
            string sourceAccountId,
            string contractId,
            string proof,
            double balance,
            double requiredAmount,
            string salt)
        {
            if (string.IsNullOrEmpty(sourceAccountId))
            {
                throw new ArgumentException("Source account ID cannot be null or empty.", nameof(sourceAccountId));
            }

            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty.", nameof(proof));
            }

            if (string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException("Salt cannot be null or empty.", nameof(salt));
            }

            var hmacKey = GetHmacKeyForVerification();
            var balanceStr = balance.ToString(CultureInfo.InvariantCulture);
            var requiredAmountStr = requiredAmount.ToString(CultureInfo.InvariantCulture);

            // Get source account from Horizon
            Server server = new(_serverUrl);
            AccountResponse sourceAccount = await server.Accounts.Account(sourceAccountId);

            var transactionBuilder = new SorobanTransactionBuilder(_network);
            var transactionXdr = transactionBuilder.BuildVerifyBalanceProofTransactionWithAccount(
                sourceAccount: sourceAccount,
                contractId: contractId,
                proof: proof,
                balanceData: balanceStr,
                requiredAmountData: requiredAmountStr,
                salt: salt,
                hmacKey: hmacKey
            );

            var rpcClient = GetRpcClient();
            return await rpcClient.InvokeContractWithTransactionXdrAsync(transactionXdr);
        }

        /// <summary>
        /// Gets the HMAC key for verification from constructor, environment, or configuration.
        /// </summary>
        /// <returns>The HMAC key as a Base64 string.</returns>
        /// <remarks>
        /// In production, this should be securely managed through:
        /// - Constructor parameter (recommended for programmatic use)
        /// - Environment variables
        /// - Azure Key Vault
        /// - AWS Secrets Manager
        /// - Or similar secure key management systems
        /// </remarks>
        private string GetHmacKeyForVerification()
        {
            // First try constructor-provided key
            if (!string.IsNullOrEmpty(_hmacKey))
            {
                return _hmacKey;
            }

            // Try to get from environment variable
            var envKey = Environment.GetEnvironmentVariable("ZKP_HMAC_KEY");
            
            if (!string.IsNullOrEmpty(envKey))
            {
                return envKey;
            }

            throw new InvalidOperationException(
                "HMAC key not configured. Provide it via constructor, " +
                "set the ZKP_HMAC_KEY environment variable, " +
                "or use a secure key management system.");
        }

        /// <summary>
        /// Verifies a Bulletproofs ZK range proof on-chain via the Soroban contract.
        /// The proof and commitment are generated by <see cref="ZkpSharp.Security.BulletproofsProvider"/>.
        /// </summary>
        /// <param name="contractId">The deployed ZkpVerifier contract address.</param>
        /// <param name="proof">The Bulletproofs range proof bytes.</param>
        /// <param name="commitment">The Pedersen commitment (33-byte compressed secp256k1 point).</param>
        /// <param name="min">The minimum value of the proven range.</param>
        /// <param name="max">The maximum value of the proven range.</param>
        /// <returns>True if the on-chain structural and Fiat-Shamir binding verification passes.</returns>
        /// <remarks>Requires <c>ZKP_SOURCE_ACCOUNT</c> unless you use <see cref="VerifyZkRangeProofWithSourceAccount"/>.</remarks>
        public Task<bool> VerifyZkRangeProof(
            string contractId,
            byte[] proof,
            byte[] commitment,
            long min,
            long max)
        {
            ValidateZkInputs(contractId, proof, commitment);
            var source = GetSimulationSourceAccountIdOrThrow();
            return VerifyZkRangeProofWithSourceAccount(source, contractId, proof, commitment, min, max);
        }

        /// <summary>
        /// Verifies a Bulletproofs ZK age proof on-chain via the Soroban contract.
        /// </summary>
        /// <param name="contractId">The deployed ZkpVerifier contract address.</param>
        /// <param name="proof">The Bulletproofs range proof bytes.</param>
        /// <param name="commitment">The Pedersen commitment (33-byte compressed secp256k1 point).</param>
        /// <param name="minAge">The minimum age that was proven.</param>
        /// <returns>True if verification passes.</returns>
        /// <remarks>Requires <c>ZKP_SOURCE_ACCOUNT</c> unless you use <see cref="VerifyZkAgeProofWithSourceAccount"/>.</remarks>
        public Task<bool> VerifyZkAgeProof(
            string contractId,
            byte[] proof,
            byte[] commitment,
            uint minAge)
        {
            ValidateZkInputs(contractId, proof, commitment);
            var source = GetSimulationSourceAccountIdOrThrow();
            return VerifyZkAgeProofWithSourceAccount(source, contractId, proof, commitment, minAge);
        }

        /// <summary>
        /// Verifies a Bulletproofs ZK balance proof on-chain via the Soroban contract.
        /// </summary>
        /// <param name="contractId">The deployed ZkpVerifier contract address.</param>
        /// <param name="proof">The Bulletproofs range proof bytes.</param>
        /// <param name="commitment">The Pedersen commitment (33-byte compressed secp256k1 point).</param>
        /// <param name="requiredAmount">The minimum balance that was proven.</param>
        /// <returns>True if verification passes.</returns>
        /// <remarks>Requires <c>ZKP_SOURCE_ACCOUNT</c> unless you use <see cref="VerifyZkBalanceProofWithSourceAccount"/>.</remarks>
        public Task<bool> VerifyZkBalanceProof(
            string contractId,
            byte[] proof,
            byte[] commitment,
            long requiredAmount)
        {
            ValidateZkInputs(contractId, proof, commitment);
            var source = GetSimulationSourceAccountIdOrThrow();
            return VerifyZkBalanceProofWithSourceAccount(source, contractId, proof, commitment, requiredAmount);
        }

        /// <summary>
        /// Verifies a Bulletproofs ZK range proof on-chain using a funded source account (valid envelope for RPC simulation).
        /// </summary>
        public async Task<bool> VerifyZkRangeProofWithSourceAccount(
            string sourceAccountId,
            string contractId,
            byte[] proof,
            byte[] commitment,
            long min,
            long max)
        {
            ValidateZkInputs(contractId, proof, commitment);
            if (string.IsNullOrEmpty(sourceAccountId))
                throw new ArgumentException("Source account ID cannot be null or empty.", nameof(sourceAccountId));

            Server server = new(_serverUrl);
            AccountResponse sourceAccount = await server.Accounts.Account(sourceAccountId);

            var transactionBuilder = new SorobanTransactionBuilder(_network);
            var transactionXdr = transactionBuilder.BuildVerifyZkRangeProofTransactionWithAccount(
                sourceAccount: sourceAccount,
                contractId: contractId,
                proof: Convert.ToBase64String(proof),
                commitment: Convert.ToBase64String(commitment),
                min: min,
                max: max);

            var rpcClient = GetRpcClient();
            return await rpcClient.InvokeContractWithTransactionXdrAsync(transactionXdr);
        }

        /// <summary>
        /// Verifies a Bulletproofs ZK age proof on-chain using a funded source account.
        /// </summary>
        public async Task<bool> VerifyZkAgeProofWithSourceAccount(
            string sourceAccountId,
            string contractId,
            byte[] proof,
            byte[] commitment,
            uint minAge)
        {
            ValidateZkInputs(contractId, proof, commitment);
            if (string.IsNullOrEmpty(sourceAccountId))
                throw new ArgumentException("Source account ID cannot be null or empty.", nameof(sourceAccountId));

            Server server = new(_serverUrl);
            AccountResponse sourceAccount = await server.Accounts.Account(sourceAccountId);

            var transactionBuilder = new SorobanTransactionBuilder(_network);
            var transactionXdr = transactionBuilder.BuildVerifyZkAgeProofTransactionWithAccount(
                sourceAccount: sourceAccount,
                contractId: contractId,
                proof: Convert.ToBase64String(proof),
                commitment: Convert.ToBase64String(commitment),
                minAge: minAge);

            var rpcClient = GetRpcClient();
            return await rpcClient.InvokeContractWithTransactionXdrAsync(transactionXdr);
        }

        /// <summary>
        /// Verifies a Bulletproofs ZK balance proof on-chain using a funded source account.
        /// </summary>
        public async Task<bool> VerifyZkBalanceProofWithSourceAccount(
            string sourceAccountId,
            string contractId,
            byte[] proof,
            byte[] commitment,
            long requiredAmount)
        {
            ValidateZkInputs(contractId, proof, commitment);
            if (string.IsNullOrEmpty(sourceAccountId))
                throw new ArgumentException("Source account ID cannot be null or empty.", nameof(sourceAccountId));

            Server server = new(_serverUrl);
            AccountResponse sourceAccount = await server.Accounts.Account(sourceAccountId);

            var transactionBuilder = new SorobanTransactionBuilder(_network);
            var transactionXdr = transactionBuilder.BuildVerifyZkBalanceProofTransactionWithAccount(
                sourceAccount: sourceAccount,
                contractId: contractId,
                proof: Convert.ToBase64String(proof),
                commitment: Convert.ToBase64String(commitment),
                requiredAmount: requiredAmount);

            var rpcClient = GetRpcClient();
            return await rpcClient.InvokeContractWithTransactionXdrAsync(transactionXdr);
        }

        private static void ValidateZkInputs(string contractId, byte[] proof, byte[] commitment)
        {
            if (string.IsNullOrEmpty(contractId))
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            if (proof == null || proof.Length == 0)
                throw new ArgumentException("Proof cannot be null or empty.", nameof(proof));
            if (commitment == null || commitment.Length == 0)
                throw new ArgumentException("Commitment cannot be null or empty.", nameof(commitment));
        }

        /// <summary>
        /// Verifies proof using a pre-built Soroban transaction XDR.
        /// Use this method when you have already built the transaction XDR using a Soroban SDK.
        /// </summary>
        /// <param name="transactionXdr">The transaction XDR (base64 encoded) for invoking the contract.</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        public async Task<bool> VerifyProofWithTransactionXdrAsync(string transactionXdr)
        {
            if (string.IsNullOrEmpty(transactionXdr))
            {
                throw new ArgumentException("Transaction XDR cannot be null or empty.", nameof(transactionXdr));
            }

            try
            {
                var rpcClient = GetRpcClient();
                return await rpcClient.InvokeContractWithTransactionXdrAsync(transactionXdr);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during proof verification: {ex.Message}", ex);
            }
        }

        public async Task<double> GetAccountBalance(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));
            }

            Server server = new(_serverUrl);
            KeyPair keypair = KeyPair.FromAccountId(accountId);

            // Get account details
            AccountResponse accountResponse = await server.Accounts.Account(keypair.AccountId);

            // Get balance (XLM)
            foreach (var balance in accountResponse.Balances)
            {
                if (string.IsNullOrEmpty(balance.AssetCode))
                {
                    if (double.TryParse(balance.BalanceString, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                    {
                        return result;
                    }
                    throw new InvalidOperationException($"Unable to parse balance: {balance.BalanceString}");
                }
            }

            throw new InvalidOperationException("Native balance not found.");
        }
    }
}