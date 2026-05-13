namespace ZkpSharp.EntityFrameworkCore.Entities;

/// <summary>
/// Persistence-side projection of <see cref="ZkpSharp.Did.DidDocument"/>.
/// Mutable POCO shape EF Core can hydrate; the domain model stays immutable.
/// </summary>
public sealed class DidDocumentEntity
{
    /// <summary>The DID string (e.g. <c>did:zkp:base58hash</c>). Primary key.</summary>
    public string Id { get; set; } = "";

    /// <summary>The controller DID; same as <see cref="Id"/> for self-controlled DIDs.</summary>
    public string Controller { get; set; } = "";

    /// <summary>Current Merkle attestation root (null when no attestations bundled yet).</summary>
    public byte[]? AttestationRoot { get; set; }

    /// <summary>True once <c>DidService.RevokeAsync</c> has been called.</summary>
    public bool Revoked { get; set; }

    /// <summary>Monotonically increasing version counter bumped on every mutation.</summary>
    public int Version { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<VerificationMethodEntity> VerificationMethods { get; set; } = new();
    public List<WalletBindingEntity> Wallets { get; set; } = new();
    public List<ChannelBindingEntity> Bindings { get; set; } = new();
}
