using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Responses;
using ZkpSharp.Interfaces;

namespace ZkpSharp.Integration.Stellar
{
    public class StellarBlockchain : IBlockchain
    {
        private readonly string _serverUrl;

        public StellarBlockchain(string serverUrl)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                throw new ArgumentException("Server URL cannot be null or empty.", nameof(serverUrl));
            }
            _serverUrl = serverUrl;
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
                //TODO: Implement proof verification using Soroban contract
                throw new NotImplementedException("Proof verification via Soroban contract is not yet implemented.");
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