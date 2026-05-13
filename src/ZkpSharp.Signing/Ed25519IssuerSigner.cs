using ZkpSharp.Attestations;

namespace ZkpSharp.Signing;

/// <summary>
/// Production Ed25519 issuer signer. Holds the 32-byte private key seed in-memory
/// and signs attestation canonical inputs on demand.
/// </summary>
/// <remarks>
/// For HSM/KMS-backed issuers, implement <see cref="IIssuerSigner"/> directly against
/// the remote signing API instead of using this class. This class is suitable for
/// development, tests, and trusted-process deployments where the private key lives
/// in the same process as the issuer service.
/// </remarks>
public sealed class Ed25519IssuerSigner : IIssuerSigner, IDisposable
{
    private readonly byte[] _privateKey;
    private bool _disposed;

    /// <param name="privateKey">32-byte Ed25519 private key seed. Copied internally.</param>
    /// <exception cref="ArgumentException">If the seed length is not 32.</exception>
    public Ed25519IssuerSigner(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != Ed25519.PrivateKeySize)
            throw new ArgumentException(
                $"Ed25519 private key seed must be exactly {Ed25519.PrivateKeySize} bytes (got {privateKey.Length}).",
                nameof(privateKey));

        _privateKey = privateKey.ToArray();
        PublicKey = Ed25519.DerivePublicKey(privateKey);
    }

    public string Algorithm => "ed25519";

    public byte[] PublicKey { get; }

    public byte[] Sign(ReadOnlySpan<byte> message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Ed25519.Sign(_privateKey, message);
    }

    /// <summary>Generate a fresh keypair and return a signer bound to it, along with the public key.</summary>
    public static (Ed25519IssuerSigner Signer, byte[] PublicKey) Generate()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        return (new Ed25519IssuerSigner(priv), pub);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Array.Clear(_privateKey);
        _disposed = true;
    }
}
