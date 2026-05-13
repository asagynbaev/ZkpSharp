namespace Tessera.Did;

using Tessera.Core;

/// <summary>
/// Storage abstraction for DID documents. Implementations: in-memory (testing/dev),
/// Postgres, etc. The store is the authoritative off-chain record; on-chain anchors
/// only reference its attestation root.
/// </summary>
public interface IDidStore
{
    Task<DidDocument?> GetAsync(DidId did, CancellationToken ct = default);
    Task SaveAsync(DidDocument document, CancellationToken ct = default);
    Task<bool> ExistsAsync(DidId did, CancellationToken ct = default);
}
