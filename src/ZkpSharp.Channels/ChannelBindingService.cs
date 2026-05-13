using System.Security.Cryptography;
using System.Text;

namespace ZkpSharp.Channels;

/// <summary>
/// Builds and verifies channel-binding commitments. Each commitment is a 32-byte HKDF-SHA256
/// output derived from <c>(pepper, channel_type, normalized_handle)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>HKDF construction</b>:
/// <list type="bullet">
///   <item><c>salt</c> = pepper (at least 32 bytes; held outside this library)</item>
///   <item><c>ikm</c> = UTF-8 bytes of <c>channel_type</c> ‖ 0x00 ‖ normalized_handle</item>
///   <item><c>info</c> = <c>"ZkpSharp/v1/channel-bind"</c> (domain separator across SDK uses)</item>
///   <item><c>L</c> = 32 (output bytes)</item>
/// </list>
/// </para>
/// <para>
/// The null byte between <c>channel_type</c> and <c>handle</c> prevents
/// <c>("phon", "e+15551234")</c> from colliding with <c>("phone", "+15551234")</c>.
/// </para>
/// </remarks>
public sealed class ChannelBindingService
{
    private static readonly byte[] HkdfInfo = Encoding.UTF8.GetBytes("ZkpSharp/v1/channel-bind");

    private readonly IPepperProvider _pepperProvider;
    private readonly IChannelNormalizer _normalizer;

    public ChannelBindingService(IPepperProvider pepperProvider, IChannelNormalizer? normalizer = null)
    {
        _pepperProvider = pepperProvider ?? throw new ArgumentNullException(nameof(pepperProvider));
        _normalizer = normalizer ?? new DefaultChannelNormalizer();
    }

    /// <summary>
    /// Derive the 32-byte commitment for a channel handle. Deterministic given the same
    /// pepper + channel type + handle (after normalisation).
    /// </summary>
    public async ValueTask<byte[]> BuildCommitmentAsync(
        string channelType,
        string handle,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelType);
        ArgumentException.ThrowIfNullOrWhiteSpace(handle);

        var normalized = _normalizer.Normalize(channelType, handle);
        var pepper = await _pepperProvider.GetPepperAsync(ct).ConfigureAwait(false);

        return DeriveCommitment(pepper.Span, channelType, normalized);
    }

    /// <summary>
    /// Constant-time check: does the 32-byte <paramref name="commitment"/> equal the
    /// commitment that would be derived from <paramref name="channelType"/> + <paramref name="handle"/>?
    /// Used issuer-side to confirm a holder still owns a previously-bound channel.
    /// </summary>
    public async ValueTask<bool> MatchesAsync(
        ReadOnlyMemory<byte> commitment,
        string channelType,
        string handle,
        CancellationToken ct = default)
    {
        if (commitment.Length != 32) return false;
        var expected = await BuildCommitmentAsync(channelType, handle, ct).ConfigureAwait(false);
        return CryptographicOperations.FixedTimeEquals(commitment.Span, expected);
    }

    private static byte[] DeriveCommitment(
        ReadOnlySpan<byte> pepper,
        string channelType,
        string normalizedHandle)
    {
        // ikm = utf8(channel_type) || 0x00 || utf8(normalized_handle)
        var typeBytes = Encoding.UTF8.GetBytes(channelType);
        var handleBytes = Encoding.UTF8.GetBytes(normalizedHandle);

        var ikm = new byte[typeBytes.Length + 1 + handleBytes.Length];
        Buffer.BlockCopy(typeBytes, 0, ikm, 0, typeBytes.Length);
        ikm[typeBytes.Length] = 0x00;
        Buffer.BlockCopy(handleBytes, 0, ikm, typeBytes.Length + 1, handleBytes.Length);

        var output = new byte[32];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, output, salt: pepper, info: HkdfInfo);
        return output;
    }
}
