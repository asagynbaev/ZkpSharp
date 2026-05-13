using NSec.Cryptography;

namespace Tessera.Signing;

/// <summary>
/// Low-level Ed25519 primitives backed by NSec.Cryptography (libsodium).
/// Produces and verifies standard 64-byte Ed25519 signatures over 32-byte raw keys.
/// </summary>
/// <remarks>
/// Key format conventions used across Tessera:
/// <list type="bullet">
///   <item><b>Private key</b>: 32-byte seed (RawPrivateKey in NSec). NOT the 64-byte expanded form some libraries use.</item>
///   <item><b>Public key</b>: 32-byte raw Ed25519 point compression.</item>
///   <item><b>Signature</b>: 64 bytes (R ‖ S).</item>
/// </list>
/// </remarks>
public static class Ed25519
{
    private static readonly SignatureAlgorithm Algo = SignatureAlgorithm.Ed25519;

    /// <summary>Length in bytes of an Ed25519 private key seed.</summary>
    public const int PrivateKeySize = 32;

    /// <summary>Length in bytes of an Ed25519 public key.</summary>
    public const int PublicKeySize = 32;

    /// <summary>Length in bytes of an Ed25519 signature.</summary>
    public const int SignatureSize = 64;

    /// <summary>Generate a fresh Ed25519 keypair (32-byte seed + 32-byte public key).</summary>
    public static (byte[] PrivateKey, byte[] PublicKey) GenerateKeypair()
    {
        using var key = Key.Create(Algo, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport,
        });
        var priv = key.Export(KeyBlobFormat.RawPrivateKey);
        var pub = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        return (priv, pub);
    }

    /// <summary>Derive the public key from an Ed25519 private key seed.</summary>
    /// <exception cref="ArgumentException">If <paramref name="privateKey"/> is not 32 bytes.</exception>
    public static byte[] DerivePublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != PrivateKeySize)
            throw new ArgumentException(
                $"Ed25519 private key seed must be exactly {PrivateKeySize} bytes (got {privateKey.Length}).",
                nameof(privateKey));

        using var key = Key.Import(Algo, privateKey, KeyBlobFormat.RawPrivateKey);
        return key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
    }

    /// <summary>Sign <paramref name="message"/> with the given private key seed.</summary>
    /// <returns>A 64-byte Ed25519 signature.</returns>
    /// <exception cref="ArgumentException">If <paramref name="privateKey"/> is not 32 bytes.</exception>
    public static byte[] Sign(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> message)
    {
        if (privateKey.Length != PrivateKeySize)
            throw new ArgumentException(
                $"Ed25519 private key seed must be exactly {PrivateKeySize} bytes (got {privateKey.Length}).",
                nameof(privateKey));

        using var key = Key.Import(Algo, privateKey, KeyBlobFormat.RawPrivateKey);
        return Algo.Sign(key, message);
    }

    /// <summary>
    /// Verify <paramref name="signature"/> over <paramref name="message"/> against <paramref name="publicKey"/>.
    /// Returns false for any malformed input rather than throwing — verification is a boolean predicate, not an assertion.
    /// </summary>
    public static bool Verify(
        ReadOnlySpan<byte> publicKey,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> signature)
    {
        if (publicKey.Length != PublicKeySize) return false;
        if (signature.Length != SignatureSize) return false;

        try
        {
            var pub = PublicKey.Import(Algo, publicKey, KeyBlobFormat.RawPublicKey);
            return Algo.Verify(pub, message, signature);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
