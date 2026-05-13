namespace ZkpSharp.Did;

/// <summary>
/// Verifies Ed25519 signatures using a delegate. Indirection lets callers plug in
/// Solnet, NSec, BouncyCastle, or any other Ed25519 implementation without making
/// this package take a hard dependency on one.
/// </summary>
public sealed class Ed25519SignatureVerifier : ISignatureVerifier
{
    private readonly Ed25519VerifyDelegate _verify;

    public Ed25519SignatureVerifier(Ed25519VerifyDelegate verify)
    {
        _verify = verify ?? throw new ArgumentNullException(nameof(verify));
    }

    public bool Verify(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        => _verify(publicKey, message, signature);
}

public delegate bool Ed25519VerifyDelegate(
    ReadOnlySpan<byte> publicKey,
    ReadOnlySpan<byte> message,
    ReadOnlySpan<byte> signature);
