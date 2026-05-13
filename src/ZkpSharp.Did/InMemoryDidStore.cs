namespace ZkpSharp.Did;

using System.Collections.Concurrent;
using ZkpSharp.Core;

/// <summary>
/// In-memory <see cref="IDidStore"/> for tests and local development. Thread-safe.
/// Do not use in production: state is lost on process restart.
/// </summary>
public sealed class InMemoryDidStore : IDidStore
{
    private readonly ConcurrentDictionary<DidId, DidDocument> _docs = new();

    public Task<DidDocument?> GetAsync(DidId did, CancellationToken ct = default)
        => Task.FromResult(_docs.TryGetValue(did, out var doc) ? doc : null);

    public Task SaveAsync(DidDocument document, CancellationToken ct = default)
    {
        _docs[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(DidId did, CancellationToken ct = default)
        => Task.FromResult(_docs.ContainsKey(did));
}
