namespace ZkpSharp.Attestations;

using ZkpSharp.Core;

/// <summary>
/// A holder's ordered set of attestations, materialized into a Merkle tree.
/// The root of this tree is what gets anchored on-chain; presentations include
/// the leaf hash plus the inclusion path.
/// </summary>
public sealed class AttestationBundle
{
    private readonly Attestation[] _attestations;
    private readonly byte[][] _leafBytes;
    private readonly byte[] _root;

    public AttestationBundle(IReadOnlyList<Attestation> attestations)
    {
        if (attestations is null || attestations.Count == 0)
            throw new ArgumentException("Bundle must contain at least one attestation.", nameof(attestations));

        _attestations = attestations.ToArray();
        _leafBytes = _attestations.Select(AttestationCanonical.BuildSigningInput).ToArray();
        _root = MerkleTree.ComputeRoot(_leafBytes);
    }

    public byte[] Root => (byte[])_root.Clone();
    public int Count => _attestations.Length;
    public Attestation this[int index] => _attestations[index];

    /// <summary>
    /// Produce a presentation disclosing the attestation at <paramref name="index"/>
    /// to <paramref name="verifier"/>, bound to a session nonce and revocation epoch.
    /// </summary>
    public AttestationDisclosure DisclosureFor(int index)
    {
        var (root, path) = MerkleTree.BuildInclusionProof(_leafBytes, index);
        if (!root.AsSpan().SequenceEqual(_root))
            throw new InvalidOperationException("Internal inconsistency: rebuilt root differs from cached root.");

        return new AttestationDisclosure
        {
            Attestation = _attestations[index],
            MerkleProof = new MerkleInclusionProof
            {
                LeafHash = MerkleTree.HashLeaf(_leafBytes[index]),
                Path = path,
                Root = root,
                LeafIndex = (ulong)index,
            },
        };
    }
}
