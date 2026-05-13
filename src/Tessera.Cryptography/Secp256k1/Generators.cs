using System.Security.Cryptography;
using System.Text;

namespace Tessera.Cryptography.Secp256k1
{
    /// <summary>
    /// Deterministic generator points for Pedersen commitments and Bulletproofs.
    /// H is derived via hash-to-curve so that no one knows the discrete log H = xG.
    /// Vector generators Gi, Hi are similarly derived for the inner product argument.
    /// </summary>
    public static class Generators
    {
        public const int DefaultN = 64;

        public static readonly Point G = Point.G;

        private static readonly Lazy<Point> _h = new(() => HashToCurve("Tessera_Pedersen_H"));
        private static readonly Lazy<Point[]> _gi = new(() => GenerateVector("Tessera_Gi", DefaultN));
        private static readonly Lazy<Point[]> _hi = new(() => GenerateVector("Tessera_Hi", DefaultN));

        public static Point H => _h.Value;
        public static Point[] Gi => _gi.Value;
        public static Point[] Hi => _hi.Value;

        /// <summary>
        /// Hash an arbitrary label to a point on secp256k1 (try-and-increment).
        /// </summary>
        private static Point HashToCurve(string label)
        {
            for (uint counter = 0; counter < 1000; counter++)
            {
                var input = Encoding.UTF8.GetBytes(label + ":" + counter);
                var hash = SHA256.HashData(input);
                var x = FieldElement.FromBytes(hash);
                var rhs = x * x * x + new FieldElement(7);
                if (!rhs.HasSqrt()) continue;
                var y = rhs.Sqrt();
                if (!y.IsEven) y = -y;
                var point = new Point(x, y, FieldElement.One);
                if (point.IsOnCurve() && !point.IsInfinity) return point;
            }
            throw new InvalidOperationException($"Failed to hash '{label}' to curve point after 1000 attempts.");
        }

        private static Point[] GenerateVector(string prefix, int n)
        {
            var result = new Point[n];
            for (int i = 0; i < n; i++)
                result[i] = HashToCurve($"{prefix}_{i}");
            return result;
        }
    }
}
