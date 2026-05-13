namespace ZkpSharp.Attestations;

using ZkpSharp.Core;

/// <summary>
/// Resolves an issuer DID to its current public verification key. Backing implementations
/// can be in-memory, database-backed, or chain-backed (reading the on-chain issuer registry).
/// </summary>
public interface IIssuerRegistry
{
    /// <summary>Return the registered public key for an issuer, or null if unknown/revoked.</summary>
    Task<IssuerRecord?> ResolveAsync(DidId issuer, CancellationToken ct = default);
}

public sealed record IssuerRecord
{
    public required DidId Did { get; init; }
    public required byte[] PublicKey { get; init; }
    public required string Algorithm { get; init; }
    public required string SchemaUri { get; init; }
    public required bool Active { get; init; }
}
