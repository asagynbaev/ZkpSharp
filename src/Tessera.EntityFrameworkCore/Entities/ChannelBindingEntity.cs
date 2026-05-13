namespace Tessera.EntityFrameworkCore.Entities;

public sealed class ChannelBindingEntity
{
    public long DbId { get; set; }

    /// <summary>FK to <see cref="DidDocumentEntity.Id"/>.</summary>
    public string DidId { get; set; } = "";

    /// <summary>Channel type: <c>"phone"</c>, <c>"email"</c>, <c>"telegram"</c>, etc.</summary>
    public string Type { get; set; } = "";

    /// <summary>blake3 / HKDF commitment to the channel identifier. Never store the raw handle.</summary>
    public byte[] Commitment { get; set; } = Array.Empty<byte>();

    /// <summary>The issuer DID that attested to this channel.</summary>
    public string Issuer { get; set; } = "";

    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public DidDocumentEntity? Document { get; set; }
}
