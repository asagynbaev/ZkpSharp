using System.Security.Cryptography;
using ZkpSharp.Core;
using ZkpSharp.Did;

namespace ZkpSharp.Did.Tests;

public class DidServiceTests
{
    /// <summary>
    /// Test keyring: maps public keys to private keys so a single stub verifier can
    /// verify signatures from multiple identities (DID controller AND wallets).
    /// Signatures are SHA-256(privKey || message). Not real Ed25519 — just enough
    /// to exercise the service plumbing.
    /// </summary>
    private sealed class TestKeyring
    {
        private readonly Dictionary<string, byte[]> _privBySerializedPub = new();

        public (byte[] Pub, byte[] Priv) NewKey()
        {
            var priv = RandomNumberGenerator.GetBytes(32);
            var pub = SHA256.HashData(priv);
            _privBySerializedPub[Convert.ToHexString(pub)] = priv;
            return (pub, priv);
        }

        public byte[] Sign(byte[] privateKey, byte[] message)
        {
            var input = new byte[privateKey.Length + message.Length];
            Buffer.BlockCopy(privateKey, 0, input, 0, privateKey.Length);
            Buffer.BlockCopy(message, 0, input, privateKey.Length, message.Length);
            return SHA256.HashData(input);
        }

        public ISignatureVerifier Verifier => new Ed25519SignatureVerifier((pk, msg, sig) =>
        {
            if (!_privBySerializedPub.TryGetValue(Convert.ToHexString(pk), out var priv)) return false;
            var expected = Sign(priv, msg.ToArray());
            return sig.SequenceEqual(expected);
        });
    }

    [Fact]
    public async Task Create_DerivesStableDid()
    {
        var kr = new TestKeyring();
        var (pub, _) = kr.NewKey();
        var svc = new DidService(new InMemoryDidStore(), kr.Verifier);

        var doc = await svc.CreateAsync(pub);
        Assert.True(doc.Id.IsWellFormed);
        Assert.Equal(doc.Id, doc.Controller);
        Assert.Single(doc.VerificationMethods);
        Assert.Equal("Ed25519VerificationKey2020", doc.VerificationMethods[0].Type);
    }

    [Fact]
    public async Task Create_SameKey_FailsBecauseDidAlreadyExists()
    {
        var kr = new TestKeyring();
        var (pub, _) = kr.NewKey();
        var svc = new DidService(new InMemoryDidStore(), kr.Verifier);

        await svc.CreateAsync(pub);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(pub));
    }

    [Fact]
    public async Task Create_RequiresThirtyTwoByteKey()
    {
        var svc = new DidService(new InMemoryDidStore(), new TestKeyring().Verifier);
        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(new byte[16]));
    }

    [Fact]
    public async Task BindWallet_AcceptsValidSignature()
    {
        var kr = new TestKeyring();
        var (didPub, _) = kr.NewKey();
        var (walletPub, walletPriv) = kr.NewKey();
        var svc = new DidService(new InMemoryDidStore(), kr.Verifier);
        var doc = await svc.CreateAsync(didPub);

        var nonce = RandomNumberGenerator.GetBytes(16);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5);

        var unsigned = new WalletBindingRequest
        {
            Chain = "solana",
            Address = "WalletAddrAbc123",
            WalletPublicKey = walletPub,
            Nonce = nonce,
            Expiry = expiry,
            Signature = Array.Empty<byte>(),
        };

        var sig = kr.Sign(walletPriv, DidService.BuildWalletChallenge(doc.Id, unsigned));
        var request = unsigned with { Signature = sig };

        var updated = await svc.BindWalletAsync(doc.Id, request);
        Assert.Single(updated.Wallets);
        Assert.Equal("solana", updated.Wallets[0].Chain);
        Assert.Equal("WalletAddrAbc123", updated.Wallets[0].Address);
    }

    [Fact]
    public async Task BindWallet_RejectsBadSignature()
    {
        var kr = new TestKeyring();
        var (didPub, _) = kr.NewKey();
        var (walletPub, _) = kr.NewKey();
        var svc = new DidService(new InMemoryDidStore(), kr.Verifier);
        var doc = await svc.CreateAsync(didPub);

        var request = new WalletBindingRequest
        {
            Chain = "solana",
            Address = "Wallet1",
            WalletPublicKey = walletPub,
            Nonce = new byte[16],
            Expiry = DateTimeOffset.UtcNow.AddMinutes(5),
            Signature = new byte[32],
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.BindWalletAsync(doc.Id, request));
    }

    [Fact]
    public async Task BindWallet_RejectsExpiredChallenge()
    {
        var kr = new TestKeyring();
        var (didPub, _) = kr.NewKey();
        var (walletPub, _) = kr.NewKey();
        var svc = new DidService(new InMemoryDidStore(), kr.Verifier);
        var doc = await svc.CreateAsync(didPub);

        var request = new WalletBindingRequest
        {
            Chain = "solana",
            Address = "WalletExp",
            WalletPublicKey = walletPub,
            Nonce = new byte[16],
            Expiry = DateTimeOffset.UtcNow.AddMinutes(-1),
            Signature = new byte[32],
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.BindWalletAsync(doc.Id, request));
        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public async Task Revoke_MarksDocumentRevoked()
    {
        var kr = new TestKeyring();
        var (didPub, didPriv) = kr.NewKey();
        var svc = new DidService(new InMemoryDidStore(), kr.Verifier);
        var doc = await svc.CreateAsync(didPub);

        var revokeMsg = DidService.BuildRevokeChallenge(doc.Id, doc.Version);
        var sig = kr.Sign(didPriv, revokeMsg);
        var updated = await svc.RevokeAsync(doc.Id, sig);

        Assert.True(updated.Revoked);
        Assert.Equal(doc.Version + 1, updated.Version);
    }

    [Fact]
    public void Base58_RoundTrip()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0x7F, 0x42 };
        var s = Base58.Encode(data);
        var back = Base58.Decode(s);
        Assert.Equal(data, back);
    }
}
