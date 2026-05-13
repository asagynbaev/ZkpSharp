using System.Security.Cryptography;
using ZkpSharp.Attestations;
using ZkpSharp.Core;
using ZkpSharp.Did;
using ZkpSharp.Sdk;
using ZkpSharp.Signing;

namespace ZkpSharp.Sdk.Tests;

public class ZkpHolderTests
{
    private static ZkpHolderOptions BuildOptions(IChainAnchorOrNull chain = IChainAnchorOrNull.None)
    {
        return new ZkpHolderOptions
        {
            Store = new InMemoryDidStore(),
            SignatureVerifier = new Ed25519Verifier(),
            ChainAnchor = chain == IChainAnchorOrNull.InMemory ? new InMemoryChainAnchor() : null,
        };
    }

    private enum IChainAnchorOrNull { None, InMemory }

    [Fact]
    public async Task CreateAsync_PersistsDidAndExposesDocument()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var options = BuildOptions();

        var holder = await ZkpHolder.CreateAsync(pub, options);

        Assert.True(holder.Did.IsWellFormed);
        Assert.Equal(holder.Did, holder.Document.Id);
        Assert.False(holder.Document.Revoked);
        Assert.True(await options.Store.ExistsAsync(holder.Did));
    }

    [Fact]
    public async Task LoadAsync_FindsExistingDidAndSeedsAttestations()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var options = BuildOptions();

        var created = await ZkpHolder.CreateAsync(pub, options);
        var didId = created.Did;

        var att = BuildSignedAttestationFor(didId);
        var reloaded = await ZkpHolder.LoadAsync(didId, new[] { att }, options);

        Assert.Equal(didId, reloaded.Did);
        Assert.Single(reloaded.Attestations);
    }

    [Fact]
    public async Task LoadAsync_MissingDid_Throws()
    {
        var options = BuildOptions();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ZkpHolder.LoadAsync(new DidId("did:zkp:not-real"), options));
    }

    [Fact]
    public async Task AcceptAttestation_WrongSubject_Throws()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions());
        var wrong = BuildSignedAttestationFor(new DidId("did:zkp:somebody-else"));

        Assert.Throws<ArgumentException>(() => holder.AcceptAttestation(wrong));
    }

    [Fact]
    public async Task CurrentRoot_IsNullUntilAttestationsExist()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions());
        Assert.Null(holder.CurrentRoot);

        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did));
        Assert.NotNull(holder.CurrentRoot);
        Assert.Equal(32, holder.CurrentRoot!.Length);
    }

    [Fact]
    public async Task AnchorRootAsync_NoChain_Throws()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions(IChainAnchorOrNull.None));
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did));

        await Assert.ThrowsAsync<InvalidOperationException>(() => holder.AnchorRootAsync());
    }

    [Fact]
    public async Task AnchorRootAsync_NoAttestations_Throws()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions(IChainAnchorOrNull.InMemory));
        await Assert.ThrowsAsync<InvalidOperationException>(() => holder.AnchorRootAsync());
    }

    [Fact]
    public async Task AnchorRootAsync_PersistsRootOnChain()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var options = BuildOptions(IChainAnchorOrNull.InMemory);
        var holder = await ZkpHolder.CreateAsync(pub, options);
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did));

        var result = await holder.AnchorRootAsync();
        Assert.StartsWith("test-tx-", result.TxId);

        var state = await holder.GetAnchorAsync();
        Assert.NotNull(state);
        Assert.Equal(holder.CurrentRoot, state.AttestationRoot);
        Assert.Equal(0UL, state.RevocationEpoch);
    }

    [Fact]
    public async Task BuildPresentation_ByIndices_ProducesValidDisclosures()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions());
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "phone_verified"));
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "human_verified"));

        var presentation = holder.BuildPresentation(
            verifier: new DidId("did:zkp:some-app"),
            indices: new[] { 1 },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,
            chain: "solana",
            holderSignature: RandomBytes(64));

        Assert.Equal(holder.Did, presentation.Holder);
        Assert.Single(presentation.Disclosures);
        Assert.Equal("human_verified", presentation.Disclosures[0].Attestation.Type);
    }

    [Fact]
    public async Task BuildPresentation_ByType_PicksMatchingAttestations()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions());
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "phone_verified"));
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "human_verified"));
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "phone_verified"));

        var presentation = holder.BuildPresentation(
            verifier: new DidId("did:zkp:some-app"),
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,
            chain: "solana",
            holderSignature: RandomBytes(64));

        Assert.Equal(2, presentation.Disclosures.Count);
        Assert.All(presentation.Disclosures, d => Assert.Equal("phone_verified", d.Attestation.Type));
    }

    [Fact]
    public async Task BuildPresentation_NoMatchingType_Throws()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions());
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "phone_verified"));

        Assert.Throws<InvalidOperationException>(() => holder.BuildPresentation(
            verifier: new DidId("did:zkp:app"),
            attestationTypes: new[] { "wallet_verified" },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,
            chain: "solana",
            holderSignature: RandomBytes(64)));
    }

    [Fact]
    public async Task BuildPresentation_OutOfRangeIndex_Throws()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions());
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did));

        Assert.Throws<ArgumentOutOfRangeException>(() => holder.BuildPresentation(
            verifier: new DidId("did:zkp:app"),
            indices: new[] { 99 },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,
            chain: "solana",
            holderSignature: RandomBytes(64)));
    }

    [Fact]
    public async Task RemoveAttestation_ShiftsList()
    {
        var (_, pub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(pub, BuildOptions());
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "phone_verified"));
        holder.AcceptAttestation(BuildSignedAttestationFor(holder.Did, type: "human_verified"));

        holder.RemoveAttestation(0);
        Assert.Single(holder.Attestations);
        Assert.Equal("human_verified", holder.Attestations[0].Type);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static byte[] RandomBytes(int n)
    {
        var b = new byte[n];
        RandomNumberGenerator.Fill(b);
        return b;
    }

    /// <summary>Build a syntactically valid attestation (random signer; tests don't verify it).</summary>
    private static Attestation BuildSignedAttestationFor(DidId subject, string type = "phone_verified")
    {
        var (priv, _) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);
        var issuer = new AttestationIssuer(new DidId("did:zkp:test-issuer"), signer);
        return issuer.Issue(type, subject, new AttestationPayload { Method = "test" });
    }
}
