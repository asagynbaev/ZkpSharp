namespace ZkpSharp.EntityFrameworkCore.Entities;

public sealed class WalletBindingEntity
{
    public long DbId { get; set; }

    /// <summary>FK to <see cref="DidDocumentEntity.Id"/>.</summary>
    public string DidId { get; set; } = "";

    /// <summary>Chain identifier: <c>"solana"</c>, <c>"stellar"</c>, etc.</summary>
    public string Chain { get; set; } = "";

    public string Address { get; set; } = "";

    /// <summary>Wallet's signature over the canonical binding challenge.</summary>
    public byte[] ProofSignature { get; set; } = Array.Empty<byte>();

    public DateTimeOffset BoundAt { get; set; }

    public DidDocumentEntity? Document { get; set; }
}
