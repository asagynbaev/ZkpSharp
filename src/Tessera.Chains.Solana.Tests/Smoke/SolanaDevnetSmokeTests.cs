using System.Security.Cryptography;
using Tessera.Chains;
using Tessera.Core;

namespace Tessera.Chains.Solana.Tests.Smoke;

/// <summary>
/// Live-network smoke tests against a deployed identity-registry program.
/// Skipped unless <see cref="SolanaSmokeConfig"/> resolves all env vars; see
/// <c>docs/deploying-solana.md</c> for the deploy + setup steps.
/// </summary>
/// <remarks>
/// <para>
/// Each test creates a fresh DID (random 32-byte hash) so the on-chain PDA is unique
/// per test run — no collision between repeated runs, no cleanup required. Cost per run
/// is a few thousand lamports.
/// </para>
/// <para>
/// Marked <see cref="CollectionAttribute"/> "SolanaDevnet" so xunit runs them serially:
/// hitting the same RPC in parallel triggers rate limiting and produces flaky failures
/// unrelated to the code under test.
/// </para>
/// </remarks>
[Collection("SolanaDevnet")]
public class SolanaDevnetSmokeTests
{
    [SkippableFact]
    public async Task AnchorRoot_RegistersFreshDid()
    {
        var anchor = SkipOrBuild();
        var did = NewRandomDid();
        var root = RandomNumberGenerator.GetBytes(32);

        var result = await anchor.AnchorRootAsync(did, root);

        Assert.False(string.IsNullOrEmpty(result.TxId), "expected non-empty tx signature");

        var state = await anchor.GetAnchorAsync(did);
        Assert.NotNull(state);
        Assert.Equal(root, state.AttestationRoot);
        Assert.Equal(0UL, state.RevocationEpoch);
    }

    [SkippableFact]
    public async Task AnchorRoot_TwiceOnSameDid_UpdatesRoot()
    {
        var anchor = SkipOrBuild();
        var did = NewRandomDid();
        var firstRoot = RandomNumberGenerator.GetBytes(32);
        var secondRoot = RandomNumberGenerator.GetBytes(32);

        await anchor.AnchorRootAsync(did, firstRoot);
        await anchor.AnchorRootAsync(did, secondRoot);

        var state = await anchor.GetAnchorAsync(did);
        Assert.NotNull(state);
        Assert.Equal(secondRoot, state.AttestationRoot);
        Assert.Equal(0UL, state.RevocationEpoch);  // bumping happens via BumpRevocation, not update_root
    }

    [SkippableFact]
    public async Task BumpRevocation_IncrementsEpoch()
    {
        var anchor = SkipOrBuild();
        var did = NewRandomDid();
        await anchor.AnchorRootAsync(did, RandomNumberGenerator.GetBytes(32));

        await anchor.BumpRevocationAsync(did, RevocationReason.HolderRequested);

        var state = await anchor.GetAnchorAsync(did);
        Assert.NotNull(state);
        Assert.Equal(1UL, state.RevocationEpoch);

        await anchor.BumpRevocationAsync(did, RevocationReason.KeyRotation);
        state = await anchor.GetAnchorAsync(did);
        Assert.Equal(2UL, state!.RevocationEpoch);
    }

    [SkippableFact]
    public async Task GetAnchor_UnknownDid_ReturnsNull()
    {
        var anchor = SkipOrBuild();
        var did = NewRandomDid();   // never anchored
        var state = await anchor.GetAnchorAsync(did);
        Assert.Null(state);
    }

    [SkippableFact]
    public async Task IsRevokedSince_TracksEpoch()
    {
        var anchor = SkipOrBuild();
        var did = NewRandomDid();
        await anchor.AnchorRootAsync(did, RandomNumberGenerator.GetBytes(32));

        Assert.False(await anchor.IsRevokedSinceAsync(did, 0));

        await anchor.BumpRevocationAsync(did, RevocationReason.HolderRequested);
        Assert.True(await anchor.IsRevokedSinceAsync(did, 0));
        Assert.False(await anchor.IsRevokedSinceAsync(did, 1));   // current epoch, not stale
    }

    // ── helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve config from env or skip. Returns a configured <see cref="SolanaChainAnchor"/>
    /// when env is set; throws <c>SkipException</c> otherwise so xunit marks the test as Skipped.
    /// </summary>
    private static SolanaChainAnchor SkipOrBuild()
    {
        Skip.IfNot(
            SolanaSmokeConfig.TryLoad(out var config, out var reason),
            reason);

        return new SolanaChainAnchor(
            rpcUrl: config!.RpcUrl,
            programId: config.ProgramId,
            payerKeypair: config.PayerKeypair);
    }

    /// <summary>
    /// Build a DID whose SHA-256 (= on-chain did_hash) is unique per test run, so each
    /// test gets a fresh PDA and tests don't collide across re-runs.
    /// </summary>
    private static DidId NewRandomDid()
    {
        Span<byte> entropy = stackalloc byte[16];
        RandomNumberGenerator.Fill(entropy);
        return new DidId("did:tessera:smoke-" + Convert.ToHexString(entropy));
    }
}
