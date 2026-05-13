namespace Tessera.Did;

using Tessera.Core;

/// <summary>
/// Off-chain DID document. The authoritative description of a DID, signed by its controller.
/// Storage strategy: relational DB + optional public mirror (IPFS). Never written to chain directly;
/// only its Merkle attestation root and revocation epoch are anchored.
/// </summary>
public sealed record DidDocument
{
    public required DidId Id { get; init; }
    public required DidId Controller { get; init; }
    public required IReadOnlyList<VerificationMethod> VerificationMethods { get; init; }
    public IReadOnlyList<WalletBinding> Wallets { get; init; } = Array.Empty<WalletBinding>();
    public IReadOnlyList<ChannelBinding> Bindings { get; init; } = Array.Empty<ChannelBinding>();
    public byte[]? AttestationRoot { get; init; }
    public bool Revoked { get; init; }
    public int Version { get; init; } = 1;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record VerificationMethod
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string PublicKeyMultibase { get; init; }
}

/// <summary>
/// A wallet bound to a DID by signature. The wallet itself signed a challenge containing
/// {did, chain, address, nonce, expiry}. The binding is verifiable without trusting the issuer.
/// </summary>
public sealed record WalletBinding
{
    public required string Chain { get; init; }
    public required string Address { get; init; }
    public required byte[] ProofSignature { get; init; }
    public required DateTimeOffset BoundAt { get; init; }
}

/// <summary>
/// A bound off-chain channel (Telegram, phone, email, etc). Stored as commitment only — never plaintext.
/// </summary>
public sealed record ChannelBinding
{
    public required string Type { get; init; }
    public required byte[] Commitment { get; init; }
    public required DidId Issuer { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
