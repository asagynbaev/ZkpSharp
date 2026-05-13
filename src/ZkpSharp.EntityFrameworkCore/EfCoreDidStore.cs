using Microsoft.EntityFrameworkCore;
using ZkpSharp.Core;
using ZkpSharp.Did;
using ZkpSharp.EntityFrameworkCore.Entities;
using ZkpSharp.EntityFrameworkCore.Internal;

namespace ZkpSharp.EntityFrameworkCore;

/// <summary>
/// EF Core <see cref="IDidStore"/>. Persists <see cref="DidDocument"/> aggregates
/// into the normalized schema defined by <see cref="ZkpSharpDbContext"/>.
/// </summary>
/// <remarks>
/// One <see cref="DbContext"/> per request — register both the <c>DbContext</c> and
/// this store as scoped services. The store does not call <c>SaveChangesAsync</c>
/// inside read paths; only <see cref="SaveAsync"/> persists.
/// </remarks>
public sealed class EfCoreDidStore : IDidStore
{
    private readonly ZkpSharpDbContext _db;

    public EfCoreDidStore(ZkpSharpDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<DidDocument?> GetAsync(DidId did, CancellationToken ct = default)
    {
        var entity = await LoadGraphAsync(did, ct).ConfigureAwait(false);
        return entity is null ? null : DomainMappings.ToDomain(entity);
    }

    public async Task SaveAsync(DidDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existing = await LoadGraphAsync(document.Id, ct).ConfigureAwait(false);
        if (existing is null)
        {
            var fresh = DomainMappings.ToEntity(document);
            _db.DidDocuments.Add(fresh);
        }
        else
        {
            DomainMappings.ToEntity(document, existing);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(DidId did, CancellationToken ct = default)
        => await _db.DidDocuments
            .AsNoTracking()
            .AnyAsync(d => d.Id == did.Value, ct)
            .ConfigureAwait(false);

    private Task<DidDocumentEntity?> LoadGraphAsync(DidId did, CancellationToken ct)
        => _db.DidDocuments
            .Include(d => d.VerificationMethods)
            .Include(d => d.Wallets)
            .Include(d => d.Bindings)
            .FirstOrDefaultAsync(d => d.Id == did.Value, ct);
}
