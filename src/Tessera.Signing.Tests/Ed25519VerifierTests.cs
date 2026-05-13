using System.Text;
using Tessera.Signing;

namespace Tessera.Signing.Tests;

public class Ed25519VerifierTests
{
    [Fact]
    public void Verifier_ImplementsDidInterface()
    {
        Tessera.Did.ISignatureVerifier verifier = new Ed25519Verifier();
        var (priv, pub) = Ed25519.GenerateKeypair();
        var msg = Encoding.UTF8.GetBytes("did-test");
        var sig = Ed25519.Sign(priv, msg);

        Assert.True(verifier.Verify(pub, msg, sig));
    }

    [Fact]
    public void Verifier_ImplementsAttestationsInterface()
    {
        Tessera.Attestations.ISignatureVerifier verifier = new Ed25519Verifier();
        var (priv, pub) = Ed25519.GenerateKeypair();
        var msg = Encoding.UTF8.GetBytes("attestation-test");
        var sig = Ed25519.Sign(priv, msg);

        Assert.True(verifier.Verify(pub, msg, sig));
    }

    [Fact]
    public void Verifier_SameInstance_WorksAsBothInterfaces()
    {
        // Critical contract: one Ed25519Verifier instance can be handed to both
        // DidService and AttestationVerifier without needing two adapters.
        var verifier = new Ed25519Verifier();
        Tessera.Did.ISignatureVerifier asDid = verifier;
        Tessera.Attestations.ISignatureVerifier asAtt = verifier;

        var (priv, pub) = Ed25519.GenerateKeypair();
        var msg = Encoding.UTF8.GetBytes("shared-instance");
        var sig = Ed25519.Sign(priv, msg);

        Assert.True(asDid.Verify(pub, msg, sig));
        Assert.True(asAtt.Verify(pub, msg, sig));
    }

    [Fact]
    public void Verifier_RejectsTamperedSignature()
    {
        var verifier = new Ed25519Verifier();
        var (priv, pub) = Ed25519.GenerateKeypair();
        var msg = Encoding.UTF8.GetBytes("tamper-test");
        var sig = Ed25519.Sign(priv, msg);
        sig[30] ^= 0x01;

        Assert.False(verifier.Verify(pub, msg, sig));
    }
}
