using System.Security.Cryptography;
using ZkpSharp.Attestations;
using ZkpSharp.Core;
using ZkpSharp.Sdk;
using ZkpSharp.Signing;

namespace ZkpSharp.Sdk.Tests;

/// <summary>
/// Full SDK flow with real cryptography and an in-memory chain anchor: issuer onboards,
/// holder creates DID, accepts attestation, anchors root, builds presentation, verifier
/// validates against policy. If any of these break, the SDK is not usable end-to-end
/// even when each unit test passes.
/// </summary>
public class EndToEndFlowTests
{
    [Fact]
    public async Task FullFlow_HolderToIssuerToVerifier_Succeeds()
    {
        // ── shared infrastructure ───────────────────────────────────────────
        var verifier = new Ed25519Verifier();
        var registry = new InMemoryIssuerRegistry();
        var chain = new InMemoryChainAnchor();

        // ── issuer side ─────────────────────────────────────────────────────
        var (issuerPriv, _) = Ed25519.GenerateKeypair();
        using var issuerSigner = new Ed25519IssuerSigner(issuerPriv);
        var issuer = new ZkpIssuer(new DidId("did:zkp:issuer-app"), issuerSigner);

        registry.Register(issuer.BuildRegistryRecord(schemaUri: "https://schemas.zkp/attestation/v1"));

        // ── holder side ─────────────────────────────────────────────────────
        var (_, holderPub) = Ed25519.GenerateKeypair();
        var holderOpts = new ZkpHolderOptions
        {
            Store = new ZkpSharp.Did.InMemoryDidStore(),
            SignatureVerifier = verifier,
            ChainAnchor = chain,
        };
        var holder = await ZkpHolder.CreateAsync(holderPub, holderOpts);

        // issuer issues an attestation FOR the holder
        var att = issuer.Issue("phone_verified", holder.Did, new AttestationPayload { Method = "test_carrier" });
        holder.AcceptAttestation(att);

        // holder anchors the resulting root on-chain
        await holder.AnchorRootAsync();

        // ── verifier side ───────────────────────────────────────────────────
        var verifierDid = new DidId("did:zkp:my-relying-app");
        var sessionNonce = RandomBytes(16);
        var presentation = holder.BuildPresentation(
            verifier: verifierDid,
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: sessionNonce,
            asOfRevocationEpoch: 0,
            chain: "test",
            holderSignature: RandomBytes(64));

        var zkpVerifier = new ZkpVerifier(new ZkpVerifierOptions
        {
            IssuerRegistry = registry,
            SignatureVerifier = verifier,
            ChainAnchor = chain,
        });

        var result = await zkpVerifier.VerifyPresentationAsync(presentation, new VerificationPolicy
        {
            ExpectedVerifier = verifierDid,
            ExpectedSessionNonce = sessionNonce,
            RequireCurrentRevocationEpoch = true,
        });

        Assert.True(result.Valid, $"presentation rejected: {result.Reason}");
    }

    [Fact]
    public async Task Verifier_VerifierMismatch_Fails()
    {
        var (holder, _, chain, issuer, registry, verifier) = await BuildPair();
        var att = issuer.Issue("phone_verified", holder.Did, new AttestationPayload { Method = "x" });
        holder.AcceptAttestation(att);
        await holder.AnchorRootAsync();

        var nonce = RandomBytes(16);
        var presentation = holder.BuildPresentation(
            verifier: new DidId("did:zkp:app-A"),
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: nonce,
            asOfRevocationEpoch: 0,
            chain: "test",
            holderSignature: RandomBytes(64));

        var zkpVerifier = new ZkpVerifier(new ZkpVerifierOptions
        {
            IssuerRegistry = registry,
            SignatureVerifier = verifier,
            ChainAnchor = chain,
        });

        var result = await zkpVerifier.VerifyPresentationAsync(presentation, new VerificationPolicy
        {
            ExpectedVerifier = new DidId("did:zkp:app-B"),  // wrong audience
            ExpectedSessionNonce = nonce,
        });

        Assert.False(result.Valid);
        Assert.Equal("verifier_mismatch", result.Reason);
    }

    [Fact]
    public async Task Verifier_SessionNonceMismatch_Fails()
    {
        var (holder, _, chain, issuer, registry, verifier) = await BuildPair();
        holder.AcceptAttestation(issuer.Issue("phone_verified", holder.Did, new AttestationPayload { Method = "x" }));
        await holder.AnchorRootAsync();

        var verifierDid = new DidId("did:zkp:app");
        var presentationNonce = RandomBytes(16);
        var presentation = holder.BuildPresentation(
            verifier: verifierDid,
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: presentationNonce,
            asOfRevocationEpoch: 0,
            chain: "test",
            holderSignature: RandomBytes(64));

        var zkpVerifier = new ZkpVerifier(new ZkpVerifierOptions
        {
            IssuerRegistry = registry,
            SignatureVerifier = verifier,
            ChainAnchor = chain,
        });

        var result = await zkpVerifier.VerifyPresentationAsync(presentation, new VerificationPolicy
        {
            ExpectedVerifier = verifierDid,
            ExpectedSessionNonce = RandomBytes(16),  // expected something else
        });

        Assert.False(result.Valid);
        Assert.Equal("session_nonce_mismatch", result.Reason);
    }

