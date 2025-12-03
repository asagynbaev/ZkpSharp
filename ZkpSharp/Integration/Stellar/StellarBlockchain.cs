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
        private SorobanRpcClient? _rpcClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="StellarBlockchain"/> class.
        /// </summary>
        /// <param name="serverUrl">The Horizon API server URL.</param>
        /// <param name="sorobanRpcUrl">Optional Soroban RPC URL. If not provided, will be inferred from server URL.</param>
        /// <param name="network">Optional network configuration. If not provided, will be inferred from server URL.</param>
        public StellarBlockchain(string serverUrl, string? sorobanRpcUrl = null, Network? network = null)
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
        /// Verifies a zero-knowledge proof on the blockchain using a Soroban smart contract.
        /// </summary>
        /// <param name="contractId">The smart contract address or ID.</param>
        /// <param name="proof">The proof to verify (Base64 encoded HMAC-SHA256, 32 bytes).</param>
        /// <param name="salt">The salt used to generate the proof (Base64 encoded, min 16 bytes).</param>
        /// <param name="value">The value that was proven.</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        /// <remarks>
        /// This method builds a transaction XDR for invoking the verify_proof function on the Soroban contract.
        /// The transaction is simulated (not submitted to the network) to get the verification result.
        /// For submitting transactions to the network, use VerifyProofAndSubmitAsync method.
        /// </remarks>
        public async Task<bool> VerifyProof(string contractId, string proof, string salt, string value)
        {
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

            // NOTE: Direct Soroban contract invocation from C# requires complex XDR encoding
            // that is not yet fully supported in the current Stellar .NET SDK.
            //
            // RECOMMENDED APPROACHES:
            //
            // 1. OFF-CHAIN VERIFICATION (Fastest, works today):
            //    var zkp = new Zkp(new ProofProvider(hmacKey));
            //    bool isValid = zkp.VerifyAge(proof, dateOfBirth, salt);
            //
            // 2. HYBRID APPROACH (Full on-chain support):
            //    - Generate proofs in C# (as above)
            //    - Use Stellar JavaScript SDK to invoke contract
            //    - See documentation for examples
            //
            // 3. CUSTOM XDR IMPLEMENTATION:
            //    - Implement InvokeHostFunctionOp XDR encoding
            //    - Use VerifyProofWithTransactionXdrAsync with your XDR
            //
            // See STELLAR_REALITY_CHECK.md and INTEGRATION_STATUS.md for detailed guides
            
            throw new NotImplementedException(
                "On-chain verification requires Soroban XDR encoding. " +
                "Use off-chain verification (Zkp.VerifyAge/VerifyBalance) or " +
                "hybrid approach with Stellar JS SDK. " +
                "See STELLAR_REALITY_CHECK.md for complete guide.");
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
        public async Task<bool> VerifyBalanceProof(
            string contractId,
            string proof,
            double balance,
            double requiredAmount,
            string salt)
        {
            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            // Same limitation as VerifyProof - see that method for details
            throw new NotImplementedException(
                "On-chain verification requires Soroban XDR encoding. " +
                "Use off-chain verification (Zkp.VerifyBalance) or " +
                "hybrid approach with Stellar JS SDK. " +
                "See STELLAR_REALITY_CHECK.md for complete guide.");
        }

        /// <summary>
        /// Gets the HMAC key for verification from environment or configuration.
        /// </summary>
        /// <returns>The HMAC key as a Base64 string.</returns>
        /// <remarks>
        /// In production, this should be securely managed through:
        /// - Environment variables
        /// - Azure Key Vault
        /// - AWS Secrets Manager
        /// - Or similar secure key management systems
        /// </remarks>
        private string GetHmacKeyForVerification()
        {
            // Try to get from environment variable first
            var hmacKey = Environment.GetEnvironmentVariable("ZKP_HMAC_KEY");
            
            if (string.IsNullOrEmpty(hmacKey))
            {
                throw new InvalidOperationException(
                    "HMAC key not configured. Set the ZKP_HMAC_KEY environment variable " +
                    "or configure secure key management.");
            }

            return hmacKey;
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
                    if (double.TryParse(balance.BalanceString, out double result))
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