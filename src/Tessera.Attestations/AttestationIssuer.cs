namespace Tessera.Attestations;

using System.Security.Cryptography;
using Tessera.Core;

/// <summary>
/// Issues attestations on behalf of a single issuer DID. Stateless aside from
/// the injected signer.
/// </summary>
public sealed class AttestationIssuer
{
    private readonly DidId _issuerDid;
    private readonly IIssuerSigner _signer;
    private readonly TimeProvider _clock;

    public AttestationIssuer(DidId issuerDid, IIssuerSigner signer, TimeProvider? clock = null)
    {
        _issuerDid = issuerDid;
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>
    /// Build, sign, and return an attestation envelope. Caller supplies the type,
    /// subject, validity window, and payload. The issuer DID and timestamp are filled in here.
    /// </summary>
    public Attestation Issue(
        string type,
        DidId subject,
        AttestationPayload payload,
        TimeSpan? validity = null,
        string schema = "https://schemas.tessera/attestation/v1")
    {
        ArgumentException.ThrowIfNullOrEmpty(type);
        ArgumentNullException.ThrowIfNull(payload);

        var now = _clock.GetUtcNow();
        var nonce = RandomNumberGenerator.GetBytes(16);
        var draft = new Attestation
        {
            Schema = schema,
            Type = type,
            Issuer = _issuerDid,
            Subject = subject,
            IssuedAt = now,
            ExpiresAt = validity is { } v ? now.Add(v) : null,
            Nonce = nonce,
            Payload = payload,
            // Placeholder signature; replaced below.
            Signature = new AttestationSignature
            {
                Algorithm = _signer.Algorithm,
                Value = Array.Empty<byte>(),
            },
        };

        var input = AttestationCanonical.BuildSigningInput(draft);
        var sig = _signer.Sign(input);
        return draft with
        {
            Signature = new AttestationSignature
            {
                Algorithm = _signer.Algorithm,
                Value = sig,
            },
        };
    }
}
