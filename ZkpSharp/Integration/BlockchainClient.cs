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

        public async Task<bool> VerifyProofAsync(string contractId, string proof, string value)
        {
            var salt = _proofProvider.GenerateSalt();
            var hmac = _proofProvider.GenerateHMAC(value);
            var proofData = ZkpSharpExporter.SerializeProof(proof, salt);
            return await _proofChecker.VerifyProofAsync(contractId, proofData, hmac, value);
        }
    }
}