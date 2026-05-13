using Tessera.Attestations;
using Tessera.Core;

namespace Tessera.Sdk;

/// <summary>
/// High-level issuer facade. One instance per issuer DID; signs attestations on demand using
/// the injected <see cref="IIssuerSigner"/>.
/// </summary>
/// <remarks>
/// <para>
/// The issuer must also be registered in an <see cref="IIssuerRegistry"/> that verifiers can
/// reach — this facade only signs; it does not publish the issuer record. Registration is a
/// one-time operational step done out of band (database insert, on-chain registration, etc.).
/// </para>
/// </remarks>
public sealed class Issuer
{
    private readonly AttestationIssuer _inner;
    private readonly IIssuerSigner _signer;

    public Issuer(DidId issuerDid, IIssuerSigner signer, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(signer);
        _signer = signer;
        _inner = new AttestationIssuer(issuerDid, signer, clock);
        Did = issuerDid;
    }

    public DidId Did { get; }

    /// <summary>The issuer's public verification key, raw bytes (algorithm-specific).</summary>
    public byte[] PublicKey => _signer.PublicKey;

    /// <summary>Signature algorithm identifier, e.g. <c>"ed25519"</c>.</summary>
    public string Algorithm => _signer.Algorithm;

    /// <summary>
    /// Issue an attestation for <paramref name="subject"/>. The returned envelope is
    /// fully signed; hand it to the subject (or their wallet) for inclusion in their bundle.
    /// </summary>
    /// <param name="type">Attestation type tag, e.g. <c>AttestationTypes.PhoneVerified</c>.</param>
    /// <param name="subject">The subject DID the attestation is about.</param>
    /// <param name="payload">Issuer-specific claims and optional commitments.</param>
    /// <param name="validity">Optional lifetime; null = no expiry.</param>
    /// <param name="schema">
    /// Schema URI describing the attestation type and payload shape. Defaults to the issuer's
    /// registered schema if not overridden.
    /// </param>
    public Attestation Issue(
        string type,
        DidId subject,
        AttestationPayload payload,
        TimeSpan? validity = null,
        string? schema = null)
        => schema is null
            ? _inner.Issue(type, subject, payload, validity)
            : _inner.Issue(type, subject, payload, validity, schema);

    /// <summary>
    /// Build an <see cref="IssuerRecord"/> suitable for registration in an <see cref="IIssuerRegistry"/>.
    /// Use this to publish the issuer's identity to verifiers.
    /// </summary>
    public IssuerRecord BuildRegistryRecord(string schemaUri, bool active = true) => new()
    {
        Did = Did,
        PublicKey = (byte[])_signer.PublicKey.Clone(),
        Algorithm = _signer.Algorithm,
        SchemaUri = schemaUri,
        Active = active,
    };
}
