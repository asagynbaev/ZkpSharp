namespace ZkpSharp.Channels;

/// <summary>
/// Source of the secret pepper used by <see cref="ChannelBindingService"/>.
/// The pepper is the only thing standing between a low-entropy identifier (phone, email,
/// Telegram handle) and a brute-force preimage attack on the commitment. Treat it as
/// equivalent to a database master key: never log it, never check it into source, store
/// it in a KMS / Key Vault / Secrets Manager and fetch it at startup.
/// </summary>
/// <remarks>
/// Implementations must return at least 32 bytes of cryptographically random data.
/// The same pepper must be used across all instances that need to compute or compare
/// commitments; rotating the pepper invalidates every previously stored commitment.
/// </remarks>
public interface IPepperProvider
{
    /// <summary>
    /// Return the pepper bytes. Async because production providers typically fetch from
    /// a remote secret store. Implementations should cache aggressively in-memory.
    /// </summary>
    ValueTask<ReadOnlyMemory<byte>> GetPepperAsync(CancellationToken ct = default);
}

/// <summary>
/// In-memory pepper provider. Suitable for tests and offline development.
/// Do NOT use in production — the pepper lives in process memory at construction time
/// and is never refreshed.
/// </summary>
public sealed class StaticPepperProvider : IPepperProvider
{
    private readonly ReadOnlyMemory<byte> _pepper;

    public StaticPepperProvider(ReadOnlyMemory<byte> pepper)
    {
        if (pepper.Length < 32)
            throw new ArgumentException(
                $"Pepper must be at least 32 bytes (got {pepper.Length}).",
                nameof(pepper));
        _pepper = pepper;
    }

    public ValueTask<ReadOnlyMemory<byte>> GetPepperAsync(CancellationToken ct = default)
        => ValueTask.FromResult(_pepper);
}

/// <summary>
/// Pepper from an environment variable. The variable value must be Base64-encoded and
/// decode to at least 32 bytes. Useful for containerised deployments where the secret
/// arrives as an env var injected by the orchestrator.
/// </summary>
public sealed class EnvironmentPepperProvider : IPepperProvider
{
    private readonly Lazy<ReadOnlyMemory<byte>> _pepper;

    public EnvironmentPepperProvider(string environmentVariableName)
    {
        if (string.IsNullOrWhiteSpace(environmentVariableName))
            throw new ArgumentException("environment variable name required.", nameof(environmentVariableName));

        _pepper = new Lazy<ReadOnlyMemory<byte>>(() =>
        {
            var b64 = Environment.GetEnvironmentVariable(environmentVariableName);
            if (string.IsNullOrEmpty(b64))
                throw new InvalidOperationException(
                    $"Environment variable {environmentVariableName} is not set or empty.");

            byte[] bytes;
            try { bytes = Convert.FromBase64String(b64); }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    $"Environment variable {environmentVariableName} is not valid Base64.", ex);
            }

            if (bytes.Length < 32)
                throw new InvalidOperationException(
                    $"Pepper from {environmentVariableName} must decode to at least 32 bytes (got {bytes.Length}).");

            return bytes;
        });
    }

    public ValueTask<ReadOnlyMemory<byte>> GetPepperAsync(CancellationToken ct = default)
        => ValueTask.FromResult(_pepper.Value);
}
