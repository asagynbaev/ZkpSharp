using Solnet.Wallet;
using Tessera.Chains.Solana.Internal;

namespace Tessera.Chains.Solana.Tests;

public class PdaTests
{
    // Arbitrary valid program ID for tests. Real deployment will replace this.
    // (The Rust declare_id! placeholder "ZkpId1..." contains 'I' which is not base58.)
    private static readonly PublicKey TestProgram = new("11111111111111111111111111111114");

    [Fact]
    public void DidAnchor_IsDeterministic()
    {
        var didHash = new byte[32];
        for (int i = 0; i < 32; i++) didHash[i] = (byte)i;

        var (pda1, bump1) = IdentityRegistryPdas.DidAnchor(TestProgram, didHash);
        var (pda2, bump2) = IdentityRegistryPdas.DidAnchor(TestProgram, didHash);

        Assert.Equal(pda1.Key, pda2.Key);
        Assert.Equal(bump1, bump2);
    }

    [Fact]
    public void DidAnchor_DifferentHashes_ProduceDifferentPdas()
    {
        var h1 = new byte[32];
        var h2 = new byte[32];
        h2[0] = 1;

        var (pda1, _) = IdentityRegistryPdas.DidAnchor(TestProgram, h1);
        var (pda2, _) = IdentityRegistryPdas.DidAnchor(TestProgram, h2);

        Assert.NotEqual(pda1.Key, pda2.Key);
    }

    [Fact]
    public void Issuer_IsDeterministic()
    {
        var hash = new byte[32];
        for (int i = 0; i < 32; i++) hash[i] = (byte)(i + 100);

        var (pda1, _) = IdentityRegistryPdas.Issuer(TestProgram, hash);
        var (pda2, _) = IdentityRegistryPdas.Issuer(TestProgram, hash);
        Assert.Equal(pda1.Key, pda2.Key);
    }

    [Fact]
    public void DidAnchor_AndIssuer_WithSameHash_ProduceDifferentPdas()
    {
        // Same did_hash bytes but different seeds ("did" vs "issuer") MUST yield different PDAs,
        // otherwise the program could collide DID anchors with issuer accounts.
        var hash = new byte[32];
        for (int i = 0; i < 32; i++) hash[i] = (byte)(i * 7);

        var (didPda, _) = IdentityRegistryPdas.DidAnchor(TestProgram, hash);
        var (issuerPda, _) = IdentityRegistryPdas.Issuer(TestProgram, hash);
        Assert.NotEqual(didPda.Key, issuerPda.Key);
    }

    [Fact]
    public void DidAnchor_WrongLengthHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => IdentityRegistryPdas.DidAnchor(TestProgram, new byte[31]));
        Assert.Throws<ArgumentException>(() => IdentityRegistryPdas.DidAnchor(TestProgram, new byte[33]));
    }

    [Fact]
    public void Issuer_WrongLengthHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => IdentityRegistryPdas.Issuer(TestProgram, new byte[16]));
    }
}
