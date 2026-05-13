using System.Text;
using Tessera.Signing;

namespace Tessera.Signing.Tests;

public class Ed25519Tests
{
    [Fact]
    public void GenerateKeypair_ReturnsCorrectSizes()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        Assert.Equal(Ed25519.PrivateKeySize, priv.Length);
        Assert.Equal(Ed25519.PublicKeySize, pub.Length);
    }

    [Fact]
    public void GenerateKeypair_ProducesDistinctKeys()
    {
        var (priv1, pub1) = Ed25519.GenerateKeypair();
        var (priv2, pub2) = Ed25519.GenerateKeypair();
        Assert.NotEqual(priv1, priv2);
        Assert.NotEqual(pub1, pub2);
    }

    [Fact]
    public void DerivePublicKey_MatchesGeneratedPublic()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        var derived = Ed25519.DerivePublicKey(priv);
        Assert.Equal(pub, derived);
    }

    [Fact]
    public void DerivePublicKey_IsDeterministic()
    {
        var (priv, _) = Ed25519.GenerateKeypair();
        var pub1 = Ed25519.DerivePublicKey(priv);
        var pub2 = Ed25519.DerivePublicKey(priv);
        Assert.Equal(pub1, pub2);
    }

    [Fact]
    public void SignVerify_RoundTrip()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        var message = Encoding.UTF8.GetBytes("hello, world");

        var sig = Ed25519.Sign(priv, message);
        Assert.Equal(Ed25519.SignatureSize, sig.Length);
        Assert.True(Ed25519.Verify(pub, message, sig));
    }

    [Fact]
    public void Verify_TamperedMessage_Fails()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        var message = Encoding.UTF8.GetBytes("hello, world");
        var sig = Ed25519.Sign(priv, message);

        var tampered = (byte[])message.Clone();
        tampered[0] ^= 0xFF;
        Assert.False(Ed25519.Verify(pub, tampered, sig));
    }

    [Fact]
    public void Verify_TamperedSignature_Fails()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        var message = Encoding.UTF8.GetBytes("hello, world");
        var sig = Ed25519.Sign(priv, message);

        sig[0] ^= 0xFF;
        Assert.False(Ed25519.Verify(pub, message, sig));
    }

    [Fact]
    public void Verify_WrongPublicKey_Fails()
    {
        var (priv1, _) = Ed25519.GenerateKeypair();
        var (_, pub2) = Ed25519.GenerateKeypair();
        var message = Encoding.UTF8.GetBytes("hello, world");
        var sig = Ed25519.Sign(priv1, message);

        Assert.False(Ed25519.Verify(pub2, message, sig));
    }

    [Fact]
    public void Verify_MalformedInputs_ReturnsFalse()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var msg = new byte[16];

        Assert.False(Ed25519.Verify(new byte[31], msg, new byte[64])); // pub too short
        Assert.False(Ed25519.Verify(pub, msg, new byte[63]));          // sig too short
        Assert.False(Ed25519.Verify(pub, msg, new byte[65]));          // sig too long
        Assert.False(Ed25519.Verify(new byte[33], msg, new byte[64])); // pub too long
    }

    [Fact]
    public void Sign_InvalidPrivateKeySize_Throws()
    {
        var msg = new byte[16];
        Assert.Throws<ArgumentException>(() => Ed25519.Sign(new byte[31], msg));
        Assert.Throws<ArgumentException>(() => Ed25519.Sign(new byte[33], msg));
    }

    [Fact]
    public void DerivePublicKey_InvalidPrivateKeySize_Throws()
    {
        Assert.Throws<ArgumentException>(() => Ed25519.DerivePublicKey(new byte[31]));
        Assert.Throws<ArgumentException>(() => Ed25519.DerivePublicKey(new byte[33]));
    }

    /// <summary>
    /// RFC 8032 §7.1 test vector 1 — proves the implementation is interoperable with
    /// the spec, not just self-consistent. Empty message, known seed.
    /// </summary>
    [Fact]
    public void Rfc8032_TestVector1_EmptyMessage()
    {
        var priv = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
        var expectedPub = Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");
        var expectedSig = Convert.FromHexString(
            "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b");

        var derivedPub = Ed25519.DerivePublicKey(priv);
        Assert.Equal(expectedPub, derivedPub);

        var sig = Ed25519.Sign(priv, ReadOnlySpan<byte>.Empty);
        Assert.Equal(expectedSig, sig);

        Assert.True(Ed25519.Verify(expectedPub, ReadOnlySpan<byte>.Empty, expectedSig));
    }
}
