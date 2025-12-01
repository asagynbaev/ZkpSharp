using System.Text;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Responses;
using ZkpSharp.Interfaces;

namespace ZkpSharp.Integration.Stellar
{
    public class StellarBlockchain : IBlockchain
    {
        private readonly string _serverUrl;
        private readonly string _sorobanRpcUrl;
        private SorobanRpcClient? _rpcClient;

        public StellarBlockchain(string serverUrl, string? sorobanRpcUrl = null)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                throw new ArgumentException("Server URL cannot be null or empty.", nameof(serverUrl));
            }
            _serverUrl = serverUrl;
            
            // Default Soroban RPC URL based on server URL
            _sorobanRpcUrl = sorobanRpcUrl ?? GetDefaultSorobanRpcUrl(serverUrl);
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

        private SorobanRpcClient GetRpcClient()
        {
            return _rpcClient ??= new SorobanRpcClient(_sorobanRpcUrl, _serverUrl);
        }

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

            try
            {
                var rpcClient = GetRpcClient();

                // Note: For full Soroban contract invocation, you need to build a proper transaction XDR
                // with InvokeHostFunctionOp. This requires using Soroban SDK or XDR encoding libraries.
                // 
                // For now, this implementation provides a structure that can be extended.
                // To use this method, you need to:
                // 1. Build a Soroban transaction XDR using a Soroban SDK (e.g., JavaScript/TypeScript SDK)
                // 2. Use InvokeContractWithTransactionXdrAsync with the pre-built transaction XDR
                //
                // Example workflow:
                // - Use Stellar/Soroban JavaScript SDK to build the transaction:
                //   const tx = new TransactionBuilder(...)
                //     .addOperation(Operation.invokeHostFunction({...}))
                //     .build();
                //   const xdr = tx.toXDR();
                // - Pass the XDR to InvokeContractWithTransactionXdrAsync

                throw new NotImplementedException(
                    "Full Soroban contract invocation requires proper transaction XDR building. " +
                    "Please use a Soroban SDK (e.g., JavaScript SDK) to build the transaction XDR, " +
                    "then use InvokeContractWithTransactionXdrAsync method. " +
                    "Alternatively, implement proper XDR encoding for InvokeHostFunctionOp.");
            }
            catch (NotImplementedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during proof verification: {ex.Message}", ex);
            }
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