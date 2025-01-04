// Author: Azimbek Sagynbaev
// Last modified on: 8/9/2021 8:57:00 AM

namespace ZkpSharp.Integration.Stellar
{
    public class StellarAccount
    {
        public string PublicKey { get; }
        public string SecretKey { get; }
        public StellarDotnetSdk.Accounts.Account Account { get; }

        public StellarAccount(string publicKey, string secretKey)
        {
            PublicKey = publicKey;
            SecretKey = secretKey;

            StellarDotnetSdk.Accounts.Account account = new(publicKey, sequenceNumber: null);
            Account = account;
        }

        public string GetPublicKey() => PublicKey;
        public string GetSecretKey() => SecretKey;
    }
}
