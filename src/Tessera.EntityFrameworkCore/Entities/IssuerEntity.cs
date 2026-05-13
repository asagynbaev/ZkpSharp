namespace Tessera.EntityFrameworkCore.Entities;

/// <summary>
/// Persistence-side projection of <see cref="Tessera.Attestations.IssuerRecord"/>.
/// </summary>
public sealed class IssuerEntity
{
    /// <summary>Issuer DID string. Primary key.</summary>
    public string Did { get; set; } = "";

    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    /// <summary>Algorithm identifier, e.g. <c>"ed25519"</c>.</summary>
    public string Algorithm { get; set; } = "";

    public string SchemaUri { get; set; } = "";

    /// <summary>False = revoked / deactivated; lookups must filter on this.</summary>
    public bool Active { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
