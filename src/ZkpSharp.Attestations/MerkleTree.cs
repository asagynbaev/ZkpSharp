namespace ZkpSharp.Attestations;

using System.Security.Cryptography;

/// <summary>
/// Domain-separated SHA-256 Merkle tree. Used to bundle a holder's set of
/// attestations into a single root that gets anchored on-chain.
/// </summary>
/// <remarks>
/// - Leaf and internal-node hashing use different domain separators (RFC 6962-style)
///   to prevent second-preimage attacks via leaf/node confusion.
/// - Odd-final-pair handling: the unpaired hash is duplicated, which is simple but
///   has known weaknesses (CVE-2012-2459 style). For v1 the holder controls the
///   leaf set, so collision-creation is not a concern — flagged for v2 review.
/// </remarks>
public static class MerkleTree
{
    private const byte LeafTag = 0x00;
    private const byte NodeTag = 0x01;

    public static byte[] HashLeaf(ReadOnlySpan<byte> leaf)
    {
        Span<byte> buf = new byte[1 + leaf.Length];
        buf[0] = LeafTag;
        leaf.CopyTo(buf[1..]);
        return SHA256.HashData(buf);
    }

    public static byte[] HashNode(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        Span<byte> buf = new byte[1 + left.Length + right.Length];
        buf[0] = NodeTag;
        left.CopyTo(buf.Slice(1));
        right.CopyTo(buf.Slice(1 + left.Length));
        return SHA256.HashData(buf);
    }

    /// <summary>Compute the root over an ordered list of leaf byte sequences.</summary>
    public static byte[] ComputeRoot(IReadOnlyList<byte[]> leaves)
    {
        if (leaves is null || leaves.Count == 0)
            throw new ArgumentException("Cannot build a Merkle tree over zero leaves.", nameof(leaves));

        var current = leaves.Select(l => HashLeaf(l.AsSpan())).ToArray();
        while (current.Length > 1)
        {
            var next = new byte[(current.Length + 1) / 2][];
            for (int i = 0; i < current.Length; i += 2)
            {
                var left = current[i];
                var right = i + 1 < current.Length ? current[i + 1] : current[i];
                next[i / 2] = HashNode(left, right);
            }
            current = next;
        }
        return current[0];
    }

    /// <summary>
    /// Compute the root and the inclusion proof for the leaf at <paramref name="targetIndex"/>.
    /// Returns the path as a list of sibling hashes from leaf to root.
    /// </summary>
    public static (byte[] Root, IReadOnlyList<byte[]> Path) BuildInclusionProof(
        IReadOnlyList<byte[]> leaves,
        int targetIndex)
    {
        if (leaves is null || leaves.Count == 0)
            throw new ArgumentException("Cannot build a Merkle tree over zero leaves.", nameof(leaves));
        if (targetIndex < 0 || targetIndex >= leaves.Count)
            throw new ArgumentOutOfRangeException(nameof(targetIndex));

        var current = leaves.Select(l => HashLeaf(l.AsSpan())).ToArray();
        var path = new List<byte[]>();
        int idx = targetIndex;
        while (current.Length > 1)
        {
            int siblingIdx = (idx % 2 == 0) ? Math.Min(idx + 1, current.Length - 1) : idx - 1;
            path.Add(current[siblingIdx]);

            var next = new byte[(current.Length + 1) / 2][];
            for (int i = 0; i < current.Length; i += 2)
            {
                var left = current[i];
                var right = i + 1 < current.Length ? current[i + 1] : current[i];
                next[i / 2] = HashNode(left, right);
            }
            current = next;
            idx /= 2;
        }
        return (current[0], path);
    }

    /// <summary>
    /// Verify that <paramref name="leafHash"/> at <paramref name="leafIndex"/> hashes
    /// up the provided path to <paramref name="expectedRoot"/>.
    /// </summary>
    public static bool VerifyInclusion(
        ReadOnlySpan<byte> leafHash,
        IReadOnlyList<byte[]> path,
        ulong leafIndex,
        ReadOnlySpan<byte> expectedRoot)
    {
        var current = leafHash.ToArray();
        var idx = leafIndex;
        foreach (var sibling in path)
        {
            current = (idx & 1) == 0
                ? HashNode(current, sibling)
                : HashNode(sibling, current);
            idx >>= 1;
        }
        return current.AsSpan().SequenceEqual(expectedRoot);
    }
}