    [Fact]
    public async Task Verifier_RevocationStale_FailsWhenPolicyDemands()
    {
        var (holder, _, chain, issuer, registry, verifier) = await BuildPair();
        holder.AcceptAttestation(issuer.Issue("phone_verified", holder.Did, new AttestationPayload { Method = "x" }));
        await holder.AnchorRootAsync();

        var verifierDid = new DidId("did:zkp:app");
        var presentation = holder.BuildPresentation(
            verifier: verifierDid,
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,  // bound to epoch 0
            chain: "test",
            holderSignature: RandomBytes(64));

        // simulate the on-chain epoch advancing past the presentation's snapshot
        await chain.BumpRevocationAsync(holder.Did, ZkpSharp.Chains.RevocationReason.HolderRequested);

        var zkpVerifier = new ZkpVerifier(new ZkpVerifierOptions
        {
            IssuerRegistry = registry,
            SignatureVerifier = verifier,
            ChainAnchor = chain,
        });

        var result = await zkpVerifier.VerifyPresentationAsync(presentation, new VerificationPolicy
        {
            ExpectedVerifier = verifierDid,
            RequireCurrentRevocationEpoch = true,
        });

        Assert.False(result.Valid);
        Assert.Equal("revocation_stale", result.Reason);
    }

    [Fact]
    public async Task Verifier_NoAnchorOnChain_Fails()
    {
        var (holder, _, chain, issuer, registry, verifier) = await BuildPair();
        // attestation accepted but NEVER anchored
        holder.AcceptAttestation(issuer.Issue("phone_verified", holder.Did, new AttestationPayload { Method = "x" }));

        var verifierDid = new DidId("did:zkp:app");
        var presentation = holder.BuildPresentation(
            verifier: verifierDid,
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,
            chain: "test",
            holderSignature: RandomBytes(64));

        var zkpVerifier = new ZkpVerifier(new ZkpVerifierOptions
        {
            IssuerRegistry = registry,
            SignatureVerifier = verifier,
            ChainAnchor = chain,
        });

        var result = await zkpVerifier.VerifyPresentationAsync(presentation, new VerificationPolicy
        {
            ExpectedVerifier = verifierDid,
        });

        Assert.False(result.Valid);
        Assert.Equal("no_anchored_root", result.Reason);
    }

    [Fact]
    public async Task Verifier_WithCallerSuppliedRoot_BypassesChainLookup()
    {
        var (holder, _, _, issuer, registry, verifier) = await BuildPair();
        holder.AcceptAttestation(issuer.Issue("phone_verified", holder.Did, new AttestationPayload { Method = "x" }));

        var verifierDid = new DidId("did:zkp:app");
        var presentation = holder.BuildPresentation(
            verifier: verifierDid,
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,
            chain: "test",
            holderSignature: RandomBytes(64));

        // No chain anchor configured — caller supplies the expected root directly.
        var zkpVerifier = new ZkpVerifier(new ZkpVerifierOptions
        {
            IssuerRegistry = registry,
            SignatureVerifier = verifier,
            ChainAnchor = null,
        });

        var result = await zkpVerifier.VerifyPresentationAsync(presentation, new VerificationPolicy
        {
            ExpectedVerifier = verifierDid,
            ExpectedAnchorRoot = holder.CurrentRoot,
        });

        Assert.True(result.Valid, $"failed: {result.Reason}");
    }

    [Fact]
    public async Task Verifier_NoChainAndNoRoot_Throws()
    {
        var (holder, _, _, issuer, registry, verifier) = await BuildPair();
        holder.AcceptAttestation(issuer.Issue("phone_verified", holder.Did, new AttestationPayload { Method = "x" }));

        var presentation = holder.BuildPresentation(
            verifier: new DidId("did:zkp:app"),
            attestationTypes: new[] { "phone_verified" },
            sessionNonce: RandomBytes(16),
            asOfRevocationEpoch: 0,
            chain: "test",
            holderSignature: RandomBytes(64));

        var zkpVerifier = new ZkpVerifier(new ZkpVerifierOptions
        {
            IssuerRegistry = registry,
            SignatureVerifier = verifier,
            ChainAnchor = null,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            zkpVerifier.VerifyPresentationAsync(presentation, new VerificationPolicy
            {
                ExpectedVerifier = new DidId("did:zkp:app"),
            }));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private record FlowFixture(
        ZkpHolder Holder,
        byte[] HolderPub,
        InMemoryChainAnchor Chain,
        ZkpIssuer Issuer,
        InMemoryIssuerRegistry Registry,
        Ed25519Verifier SignatureVerifier);

    private static async Task<FlowFixture> BuildPair()
    {
        var verifier = new Ed25519Verifier();
        var registry = new InMemoryIssuerRegistry();
        var chain = new InMemoryChainAnchor();

        var (issuerPriv, _) = Ed25519.GenerateKeypair();
        var issuerSigner = new Ed25519IssuerSigner(issuerPriv);
        var issuer = new ZkpIssuer(new DidId("did:zkp:flow-issuer"), issuerSigner);
        registry.Register(issuer.BuildRegistryRecord(schemaUri: "https://schemas.zkp/attestation/v1"));

        var (_, holderPub) = Ed25519.GenerateKeypair();
        var holder = await ZkpHolder.CreateAsync(holderPub, new ZkpHolderOptions
        {
            Store = new ZkpSharp.Did.InMemoryDidStore(),
            SignatureVerifier = verifier,
            ChainAnchor = chain,
        });

        return new FlowFixture(holder, holderPub, chain, issuer, registry, verifier);
    }

    private static byte[] RandomBytes(int n)
    {
        var b = new byte[n];
        RandomNumberGenerator.Fill(b);
        return b;
    }
}
