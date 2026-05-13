using System.Collections.Concurrent;
using Tessera.Chains;
using Tessera.Core;

namespace Tessera.Sdk.Tests;

/// <summary>
/// Test double for <see cref="IChainAnchor"/>. Stores anchor state in a dictionary;
/// thread-safe; resets between fixtures.
/// </summary>
/// <remarks>
/// Mirrors the behavior of the on-chain identity-registry program closely enough for
/// flow testing: register/update root, monotonically increasing revocation epoch.
/// </remarks>
internal sealed class InMemoryChainAnchor : IChainAnchor
{
    private readonly ConcurrentDictionary<DidId, AnchorState> _state = new();

    public string ChainId => "test";

    public Task<AnchorTxResult> AnchorRootAsync(DidId did, byte[] attestationRoot, CancellationToken ct = default)
    {
        _state.AddOrUpdate(
            did,
            _ => new AnchorState
            {
                Did = did,
                AttestationRoot = (byte[])attestationRoot.Clone(),
                RevocationEpoch = 0,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            (_, existing) => existing with
            {
                AttestationRoot = (byte[])attestationRoot.Clone(),
                UpdatedAt = DateTimeOffset.UtcNow,
            });

        return Task.FromResult(new AnchorTxResult(
            TxId: "test-tx-" + Guid.NewGuid().ToString("N"),
            Slot: null,
            SubmittedAt: DateTimeOffset.UtcNow));
    }

    public Task<AnchorState?> GetAnchorAsync(DidId did, CancellationToken ct = default)
        => Task.FromResult(_state.TryGetValue(did, out var s) ? s : null);

    public Task<AnchorTxResult> BumpRevocationAsync(DidId did, RevocationReason reason, CancellationToken ct = default)
    {
        if (!_state.TryGetValue(did, out var existing))
            throw new InvalidOperationException($"Cannot bump revocation for unanchored DID: {did}.");

        _state[did] = existing with
        {
            RevocationEpoch = existing.RevocationEpoch + 1,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        return Task.FromResult(new AnchorTxResult(
            TxId: "test-revoke-" + Guid.NewGuid().ToString("N"),
            Slot: null,
            SubmittedAt: DateTimeOffset.UtcNow));
    }

    public Task<bool> IsRevokedSinceAsync(DidId did, ulong asOfEpoch, CancellationToken ct = default)
        => Task.FromResult(_state.TryGetValue(did, out var s) && s.RevocationEpoch > asOfEpoch);
}
