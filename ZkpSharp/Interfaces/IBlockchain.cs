namespace ZkpSharp.Interfaces
{
    /// <summary>
    /// Interface for blockchain integration to verify proofs on-chain.
    /// </summary>
    public interface IBlockchain
    {
        /// <summary>
        /// Verifies a zero-knowledge proof on the blockchain using a smart contract (RPC simulation).
        /// </summary>
        /// <param name="contractId">The smart contract address or ID.</param>
        /// <param name="proof">The proof to verify.</param>
        /// <param name="salt">The salt used to generate the proof.</param>
        /// <param name="value">The value that was proven.</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        /// <remarks>
        /// Stellar implementation requires <c>ZKP_SOURCE_ACCOUNT</c> when using <see cref="ZkpSharp.Integration.Stellar.StellarBlockchain"/>.
        /// </remarks>
        Task<bool> VerifyProof(string contractId, string proof, string salt, string value);

        /// <summary>
        /// Gets the account balance from the blockchain.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <returns>The account balance.</returns>
        Task<double> GetAccountBalance(string accountId);
    }
}