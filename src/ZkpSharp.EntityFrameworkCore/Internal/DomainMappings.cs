using ZkpSharp.Attestations;
using ZkpSharp.Core;
using ZkpSharp.Did;
using ZkpSharp.EntityFrameworkCore.Entities;

namespace ZkpSharp.EntityFrameworkCore.Internal;

/// <summary>
/// Conversions between immutable domain records (DidDocument, IssuerRecord) and the
/// mutable POCOs EF Core uses for change tracking. Kept internal so consumers cannot
/// accidentally couple to the persistence shape.
/// </summary>
internal static class DomainMappings
{
    public static DidDocumentEntity ToEntity(DidDocument doc, DidDocumentEntity? existing = null)
    {
        var e = existing ?? new DidDocumentEntity { Id = doc.Id.Value };

        e.Id = doc.Id.Value;
        e.Controller = doc.Controller.Value;
        e.AttestationRoot = doc.AttestationRoot is null ? null : (byte[])doc.AttestationRoot.Clone();
        e.Revoked = doc.Revoked;
        e.Version = doc.Version;
        e.CreatedAt = doc.CreatedAt;
        e.UpdatedAt = doc.UpdatedAt;

        ReplaceList(
            e.VerificationMethods,
            doc.VerificationMethods,
            m => new VerificationMethodEntity
            {
                DidId = doc.Id.Value,
                Id = m.Id,
                Type = m.Type,
                PublicKeyMultibase = m.PublicKeyMultibase,
            });

        ReplaceList(
            e.Wallets,
            doc.Wallets,
            w => new WalletBindingEntity
            {
                DidId = doc.Id.Value,
                Chain = w.Chain,
                Address = w.Address,
                ProofSignature = (byte[])w.ProofSignature.Clone(),
                BoundAt = w.BoundAt,
            });

        ReplaceList(
            e.Bindings,
            doc.Bindings,
            c => new ChannelBindingEntity
            {
                DidId = doc.Id.Value,
                Type = c.Type,
                Commitment = (byte[])c.Commitment.Clone(),
                Issuer = c.Issuer.Value,
                IssuedAt = c.IssuedAt,
                ExpiresAt = c.ExpiresAt,
            });

        return e;
    }

    public static DidDocument ToDomain(DidDocumentEntity e)
    {
        return new DidDocument
        {
            Id = new DidId(e.Id),
            Controller = new DidId(e.Controller),
            AttestationRoot = e.AttestationRoot is null ? null : (byte[])e.AttestationRoot.Clone(),
            Revoked = e.Revoked,
            Version = e.Version,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            VerificationMethods = e.VerificationMethods
                .Select(m => new VerificationMethod
                {
                    Id = m.Id,
                    Type = m.Type,
                    PublicKeyMultibase = m.PublicKeyMultibase,
                })
                .ToArray(),
            Wallets = e.Wallets
                .Select(w => new WalletBinding
                {
                    Chain = w.Chain,
                    Address = w.Address,
                    ProofSignature = (byte[])w.ProofSignature.Clone(),
                    BoundAt = w.BoundAt,
                })
                .ToArray(),
            Bindings = e.Bindings
                .Select(c => new ChannelBinding
                {
                    Type = c.Type,
                    Commitment = (byte[])c.Commitment.Clone(),
                    Issuer = new DidId(c.Issuer),
                    IssuedAt = c.IssuedAt,
                    ExpiresAt = c.ExpiresAt,
                })
                .ToArray(),
        };
    }

    public static IssuerEntity ToEntity(IssuerRecord r, DateTimeOffset now, IssuerEntity? existing = null)
    {
        if (existing is null)
        {
            return new IssuerEntity
            {
                Did = r.Did.Value,
                PublicKey = (byte[])r.PublicKey.Clone(),
                Algorithm = r.Algorithm,
                SchemaUri = r.SchemaUri,
                Active = r.Active,
                CreatedAt = now,
                UpdatedAt = now,
            };
        }

        existing.PublicKey = (byte[])r.PublicKey.Clone();
        existing.Algorithm = r.Algorithm;
        existing.SchemaUri = r.SchemaUri;
        existing.Active = r.Active;
        existing.UpdatedAt = now;
        return existing;
    }

    public static IssuerRecord ToDomain(IssuerEntity e) => new()
    {
        Did = new DidId(e.Did),
        PublicKey = (byte[])e.PublicKey.Clone(),
        Algorithm = e.Algorithm,
        SchemaUri = e.SchemaUri,
        Active = e.Active,
    };

    /// <summary>
    /// Reconcile an EF-tracked child collection with the desired set. Clearing and re-adding
    /// is safe because all child rows are owned by the parent DID and we use cascade-delete.
    /// </summary>
    private static void ReplaceList<TSource, TEntity>(
        List<TEntity> dest,
        IReadOnlyList<TSource> source,
        Func<TSource, TEntity> map)
    {
        dest.Clear();
        for (int i = 0; i < source.Count; i++)
            dest.Add(map(source[i]));
    }
}
