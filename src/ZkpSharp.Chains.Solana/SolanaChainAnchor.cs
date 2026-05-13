using System.Security.Cryptography;
using System.Text;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using ZkpSharp.Chains.Solana.Accounts;
using ZkpSharp.Chains.Solana.Instructions;
using ZkpSharp.Chains.Solana.Internal;
using ZkpSharp.Core;

namespace ZkpSharp.Chains.Solana;

/// <summary>
/// Solana implementation of <see cref="IChainAnchor"/>. Drives the
/// <c>chains/solana/programs/identity-registry</c> Anchor program via Solnet RPC.
/// </summary>
/// <remarks>
/// <para>
/// Owner semantics: the <c>owner</c> field of the on-chain <c>DidAnchor</c> account is the
/// payer keypair passed to this anchor. The same keypair must sign every subsequent
/// <see cref="AnchorRootAsync"/> / <see cref="BumpRevocationAsync"/> call, which the
/// program enforces via <c>require_keys_eq!(anchor.owner, signer)</c>.
/// </para>
/// <para>
/// <see cref="AnchorRootAsync"/> auto-detects whether to call <c>register_did</c>
/// (PDA does not exist yet) or <c>update_root</c> (PDA already exists).
/// </para>
/// </remarks>
public sealed class SolanaChainAnchor : IChainAnchor
{
    private readonly IRpcClient _rpc;
    private readonly PublicKey _programId;
    private readonly Account _payer;
    private readonly Commitment _commitment;

    /// <param name="rpcUrl">Solana RPC endpoint, e.g. <c>https://api.devnet.solana.com</c>.</param>
    /// <param name="programId">Deployed identity-registry program ID, base58.</param>
    /// <param name="payerKeypair">64-byte Ed25519 keypair: 32-byte seed ‖ 32-byte public key.</param>
    /// <param name="commitment">Confirmation level for reads and submission. Default: <see cref="Commitment.Confirmed"/>.</param>
    public SolanaChainAnchor(
        string rpcUrl,
        string programId,
        byte[] payerKeypair,
        Commitment commitment = Commitment.Confirmed)
    {
        if (string.IsNullOrEmpty(rpcUrl)) throw new ArgumentException("rpcUrl required.", nameof(rpcUrl));
        if (string.IsNullOrEmpty(programId)) throw new ArgumentException("programId required.", nameof(programId));
        if (payerKeypair is null || payerKeypair.Length != 64)
            throw new ArgumentException("payerKeypair must be a 64-byte Ed25519 keypair (32-byte seed + 32-byte public key).", nameof(payerKeypair));

        _rpc = ClientFactory.GetClient(rpcUrl);
        _programId = new PublicKey(programId);
        _payer = new Account(payerKeypair[..32].ToArray(), payerKeypair[32..].ToArray());
        _commitment = commitment;
    }

    /// <summary>Testing/composition-root constructor. Lets callers inject a mock <see cref="IRpcClient"/>.</summary>
    internal SolanaChainAnchor(
        IRpcClient rpc,
        PublicKey programId,
        Account payer,
        Commitment commitment = Commitment.Confirmed)
    {
        _rpc = rpc;
        _programId = programId;
        _payer = payer;
        _commitment = commitment;
    }

    public string ChainId => "solana";

    public async Task<AnchorTxResult> AnchorRootAsync(DidId did, byte[] attestationRoot, CancellationToken ct = default)
    {
        if (attestationRoot is null || attestationRoot.Length != 32)
            throw new ArgumentException("attestationRoot must be exactly 32 bytes.", nameof(attestationRoot));

        var didHash = ComputeDidHash(did);
        var (pda, _) = IdentityRegistryPdas.DidAnchor(_programId, didHash);

        // If the PDA already exists, update; otherwise register.
        var existing = await TryLoadDidAnchorAsync(pda, ct).ConfigureAwait(false);
        var ix = existing is null
            ? IdentityRegistryInstructions.RegisterDid(_programId, pda, _payer.PublicKey, didHash, attestationRoot)
            : IdentityRegistryInstructions.UpdateRoot(_programId, pda, _payer.PublicKey, attestationRoot);

        return await SubmitAsync(ix, ct).ConfigureAwait(false);
    }

    public async Task<AnchorState?> GetAnchorAsync(DidId did, CancellationToken ct = default)
    {
        var didHash = ComputeDidHash(did);
        var (pda, _) = IdentityRegistryPdas.DidAnchor(_programId, didHash);
        var account = await TryLoadDidAnchorAsync(pda, ct).ConfigureAwait(false);
        if (account is null) return null;

        return new AnchorState
        {
            Did = did,
            AttestationRoot = account.AttestationRoot,
            RevocationEpoch = account.RevocationEpoch,
            UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(account.UpdatedAt),
        };
    }

    public async Task<AnchorTxResult> BumpRevocationAsync(DidId did, RevocationReason reason, CancellationToken ct = default)
    {
        var didHash = ComputeDidHash(did);
        var (pda, _) = IdentityRegistryPdas.DidAnchor(_programId, didHash);
        var ix = IdentityRegistryInstructions.BumpRevocation(_programId, pda, _payer.PublicKey, (byte)reason);
        return await SubmitAsync(ix, ct).ConfigureAwait(false);
    }

    public async Task<bool> IsRevokedSinceAsync(DidId did, ulong asOfEpoch, CancellationToken ct = default)
    {
        var state = await GetAnchorAsync(did, ct).ConfigureAwait(false);
        if (state is null) return false;
        return state.RevocationEpoch > asOfEpoch;
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private async Task<DidAnchorAccount?> TryLoadDidAnchorAsync(PublicKey pda, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var response = await _rpc.GetAccountInfoAsync(pda.Key, _commitment).ConfigureAwait(false);
        if (!response.WasSuccessful || response.Result?.Value?.Data is null || response.Result.Value.Data.Count == 0)
            return null;

        var raw = Convert.FromBase64String(response.Result.Value.Data[0]);
        return DidAnchorAccount.Decode(raw);
    }

    private async Task<AnchorTxResult> SubmitAsync(TransactionInstruction ix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var blockhashResp = await _rpc.GetLatestBlockHashAsync(_commitment).ConfigureAwait(false);
        if (!blockhashResp.WasSuccessful || blockhashResp.Result?.Value is null)
            throw new InvalidOperationException($"Failed to fetch latest blockhash: {blockhashResp.Reason}");

        var tx = new TransactionBuilder()
            .SetRecentBlockHash(blockhashResp.Result.Value.Blockhash)
            .SetFeePayer(_payer)
            .AddInstruction(ix)
            .Build(_payer);

        ct.ThrowIfCancellationRequested();
        var sendResp = await _rpc.SendTransactionAsync(tx, commitment: _commitment).ConfigureAwait(false);
        if (!sendResp.WasSuccessful || string.IsNullOrEmpty(sendResp.Result))
            throw new InvalidOperationException($"Solana submit failed: {sendResp.Reason}");

        return new AnchorTxResult(sendResp.Result, null, DateTimeOffset.UtcNow);
    }

    private static byte[] ComputeDidHash(DidId did)
        => SHA256.HashData(Encoding.UTF8.GetBytes(did.Value));
}
