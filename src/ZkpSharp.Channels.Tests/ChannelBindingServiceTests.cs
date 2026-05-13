using System.Security.Cryptography;
using ZkpSharp.Channels;

namespace ZkpSharp.Channels.Tests;

public class ChannelBindingServiceTests
{
    private static readonly byte[] TestPepperA = RandomNumberGenerator.GetBytes(32);
    private static readonly byte[] TestPepperB = RandomNumberGenerator.GetBytes(32);

    private static ChannelBindingService BuildService(byte[]? pepper = null)
        => new(new StaticPepperProvider(pepper ?? TestPepperA));

    [Fact]
    public async Task BuildCommitment_IsDeterministic()
    {
        var svc = BuildService();
        var a = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        var b = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        Assert.Equal(a, b);
    }

    [Fact]
    public async Task BuildCommitment_ProducesThirtyTwoBytes()
    {
        var svc = BuildService();
        var c = await svc.BuildCommitmentAsync(ChannelTypes.Email, "foo@example.com");
        Assert.Equal(32, c.Length);
    }

    [Fact]
    public async Task BuildCommitment_DifferentPepper_DifferentCommitment()
    {
        var a = await BuildService(TestPepperA).BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        var b = await BuildService(TestPepperB).BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task BuildCommitment_DifferentHandle_DifferentCommitment()
    {
        var svc = BuildService();
        var a = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        var b = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+15551235");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task BuildCommitment_DifferentChannelType_DifferentCommitment()
    {
        // Critical: ("phone", "alice") and ("email", "alice") must not collide.
        var svc = BuildService();
        var phone = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        var email = await svc.BuildCommitmentAsync(ChannelTypes.Email, "+15551234");  // same string, different type
        Assert.NotEqual(phone, email);
    }

    [Fact]
    public async Task BuildCommitment_AmbiguousBoundary_NoCollision()
    {
        // Reject the attack: ("phon", "e+15551234") must not equal ("phone", "+15551234").
        // The 0x00 separator between type and handle prevents this.
        var svc = BuildService();
        var honest = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        var attack = await svc.BuildCommitmentAsync("phon", "e+15551234");
        Assert.NotEqual(honest, attack);
    }

    [Fact]
    public async Task BuildCommitment_NormalizesPhoneVariants()
    {
        var svc = BuildService();
        var canonical = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+15551234");
        var withSpaces = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "+1 555-1234");
        var noPlus = await svc.BuildCommitmentAsync(ChannelTypes.Phone, "15551234");
        Assert.Equal(canonical, withSpaces);
        Assert.Equal(canonical, noPlus);
    }

    [Fact]
    public async Task BuildCommitment_NormalizesEmailCase()
    {
        var svc = BuildService();
        var lower = await svc.BuildCommitmentAsync(ChannelTypes.Email, "alice@example.com");
        var mixed = await svc.BuildCommitmentAsync(ChannelTypes.Email, "Alice@Example.COM");
        Assert.Equal(lower, mixed);
    }

    [Fact]
    public async Task BuildCommitment_NormalizesTelegramHandle()
    {
        var svc = BuildService();
        var bare = await svc.BuildCommitmentAsync(ChannelTypes.Telegram, "alice");
        var withAt = await svc.BuildCommitmentAsync(ChannelTypes.Telegram, "@Alice");
        Assert.Equal(bare, withAt);
    }

    [Fact]
    public async Task Matches_TrueForOwnCommitment()
    {
        var svc = BuildService();
        var c = await svc.BuildCommitmentAsync(ChannelTypes.Email, "alice@example.com");
        Assert.True(await svc.MatchesAsync(c, ChannelTypes.Email, "alice@example.com"));
    }

    [Fact]
    public async Task Matches_FalseForWrongHandle()
    {
        var svc = BuildService();
        var c = await svc.BuildCommitmentAsync(ChannelTypes.Email, "alice@example.com");
        Assert.False(await svc.MatchesAsync(c, ChannelTypes.Email, "bob@example.com"));
    }

    [Fact]
    public async Task Matches_FalseForWrongLength()
    {
        var svc = BuildService();
        Assert.False(await svc.MatchesAsync(new byte[31], ChannelTypes.Email, "x@x.com"));
        Assert.False(await svc.MatchesAsync(new byte[33], ChannelTypes.Email, "x@x.com"));
    }

    [Fact]
    public async Task Matches_FalseForDifferentPepper()
    {
        var svcA = BuildService(TestPepperA);
        var svcB = BuildService(TestPepperB);

        var aCommit = await svcA.BuildCommitmentAsync(ChannelTypes.Email, "x@y.com");
        Assert.False(await svcB.MatchesAsync(aCommit, ChannelTypes.Email, "x@y.com"));
    }

    [Fact]
    public async Task BuildCommitment_RejectsEmptyHandle()
    {
        var svc = BuildService();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.BuildCommitmentAsync(ChannelTypes.Email, "").AsTask());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.BuildCommitmentAsync(ChannelTypes.Email, "   ").AsTask());
    }
}
