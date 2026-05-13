using System.Text;
using Tessera.Attestations;
using Tessera.Signing;

namespace Tessera.Signing.Tests;

public class Ed25519IssuerSignerTests
{
    [Fact]
    public void Constructor_ExposesDerivedPublicKey()
    {
        var (priv, expectedPub) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);

        Assert.Equal("ed25519", signer.Algorithm);
        Assert.Equal(expectedPub, signer.PublicKey);
    }

    [Fact]
    public void Constructor_InvalidKeySize_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Ed25519IssuerSigner(new byte[31]));
        Assert.Throws<ArgumentException>(() => new Ed25519IssuerSigner(new byte[33]));
    }

    [Fact]
    public void Sign_RoundTripVerifies()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);
        var msg = Encoding.UTF8.GetBytes("attestation-canonical-bytes");

        var sig = signer.Sign(msg);
        Assert.True(Ed25519.Verify(pub, msg, sig));
    }

    [Fact]
    public void Sign_AsInterface_WorksWithVerifier()
    {
        // Production contract: signer plugs into AttestationIssuer (via IIssuerSigner)
        // and the resulting signature verifies through Ed25519Verifier (via ISignatureVerifier).
        var (priv, _) = Ed25519.GenerateKeypair();
        IIssuerSigner signer = new Ed25519IssuerSigner(priv);
        var verifier = new Ed25519Verifier();
        var msg = Encoding.UTF8.GetBytes("interface-flow");

        var sig = signer.Sign(msg);

        Assert.True(verifier.Verify(signer.PublicKey, msg, sig));
    }

    [Fact]
    public void Generate_ReturnsBoundSignerAndMatchingPublic()
    {
        var (signer, pub) = Ed25519IssuerSigner.Generate();
        using (signer)
        {
            Assert.Equal(pub, signer.PublicKey);

            var msg = Encoding.UTF8.GetBytes("generated");
            var sig = signer.Sign(msg);
            Assert.True(Ed25519.Verify(pub, msg, sig));
        }
    }

    [Fact]
    public void Sign_AfterDispose_Throws()
    {
        var (priv, _) = Ed25519.GenerateKeypair();
        var signer = new Ed25519IssuerSigner(priv);
        signer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => signer.Sign(new byte[16]));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var (priv, _) = Ed25519.GenerateKeypair();
        var signer = new Ed25519IssuerSigner(priv);
        signer.Dispose();
        signer.Dispose(); // must not throw
    }

    [Fact]
    public void Constructor_CopiesPrivateKey_CallerCanZeroSource()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);

        // Caller wipes their copy; signer must still work from its internal copy.
        Array.Clear(priv);

        var msg = Encoding.UTF8.GetBytes("after-wipe");
        var sig = signer.Sign(msg);
        Assert.True(Ed25519.Verify(pub, msg, sig));
    }
}
