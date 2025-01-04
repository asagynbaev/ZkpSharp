using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Responses;
using StellarDotnetSdk.Transactions;
using StellarDotnetSdk.Xdr;
using ZkpSharp.Interfaces;

namespace ZkpSharp.Stellar
{
    public class StellarBlockchain : IBlockChain
    {
        private readonly string _serverUrl;

        public StellarBlockchain(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public async Task<bool> VerifyProof(string contractId, string proof, string salt, string value)
        {
            try
            {
                //TODO: Implement proof verification
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during proof verification: {ex.Message}");
                return false;
            }
        }

        public async Task<double> GetAccountBalance(string accountId)
        {
            Server server = new(_serverUrl);
            KeyPair keypair = KeyPair.FromAccountId(accountId);

            // Get account details
            AccountResponse accountResponse = await server.Accounts.Account(keypair.AccountId);

            // Get balance (XLM)
            foreach (var balance in accountResponse.Balances)
            {
                if (string.IsNullOrEmpty(balance.AssetCode))
                {
                    return double.Parse(balance.BalanceString);
                }
            }

            throw new InvalidOperationException("Native balance not found.");
        }
    }
}