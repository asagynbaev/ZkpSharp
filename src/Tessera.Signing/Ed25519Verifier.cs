namespace Tessera.Signing;

/// <summary>
/// Production Ed25519 signature verifier. Drop-in implementation for both
/// <see cref="Tessera.Did.ISignatureVerifier"/> and <see cref="Tessera.Attestations.ISignatureVerifier"/>
/// so the same instance can be handed to <c>DidService</c> and <c>AttestationVerifier</c>.
/// </summary>
/// <remarks>
/// Stateless and thread-safe. Construct once at startup and reuse.
/// </remarks>
public sealed class Ed25519Verifier
    : Tessera.Did.ISignatureVerifier,
      Tessera.Attestations.ISignatureVerifier
{
    public bool Verify(
        ReadOnlySpan<byte> publicKey,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> signature)
        => Ed25519.Verify(publicKey, message, signature);
}
