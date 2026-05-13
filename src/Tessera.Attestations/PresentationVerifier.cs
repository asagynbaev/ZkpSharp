namespace Tessera.Attestations;

/// <summary>
/// End-to-end verification of a holder's presentation: each disclosed attestation
/// must verify against its issuer, its Merkle inclusion path must hash to the
/// expected root, and (caller-supplied) the root must match what is anchored on-chain
/// for the holder DID at <c>AsOfRevocationEpoch</c>.
/// </summary>
public sealed class PresentationVerifier
{
    private readonly AttestationVerifier _attestationVerifier;

    public PresentationVerifier(AttestationVerifier attestationVerifier)
    {
        _attestationVerifier = attestationVerifier ?? throw new ArgumentNullException(nameof(attestationVerifier));
    }

    /// <summary>
    /// Verify the cryptographic content of a presentation. Caller is responsible for
    /// (a) checking the holder's signature on <see cref="PresentationBinding"/> against
    /// the holder's DID document and (b) confirming <paramref name="expectedAnchorRoot"/>
    /// is the current on-chain root for the holder DID at the presented revocation epoch.
    /// </summary>
    public async Task<VerificationResult> VerifyAsync(
        Presentation presentation,
        ReadOnlyMemory<byte> expectedAnchorRoot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(presentation);

        if (presentation.Disclosures.Count == 0)
            return VerificationResult.Fail("no_disclosures");

        foreach (var disclosure in presentation.Disclosures)
        {
            var sigResult = await _attestationVerifier.VerifyAsync(disclosure.Attestation, ct).ConfigureAwait(false);
            if (!sigResult.Valid) return sigResult;

            if (disclosure.Attestation.Subject != presentation.Holder)
                return VerificationResult.Fail("subject_mismatch");

            var canonical = AttestationCanonical.BuildSigningInput(disclosure.Attestation);
            var expectedLeafHash = MerkleTree.HashLeaf(canonical);
            if (!disclosure.MerkleProof.LeafHash.AsSpan().SequenceEqual(expectedLeafHash))
                return VerificationResult.Fail("leaf_hash_mismatch");

            if (!disclosure.MerkleProof.Root.AsSpan().SequenceEqual(expectedAnchorRoot.Span))
                return VerificationResult.Fail("root_not_anchored");

            if (!MerkleTree.VerifyInclusion(
                    disclosure.MerkleProof.LeafHash,
                    disclosure.MerkleProof.Path,
                    disclosure.MerkleProof.LeafIndex,
                    expectedAnchorRoot.Span))
                return VerificationResult.Fail("merkle_path_invalid");
        }

        return VerificationResult.Ok();
    }
}
