using System.Security.Cryptography;
using Tessera.Channels;
using Tessera.Core;
using Tessera.Did;

namespace Tessera.Channels.Tests;

/// <summary>
/// End-to-end channel binding: issuer-side commits a handle, holder writes the binding
/// to their DID document, issuer-side later verifies the binding is still valid for a
/// re-presented handle.
/// </summary>
public class ChannelBindingFlowTests
{
    [Fact]
    public async Task FullFlow_PhoneBindAndVerify()
    {
        // ── shared services ────────────────────────────────────────────────
        var pepper = RandomNumberGenerator.GetBytes(32);
        var channels = new ChannelBindingService(new StaticPepperProvider(pepper));

        var didService = new DidService(new InMemoryDidStore(), new AlwaysTrueVerifier());

        // ── create a holder DID with a deterministic key (verifier is stubbed) ─
        var pub = new byte[32];
        RandomNumberGenerator.Fill(pub);
        var doc = await didService.CreateAsync(pub);

        // ── issuer-side: build commitment for holder's phone ──────────────
        var phone = "+1 555 010 1234";
        var commitment = await channels.BuildCommitmentAsync(ChannelTypes.Phone, phone);

        // ── holder-side: attach binding to DID document ────────────────────
        var issuerDid = new DidId("did:tessera:phone-verifier-service");
        var updated = await didService.AddChannelBindingAsync(doc.Id, new ChannelBinding
        {
            Type = ChannelTypes.Phone,
            Commitment = commitment,
            Issuer = issuerDid,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddYears(1),
        });

        Assert.Single(updated.Bindings);
        Assert.Equal(ChannelTypes.Phone, updated.Bindings[0].Type);
        Assert.Equal(doc.Version + 1, updated.Version);

        // ── issuer-side: re-presented handle matches the stored commitment ─
        var match = await channels.MatchesAsync(updated.Bindings[0].Commitment, ChannelTypes.Phone, phone);
        Assert.True(match);

        // ── wrong handle does not match ────────────────────────────────────
        var noMatch = await channels.MatchesAsync(updated.Bindings[0].Commitment, ChannelTypes.Phone, "+15550000");
        Assert.False(noMatch);
    }

    [Fact]
    public async Task AddChannelBinding_AllowsMultipleBindingsSameType()
    {
        var pepper = RandomNumberGenerator.GetBytes(32);
        var channels = new ChannelBindingService(new StaticPepperProvider(pepper));
        var didService = new DidService(new InMemoryDidStore(), new AlwaysTrueVerifier());

        var pub = RandomNumberGenerator.GetBytes(32);
        var doc = await didService.CreateAsync(pub);

        var issuerDid = new DidId("did:tessera:phone-issuer");
        await didService.AddChannelBindingAsync(doc.Id, new ChannelBinding
        {
            Type = ChannelTypes.Phone,
            Commitment = await channels.BuildCommitmentAsync(ChannelTypes.Phone, "+15550001"),
            Issuer = issuerDid,
            IssuedAt = DateTimeOffset.UtcNow,
        });

        var updated = await didService.AddChannelBindingAsync(doc.Id, new ChannelBinding
        {
            Type = ChannelTypes.Phone,
            Commitment = await channels.BuildCommitmentAsync(ChannelTypes.Phone, "+15550002"),
            Issuer = issuerDid,
            IssuedAt = DateTimeOffset.UtcNow,
        });

        Assert.Equal(2, updated.Bindings.Count);
    }

    [Fact]
    public async Task RemoveChannelBinding_RemovesByTypeAndCommitment()
    {
        var pepper = RandomNumberGenerator.GetBytes(32);
        var channels = new ChannelBindingService(new StaticPepperProvider(pepper));
        var didService = new DidService(new InMemoryDidStore(), new AlwaysTrueVerifier());

        var pub = RandomNumberGenerator.GetBytes(32);
        var doc = await didService.CreateAsync(pub);

        var commit1 = await channels.BuildCommitmentAsync(ChannelTypes.Phone, "+15550001");
        var commit2 = await channels.BuildCommitmentAsync(ChannelTypes.Phone, "+15550002");

        await didService.AddChannelBindingAsync(doc.Id, new ChannelBinding
        {
            Type = ChannelTypes.Phone,
            Commitment = commit1,
            Issuer = new DidId("did:tessera:issuer"),
            IssuedAt = DateTimeOffset.UtcNow,
        });
        await didService.AddChannelBindingAsync(doc.Id, new ChannelBinding
        {
            Type = ChannelTypes.Phone,
            Commitment = commit2,
            Issuer = new DidId("did:tessera:issuer"),
            IssuedAt = DateTimeOffset.UtcNow,
        });

        var removed = await didService.RemoveChannelBindingAsync(doc.Id, ChannelTypes.Phone, commit1);
        Assert.Single(removed.Bindings);
        Assert.Equal(commit2, removed.Bindings[0].Commitment);
    }

    [Fact]
    public async Task AddChannelBinding_OnRevokedDid_Throws()
    {
        var didService = new DidService(new InMemoryDidStore(), new AlwaysTrueVerifier());
        var pub = RandomNumberGenerator.GetBytes(32);
        var doc = await didService.CreateAsync(pub);
        await didService.RevokeAsync(doc.Id, new byte[64]);  // verifier is stubbed; any sig "verifies"

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            didService.AddChannelBindingAsync(doc.Id, new ChannelBinding
            {
                Type = ChannelTypes.Email,
                Commitment = new byte[32],
                Issuer = new DidId("did:tessera:any"),
                IssuedAt = DateTimeOffset.UtcNow,
            }));
    }

    [Fact]
    public async Task AddChannelBinding_EmptyCommitment_Throws()
    {
        var didService = new DidService(new InMemoryDidStore(), new AlwaysTrueVerifier());
        var pub = RandomNumberGenerator.GetBytes(32);
        var doc = await didService.CreateAsync(pub);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            didService.AddChannelBindingAsync(doc.Id, new ChannelBinding
            {
                Type = ChannelTypes.Email,
                Commitment = Array.Empty<byte>(),
                Issuer = new DidId("did:tessera:issuer"),
                IssuedAt = DateTimeOffset.UtcNow,
            }));
    }

    /// <summary>Test stub: accepts every signature so we can exercise DID/channel flow without a real signer.</summary>
    private sealed class AlwaysTrueVerifier : ISignatureVerifier
    {
        public bool Verify(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature) => true;
    }
}
