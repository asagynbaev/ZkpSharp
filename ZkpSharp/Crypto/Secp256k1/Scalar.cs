using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;

namespace ZkpSharp.Crypto.Secp256k1
{
    /// <summary>
    /// Element of the scalar field F_n where n is the secp256k1 group order.
    /// Used for discrete logarithms, blinding factors, and challenges.
    /// </summary>
    public readonly struct Scalar : IEquatable<Scalar>
    {
        public static readonly BigInteger N = BigInteger.Parse(
            "0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141",
            NumberStyles.HexNumber);

        private readonly BigInteger _value;

        public Scalar(BigInteger value)
        {
            var r = value % N;
            _value = r.Sign < 0 ? r + N : r;
        }

        public static Scalar Zero => new(BigInteger.Zero);
        public static Scalar One => new(BigInteger.One);
        public static Scalar Two => new(new BigInteger(2));

        public BigInteger Value => _value;
        public bool IsZero => _value.IsZero;

        public static Scalar operator +(Scalar a, Scalar b)
            => new(a._value + b._value);

        public static Scalar operator -(Scalar a, Scalar b)
            => new(a._value - b._value);

        public static Scalar operator -(Scalar a)
            => new(a.IsZero ? BigInteger.Zero : N - a._value);

        public static Scalar operator *(Scalar a, Scalar b)
            => new(a._value * b._value);

        public Scalar Square() => new(_value * _value);

        /// <summary>
        /// Modular inverse via Fermat's little theorem: a^(n-2) mod n.
        /// </summary>
        public Scalar Inv()
        {
            if (IsZero)
                throw new DivideByZeroException("Cannot invert zero scalar.");
            return new(BigInteger.ModPow(_value, N - 2, N));
        }

        /// <summary>
        /// Raise to a power mod n.
        /// </summary>
        public Scalar Pow(BigInteger exponent)
            => new(BigInteger.ModPow(_value, exponent, N));

        /// <summary>
        /// Generate a cryptographically random non-zero scalar.
        /// </summary>
        public static Scalar Random()
        {
            var bytes = new byte[32];
            Scalar s;
            do
            {
                RandomNumberGenerator.Fill(bytes);
                s = new Scalar(new BigInteger(bytes, isUnsigned: true, isBigEndian: true));
            } while (s.IsZero);
            return s;
        }

        /// <summary>
        /// Create scalar from a long value (non-negative).
        /// </summary>
        public static Scalar From(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
            return new(new BigInteger(value));
        }

        public byte[] ToBytes()
        {
            var bytes = _value.ToByteArray(isUnsigned: true, isBigEndian: true);
            if (bytes.Length == 32) return bytes;
            if (bytes.Length > 32) return bytes[^32..];
            var result = new byte[32];
            bytes.CopyTo(result, 32 - bytes.Length);
            return result;
        }

        public static Scalar FromBytes(byte[] bytes)
        {
            if (bytes.Length != 32)
                throw new ArgumentException("Scalar encoding must be exactly 32 bytes.", nameof(bytes));
            return new(new BigInteger(bytes, isUnsigned: true, isBigEndian: true));
        }

        public static Scalar FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 32)
                throw new ArgumentException("Scalar encoding must be exactly 32 bytes.", nameof(bytes));
            return new(new BigInteger(bytes, isUnsigned: true, isBigEndian: true));
        }

        /// <summary>
        /// Inner product of two scalar vectors: sum(a[i] * b[i]).
        /// </summary>
        public static Scalar InnerProduct(Scalar[] a, Scalar[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same length.");
            var sum = Zero;
            for (int i = 0; i < a.Length; i++)
                sum = sum + a[i] * b[i];
            return sum;
        }

        public bool Equals(Scalar other) => _value == other._value;
        public override bool Equals(object? obj) => obj is Scalar s && Equals(s);
        public override int GetHashCode() => _value.GetHashCode();
        public static bool operator ==(Scalar a, Scalar b) => a._value == b._value;
        public static bool operator !=(Scalar a, Scalar b) => a._value != b._value;
        public override string ToString() => _value.ToString("X64");
    }
}
