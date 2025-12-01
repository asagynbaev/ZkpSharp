using ZkpSharp.Interfaces;
using ZkpSharp.Serialization;

namespace ZkpSharp.Integration
{
    public class BlockchainClient
    {
        private readonly IProofChecker _proofChecker;
        private readonly IProofProvider _proofProvider;

        public BlockchainClient(IProofChecker proofChecker, IProofProvider proofProvider)
        {
            _proofChecker = proofChecker;
            _proofProvider = proofProvider;
        }

        public async Task<bool> VerifyProofAsync(string contractId, string proof, string salt, string value)
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

            var proofData = ZkpSharpExporter.SerializeProof(proof, salt);
            return await _proofChecker.VerifyProofAsync(contractId, proofData, salt, value);
        }
    }
}