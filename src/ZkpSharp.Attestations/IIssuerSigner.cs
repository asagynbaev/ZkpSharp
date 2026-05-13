namespace ZkpSharp.Attestations;

/// <summary>
/// Pluggable signer for issuer attestations. Implementations call out to whatever
/// key store the issuer uses (HSM, KMS, local file, hardware wallet). The
/// attestation layer never sees the private key directly.
/// </summary>
public interface IIssuerSigner
{
    /// <summary>The signature algorithm identifier, e.g. <c>"ed25519"</c>.</summary>
    string Algorithm { get; }

    /// <summary>Sign the canonical attestation signing input.</summary>
    byte[] Sign(ReadOnlySpan<byte> message);

    /// <summary>The issuer's public verification key, in raw bytes.</summary>
    byte[] PublicKey { get; }
}
