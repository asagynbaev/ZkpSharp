using ZkpSharp.Chains.Solana.Internal;

namespace ZkpSharp.Chains.Solana.Accounts;

/// <summary>
/// Decoded <c>Issuer</c> account from the identity-registry program.
/// Mirrors the Rust <c>#[account] struct Issuer</c> after the 8-byte discriminator.
/// </summary>
public sealed record IssuerAccount
{
    public required byte AccountVersion { get; init; }
    public required byte[] IssuerDidHash { get; init; }
    public required byte[] SigningKey { get; init; }
    public required string SchemaUri { get; init; }
    public required bool Active { get; init; }
    public required long CreatedAt { get; init; }

    /// <summary>
    /// Decode raw account data (including the 8-byte Anchor discriminator) into an <see cref="IssuerAccount"/>.
    /// </summary>
    public static IssuerAccount Decode(ReadOnlySpan<byte> rawAccountData)
    {
        // Minimum: discriminator + version + did_hash + signing_key + string_len + bool + i64
        const int minLen = 8 + 1 + 32 + 32 + 4 + 1 + 8;
        if (rawAccountData.Length < minLen)
            throw new ArgumentException(
                $"Issuer account data must be at least {minLen} bytes (got {rawAccountData.Length}).",
                nameof(rawAccountData));

        var disc = rawAccountData[..8];
        if (!disc.SequenceEqual(IdentityRegistryDiscriminators.IssuerAccount))
            throw new ArgumentException("Account discriminator does not match Issuer.", nameof(rawAccountData));

        var reader = new AnchorBorshReader(rawAccountData[8..]);
        return new IssuerAccount
        {
            AccountVersion = reader.ReadU8(),
            IssuerDidHash = reader.ReadFixedBytes(32),
            SigningKey = reader.ReadPubkey(),
            SchemaUri = reader.ReadString(),
            Active = reader.ReadBool(),
            CreatedAt = reader.ReadI64(),
        };
    }
}
