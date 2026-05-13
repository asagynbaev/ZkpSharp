namespace Tessera.Attestations;

using System.Text;
using Tessera.Core;

/// <summary>
/// Canonical byte representation of an attestation, used as the message that the
/// issuer signs and as the leaf hash input for Merkle bundling. The format is
/// deliberately field-ordered and length-prefixed so that two implementations
/// in any language produce the same bytes for the same logical attestation.
/// </summary>
public static class AttestationCanonical
{
    /// <summary>Domain separator. Bump on breaking format changes.</summary>
    public const string DomainSeparator = "Tessera/v1/attestation";

    /// <summary>
    /// Build the canonical byte sequence for an attestation envelope's signing input.
    /// Excludes <see cref="Attestation.Signature"/> (the signature signs everything else).
    /// </summary>
    public static byte[] BuildSigningInput(Attestation a)
    {
        ArgumentNullException.ThrowIfNull(a);
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        w.Write(DomainSeparator);
        w.Write(a.Schema);
        w.Write(a.Type);
        w.Write(a.Issuer.Value);
        w.Write(a.Subject.Value);
        w.Write(a.IssuedAt.ToUnixTimeMilliseconds());
        w.Write(a.ExpiresAt?.ToUnixTimeMilliseconds() ?? 0L);

        w.Write(a.Nonce.Length);
        w.Write(a.Nonce);

        w.Write(a.Payload.Method);
        var commitment = a.Payload.Commitment ?? Array.Empty<byte>();
        w.Write(commitment.Length);
        w.Write(commitment);

        // Claims are serialized as a sorted key/value sequence so that map ordering
        // does not change the canonical bytes.
        var claims = a.Payload.Claims;
        if (claims is null)
        {
            w.Write(0);
        }
        else
        {
            var ordered = claims.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToArray();
            w.Write(ordered.Length);
            foreach (var kv in ordered)
            {
                w.Write(kv.Key);
                w.Write(kv.Value?.ToString() ?? "");
            }
        }
        return ms.ToArray();
    }
}
