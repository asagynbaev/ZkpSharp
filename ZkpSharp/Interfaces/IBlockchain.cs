namespace ZkpSharp.Interfaces
{
    public interface IBlockChain
    {
        Task<bool> VerifyProof(string contractId, string proof, string salt, string value);
        Task<double> GetAccountBalance(string accountId);
    }
}