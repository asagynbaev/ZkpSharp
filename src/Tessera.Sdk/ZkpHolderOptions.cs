using Tessera.Chains;
using Tessera.Did;

namespace Tessera.Sdk;

/// <summary>
/// Composition-root configuration for <see cref="ZkpHolder"/>.
/// </summary>
/// <remarks>
/// <para>
/// The holder facade composes a DID service over the provided store and signature verifier.
/// The chain anchor is optional — pass null for offline-only flows where the holder produces
/// presentations but does not anchor roots on-chain.
/// </para>
/// <para>
/// Lifetime: an instance of <see cref="ZkpHolder"/> represents one specific DID. Construct
/// multiple holders for multi-tenant scenarios.
/// </para>
/// </remarks>
public sealed record ZkpHolderOptions
{
    public required IDidStore Store { get; init; }
    public required ISignatureVerifier SignatureVerifier { get; init; }

    /// <summary>
    /// Optional on-chain anchor. When null, <see cref="ZkpHolder.AnchorRootAsync"/> throws.
    /// Off-chain bundling and presentation building work without a chain anchor.
    /// </summary>
    public IChainAnchor? ChainAnchor { get; init; }

    public TimeProvider? Clock { get; init; }
}
