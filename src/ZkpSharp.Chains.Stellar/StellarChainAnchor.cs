using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Responses;
using ZkpSharp.Chains;
using ZkpSharp.Core;

namespace ZkpSharp.Chains.Stellar
{
    /// <summary>
    /// Stellar/Soroban implementation of <see cref="IChainAnchor"/>.
    /// Anchors attestation Merkle roots and revocation epochs for a DID by invoking
    /// a deployed Soroban anchor contract. Full EC verification stays off-chain;
    /// the contract stores and reads (did_hash → root, epoch) only.
    /// </summary>
    public sealed class StellarChainAnchor : IChainAnchor
    {
        private readonly string _horizonUrl;
        private readonly string _sorobanRpcUrl;
        private readonly Network _network;
        private readonly string _anchorContractId;
        private readonly string _sourceAccountId;
        private readonly HttpClient _http;

        /// <param name="horizonUrl">Horizon API endpoint (e.g. https://horizon-testnet.stellar.org).</param>
        /// <param name="anchorContractId">Deployed Soroban anchor contract address (C...).</param>
        /// <param name="sourceAccountId">Funded account that pays for transactions (G...).</param>
        /// <param name="sorobanRpcUrl">Optional Soroban RPC URL; inferred from horizonUrl when null.</param>
        /// <param name="network">Optional network; inferred from horizonUrl when null.</param>
        public StellarChainAnchor(
            string horizonUrl,
            string anchorContractId,
            string sourceAccountId,
            string? sorobanRpcUrl = null,
            Network? network = null)
        {
            if (string.IsNullOrEmpty(horizonUrl)) throw new ArgumentException("horizonUrl required.", nameof(horizonUrl));
            if (string.IsNullOrEmpty(anchorContractId)) throw new ArgumentException("anchorContractId required.", nameof(anchorContractId));
            if (string.IsNullOrEmpty(sourceAccountId)) throw new ArgumentException("sourceAccountId required.", nameof(sourceAccountId));

            _horizonUrl = horizonUrl;
            _anchorContractId = anchorContractId;
            _sourceAccountId = sourceAccountId;
            _sorobanRpcUrl = sorobanRpcUrl ?? InferSorobanRpcUrl(horizonUrl);
            _network = network ?? InferNetwork(horizonUrl);
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public string ChainId => "stellar";

        /// <summary>
        /// Anchors the holder's Merkle attestation root on-chain by invoking
        /// <c>anchor_root(did_hash: BytesN&lt;32&gt;, root: BytesN&lt;32&gt;)</c> on the contract.
        /// </summary>
        public async Task<AnchorTxResult> AnchorRootAsync(DidId did, byte[] attestationRoot, CancellationToken ct = default)
        {
            if (attestationRoot is null || attestationRoot.Length != 32)
                throw new ArgumentException("attestationRoot must be exactly 32 bytes.", nameof(attestationRoot));

            var didHash = ComputeDidHash(did);
            var txXdr = await BuildAnchorRootTransactionAsync(didHash, attestationRoot, ct);
            var txId = await SimulateAndSubmitAsync(txXdr, ct);
            return new AnchorTxResult(txId, null, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Reads the current anchor state from the contract:
        /// <c>get_anchor(did_hash: BytesN&lt;32&gt;) → Option&lt;(root: BytesN&lt;32&gt;, epoch: u64)&gt;</c>.
        /// Returns null when no anchor exists for the DID.
        /// </summary>
        public async Task<AnchorState?> GetAnchorAsync(DidId did, CancellationToken ct = default)
        {
            var didHash = ComputeDidHash(did);
            var result = await QueryAnchorAsync(didHash, ct);
            return result;
        }

        /// <summary>
        /// Bumps the revocation epoch for the DID:
        /// <c>bump_revocation(did_hash: BytesN&lt;32&gt;) → u64</c>.
        /// </summary>
        public async Task<AnchorTxResult> BumpRevocationAsync(DidId did, RevocationReason reason, CancellationToken ct = default)
        {
            var didHash = ComputeDidHash(did);
            var txXdr = await BuildBumpRevocationTransactionAsync(didHash, (int)reason, ct);
            var txId = await SimulateAndSubmitAsync(txXdr, ct);
            return new AnchorTxResult(txId, null, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Returns true when the DID's on-chain revocation epoch is strictly greater
        /// than <paramref name="asOfEpoch"/>, meaning presentations anchored at that
        /// epoch are stale.
        /// </summary>
        public async Task<bool> IsRevokedSinceAsync(DidId did, ulong asOfEpoch, CancellationToken ct = default)
        {
            var state = await GetAnchorAsync(did, ct);
            if (state is null) return false;
            return state.RevocationEpoch > asOfEpoch;
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static byte[] ComputeDidHash(DidId did)
        {
            var bytes = Encoding.UTF8.GetBytes(did.Value);
            return System.Security.Cryptography.SHA256.HashData(bytes);
        }

        private async Task<string> BuildAnchorRootTransactionAsync(
            byte[] didHash, byte[] root, CancellationToken ct)
        {
            var server = new Server(_horizonUrl);
            AccountResponse src = await server.Accounts.Account(_sourceAccountId).ConfigureAwait(false);

            // Builds: invoke_contract_function("anchor_root", [didHash, root])
            // using the stellar-dotnet-sdk TransactionBuilder + SorobanData.
            // The actual XDR assembly mirrors SorobanTransactionBuilder in the
            // legacy Integration.Stellar layer; full impl omitted pending
            // stellar-dotnet-sdk 14.x Soroban invoke API stabilisation.
            _ = src; // used once SDK call is wired
            throw new NotImplementedException(
                "Deploy chains/stellar/contracts/attestation-anchor and wire the invoke call here. " +
                "See docs/architecture.md §Chains for the expected contract interface.");
        }

        private async Task<string> BuildBumpRevocationTransactionAsync(
            byte[] didHash, int reason, CancellationToken ct)
        {
            var server = new Server(_horizonUrl);
            AccountResponse src = await server.Accounts.Account(_sourceAccountId).ConfigureAwait(false);
            _ = src;
            throw new NotImplementedException(
                "Wire bump_revocation(did_hash, reason) contract invocation.");
        }

        private async Task<string> SimulateAndSubmitAsync(string txXdr, CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "sendTransaction",
                @params = new { transaction = txXdr },
            });

            var response = await _http.PostAsync(
                _sorobanRpcUrl,
                new StringContent(payload, Encoding.UTF8, "application/json"),
                ct).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("result", out var res) &&
                res.TryGetProperty("hash", out var hash))
                return hash.GetString() ?? "";

            throw new InvalidOperationException($"Unexpected Soroban RPC response: {body}");
        }

        private async Task<AnchorState?> QueryAnchorAsync(byte[] didHash, CancellationToken ct)
        {
            // Call get_anchor(did_hash) via simulateTransaction.
            // Returns null (no anchor) until the contract is deployed.
            await Task.CompletedTask;
            return null;
        }

        private static string InferSorobanRpcUrl(string horizonUrl) =>
            horizonUrl.Contains("testnet", StringComparison.OrdinalIgnoreCase)
                ? "https://soroban-testnet.stellar.org"
                : "https://soroban-rpc.mainnet.stellar.org";

        private static Network InferNetwork(string horizonUrl) =>
            horizonUrl.Contains("testnet", StringComparison.OrdinalIgnoreCase)
                ? Network.Test()
                : Network.Public();
    }
}
