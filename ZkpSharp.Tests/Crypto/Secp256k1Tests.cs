using System.Numerics;
using Xunit;
using ZkpSharp.Crypto;
using ZkpSharp.Crypto.Secp256k1;

namespace ZkpSharp.Tests.Crypto
{
    public class Secp256k1Tests
    {
        #region FieldElement Tests

        [Fact]
        public void FieldElement_AddSubMul_ArithmeticIdentities()
        {
            var a = new FieldElement(17);
            var b = new FieldElement(42);

            Assert.Equal(new FieldElement(59), a + b);
            Assert.Equal(new FieldElement(17), (a + b) - b);
            Assert.Equal(new FieldElement(714), a * b);
        }

        [Fact]
        public void FieldElement_Inverse_RoundTrip()
        {
            var a = new FieldElement(12345);
            var aInv = a.Inv();
            Assert.Equal(FieldElement.One, a * aInv);
        }

        [Fact]
        public void FieldElement_Sqrt_KnownValue()
        {
            var four = new FieldElement(4);
            var two = four.Sqrt();
            Assert.Equal(four, two * two);
        }

        [Fact]
        public void FieldElement_Negation()
        {
            var a = new FieldElement(100);
            Assert.Equal(FieldElement.Zero, a + (-a));
        }

        [Fact]
        public void FieldElement_BytesRoundTrip()
        {
            var a = new FieldElement(BigInteger.Parse("0DEADBEEF", System.Globalization.NumberStyles.HexNumber));
            var bytes = a.ToBytes();
            Assert.Equal(32, bytes.Length);
            Assert.Equal(a, FieldElement.FromBytes(bytes));
        }

        #endregion

        #region Scalar Tests

        [Fact]
        public void Scalar_Arithmetic()
        {
            var a = new Scalar(7);
            var b = new Scalar(11);

            Assert.Equal(new Scalar(77), a * b);
            Assert.Equal(new Scalar(18), a + b);
            Assert.Equal(Scalar.Zero, a - a);
        }

        [Fact]
        public void Scalar_Inverse()
        {
            var a = new Scalar(999);
            Assert.Equal(Scalar.One, a * a.Inv());
        }

        [Fact]
        public void Scalar_Random_NotZero()
        {
            var s = Scalar.Random();
            Assert.False(s.IsZero);
        }

        [Fact]
        public void Scalar_BytesRoundTrip()
        {
            var s = Scalar.Random();
            var bytes = s.ToBytes();
            Assert.Equal(32, bytes.Length);
            Assert.Equal(s, Scalar.FromBytes(bytes));
        }

        [Fact]
        public void Scalar_InnerProduct()
        {
            var a = new Scalar[] { new(2), new(3) };
            var b = new Scalar[] { new(5), new(7) };
            Assert.Equal(new Scalar(31), Scalar.InnerProduct(a, b)); // 2*5 + 3*7 = 31
        }

        #endregion

        #region Point Tests

        [Fact]
        public void Point_GeneratorOnCurve()
        {
            Assert.True(Point.G.IsOnCurve());
        }

        [Fact]
        public void Point_AddIdentity()
        {
            Assert.Equal(Point.G, Point.G + Point.Infinity);
            Assert.Equal(Point.G, Point.Infinity + Point.G);
        }

        [Fact]
        public void Point_AddNegation_GivesInfinity()
        {
            var negG = -Point.G;
            Assert.True((Point.G + negG).IsInfinity);
        }

        [Fact]
        public void Point_ScalarMul_One()
        {
            Assert.Equal(Point.G, Scalar.One * Point.G);
        }

        [Fact]
        public void Point_ScalarMul_Two_EqualsDouble()
        {
            var doubled = Point.Double(Point.G);
            var twoG = Scalar.Two * Point.G;
            Assert.Equal(doubled, twoG);
        }

        [Fact]
        public void Point_ScalarMul_Order_GivesInfinity()
        {
            var nG = new Scalar(Scalar.N) * Point.G;
            Assert.True(nG.IsInfinity);
        }

        [Fact]
        public void Point_EncodeDecodeRoundTrip()
        {
            var encoded = Point.G.Encode();
            Assert.Equal(33, encoded.Length);
            var decoded = Point.Decode(encoded);
            Assert.Equal(Point.G, decoded);
        }

        [Fact]
        public void Point_ScalarMul_Distributive()
        {
            var a = new Scalar(7);
            var b = new Scalar(11);
            var lhs = (a + b) * Point.G;
            var rhs = a * Point.G + b * Point.G;
            Assert.Equal(lhs, rhs);
        }

        #endregion

        #region Generator Tests

        [Fact]
        public void Generators_H_OnCurve_NotG()
        {
            Assert.True(Generators.H.IsOnCurve());
            Assert.NotEqual(Generators.G, Generators.H);
        }

        [Fact]
        public void Generators_VectorGi_AllOnCurve()
        {
            foreach (var gi in Generators.Gi)
                Assert.True(gi.IsOnCurve());
        }

        [Fact]
        public void Generators_VectorHi_AllOnCurve()
        {
            foreach (var hi in Generators.Hi)
                Assert.True(hi.IsOnCurve());
        }

        [Fact]
        public void Generators_AllDistinct()
        {
            var allPoints = new List<Point> { Generators.G, Generators.H };
            allPoints.AddRange(Generators.Gi);
            allPoints.AddRange(Generators.Hi);

            var encodedSet = new HashSet<string>();
            foreach (var p in allPoints)
                Assert.True(encodedSet.Add(Convert.ToBase64String(p.Encode())),
                    "Duplicate generator point detected.");
        }

        #endregion

        #region Pedersen Commitment Tests

        [Fact]
        public void PedersenCommitment_OpenVerify()
        {
            var v = new Scalar(42);
            var r = Scalar.Random();
            var C = PedersenCommitment.Commit(v, r);

            Assert.True(PedersenCommitment.Open(C, v, r));
            Assert.False(PedersenCommitment.Open(C, new Scalar(43), r));
        }

        [Fact]
        public void PedersenCommitment_Homomorphic()
        {
            var v1 = new Scalar(100);
            var r1 = Scalar.Random();
            var v2 = new Scalar(200);
            var r2 = Scalar.Random();

            var C1 = PedersenCommitment.Commit(v1, r1);
            var C2 = PedersenCommitment.Commit(v2, r2);
            var CSum = PedersenCommitment.Commit(v1 + v2, r1 + r2);

            Assert.Equal(CSum, C1 + C2);
        }

        #endregion
    }
}
