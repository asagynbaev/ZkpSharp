using System.Security.Cryptography;
using System.Text;
using ZkpSharp.Chains;
using ZkpSharp.Core;

namespace ZkpSharp.Chains.Solana
{
    /// <summary>
    /// Solana implementation of <see cref="IChainAnchor"/>.
    /// Drives the <c>chains/solana/programs/identity-registry</c> Anchor program.
    /// <para>
    /// PDAs used:
    /// <list type="bullet">
    ///   <item><c>["did", did_hash]</c> → <c>DidAnchor</c> (attestation_root + revocation_epoch)</item>
    ///   <item><c>["issuer", issuer_did_hash]</c> → <c>Issuer</c> (signing_key + schema_uri + active)</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class SolanaChainAnchor : IChainAnchor
    {
        private readonly string _rpcUrl;
        private readonly string _programId;
        private readonly byte[] _payerKeypair;

        /// <param name="rpcUrl">Solana RPC endpoint (e.g. https://api.devnet.solana.com).</param>
        /// <param name="programId">Deployed IdentityRegistry program ID (base58).</param>
        /// <param name="payerKeypair">64-byte Ed25519 keypair for signing transactions.</param>
        public SolanaChainAnchor(string rpcUrl, string programId, byte[] payerKeypair)
        {
            if (string.IsNullOrEmpty(rpcUrl)) throw new ArgumentException("rpcUrl required.", nameof(rpcUrl));
            if (string.IsNullOrEmpty(programId)) throw new ArgumentException("programId required.", nameof(programId));
            if (payerKeypair is null || payerKeypair.Length != 64)
                throw new ArgumentException("payerKeypair must be a 64-byte Ed25519 keypair.", nameof(payerKeypair));

            _rpcUrl = rpcUrl;
            _programId = programId;
            _payerKeypair = payerKeypair;
        }

        public string ChainId => "solana";

        /// <summary>
        /// Calls <c>update_root(did_hash, attestation_root)</c> on the IdentityRegistry program.
        /// Creates the <c>DidAnchor</c> PDA on first call (register_did), updates on subsequent calls.
        /// </summary>
        public Task<AnchorTxResult> AnchorRootAsync(DidId did, byte[] attestationRoot, CancellationToken ct = default)
        {
            if (attestationRoot is null || attestationRoot.Length != 32)
                throw new ArgumentException("attestationRoot must be exactly 32 bytes.", nameof(attestationRoot));

            // TODO: wire Solnet
            // var rpc = ClientFactory.GetClient(_rpcUrl);
            // var payer = new Account(_payerKeypair[..32], _payerKeypair[32..]);
            // var didHashBytes = ComputeDidHash(did);
            // var (didAnchorPda, _) = PublicKey.FindProgramAddress(
            //     new[] { Encoding.UTF8.GetBytes("did"), didHashBytes }, new PublicKey(_programId));
            // Build + send update_root instruction ...
            throw new NotImplementedException(
                "Add Solnet package references (see ZkpSharp.Chains.Solana.csproj) and wire " +
                "the update_root instruction against the identity-registry program.");
        }

        /// <summary>
        /// Reads the <c>DidAnchor</c> PDA account and returns its state.
        /// Returns null when the PDA does not exist yet.
        /// </summary>
        public Task<AnchorState?> GetAnchorAsync(DidId did, CancellationToken ct = default)
        {
            // TODO: wire Solnet
            // var rpc = ClientFactory.GetClient(_rpcUrl);
            // var didHashBytes = ComputeDidHash(did);
            // var (pda, _) = PublicKey.FindProgramAddress(...)
            // var account = await rpc.GetAccountInfoAsync(pda.Key);
            // if (!account.WasSuccessful || account.Result.Value == null) return null;
            // Deserialize DidAnchor Borsh layout and return AnchorState.
            return Task.FromResult<AnchorState?>(null);
        }

        /// <summary>
        /// Calls <c>bump_revocation(did_hash)</c> on the IdentityRegistry program,
        /// incrementing the on-chain revocation epoch.
        /// </summary>
        public Task<AnchorTxResult> BumpRevocationAsync(DidId did, RevocationReason reason, CancellationToken ct = default)
        {
            throw new NotImplementedException(
                "Wire the bump_revocation instruction. See chains/solana/programs/identity-registry/src/lib.rs.");
        }

        public async Task<bool> IsRevokedSinceAsync(DidId did, ulong asOfEpoch, CancellationToken ct = default)
        {
            var state = await GetAnchorAsync(did, ct).ConfigureAwait(false);
            if (state is null) return false;
            return state.RevocationEpoch > asOfEpoch;
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static byte[] ComputeDidHash(DidId did)
            => SHA256.HashData(Encoding.UTF8.GetBytes(did.Value));
    }
}
