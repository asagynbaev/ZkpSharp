using Tessera.Chains.Solana.Internal;

namespace Tessera.Chains.Solana.Accounts;

/// <summary>
/// Decoded <c>DidAnchor</c> account from the identity-registry program.
/// Mirrors the Rust <c>#[account] struct DidAnchor</c> after the 8-byte discriminator.
/// </summary>
public sealed record DidAnchorAccount
{
    public required byte AccountVersion { get; init; }
    public required byte[] DidHash { get; init; }
    public required byte[] Owner { get; init; }
    public required byte[] AttestationRoot { get; init; }
    public required ulong RevocationEpoch { get; init; }
    public required long CreatedAt { get; init; }
    public required long UpdatedAt { get; init; }

    /// <summary>
    /// Decode raw account data (including the 8-byte Anchor discriminator) into a <see cref="DidAnchorAccount"/>.
    /// </summary>
    /// <exception cref="ArgumentException">If the discriminator does not match <c>DidAnchor</c>.</exception>
    public static DidAnchorAccount Decode(ReadOnlySpan<byte> rawAccountData)
    {
        const int expectedLen = 8 + 1 + 32 + 32 + 32 + 8 + 8 + 8; // 129
        if (rawAccountData.Length < expectedLen)
            throw new ArgumentException(
                $"DidAnchor account data must be at least {expectedLen} bytes (got {rawAccountData.Length}).",
                nameof(rawAccountData));

        var disc = rawAccountData[..8];
        if (!disc.SequenceEqual(IdentityRegistryDiscriminators.DidAnchorAccount))
            throw new ArgumentException("Account discriminator does not match DidAnchor.", nameof(rawAccountData));

        var reader = new AnchorBorshReader(rawAccountData[8..]);
        return new DidAnchorAccount
        {
            AccountVersion = reader.ReadU8(),
            DidHash = reader.ReadFixedBytes(32),
            Owner = reader.ReadPubkey(),
            AttestationRoot = reader.ReadFixedBytes(32),
            RevocationEpoch = reader.ReadU64(),
            CreatedAt = reader.ReadI64(),
            UpdatedAt = reader.ReadI64(),
        };
    }
}
