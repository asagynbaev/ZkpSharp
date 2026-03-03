using System.Globalization;
using System.Numerics;

namespace ZkpSharp.Crypto.Secp256k1
{
    /// <summary>
    /// Element of the finite field F_p where p is the secp256k1 prime.
    /// All arithmetic is performed modulo p.
    /// </summary>
    public readonly struct FieldElement : IEquatable<FieldElement>
    {
        public static readonly BigInteger P = BigInteger.Parse(
            "0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F",
            NumberStyles.HexNumber);

        private readonly BigInteger _value;

        public FieldElement(BigInteger value)
        {
            var r = value % P;
            _value = r.Sign < 0 ? r + P : r;
        }

        public static FieldElement Zero => new(BigInteger.Zero);
        public static FieldElement One => new(BigInteger.One);

        public BigInteger Value => _value;
        public bool IsZero => _value.IsZero;
        public bool IsEven => _value.IsEven;

        public static FieldElement operator +(FieldElement a, FieldElement b)
            => new(a._value + b._value);

        public static FieldElement operator -(FieldElement a, FieldElement b)
            => new(a._value - b._value);

        public static FieldElement operator -(FieldElement a)
            => new(a.IsZero ? BigInteger.Zero : P - a._value);

        public static FieldElement operator *(FieldElement a, FieldElement b)
            => new(a._value * b._value);

        public FieldElement Square() => new(_value * _value);

        /// <summary>
        /// Modular inverse via Fermat's little theorem: a^(p-2) mod p.
        /// </summary>
        public FieldElement Inv()
        {
            if (IsZero)
                throw new DivideByZeroException("Cannot invert zero in the field.");
            return new(BigInteger.ModPow(_value, P - 2, P));
        }

        /// <summary>
        /// Square root using the identity sqrt(a) = a^((p+1)/4) mod p.
        /// Valid because p ≡ 3 (mod 4) for secp256k1.
        /// </summary>
        public FieldElement Sqrt()
        {
            var candidate = new FieldElement(BigInteger.ModPow(_value, (P + 1) / 4, P));
            if (candidate.Square() != this)
                throw new ArithmeticException("Element has no square root in the field.");
            return candidate;
        }

        public bool HasSqrt()
        {
            if (IsZero) return true;
            var candidate = new FieldElement(BigInteger.ModPow(_value, (P + 1) / 4, P));
            return candidate.Square() == this;
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

        public static FieldElement FromBytes(byte[] bytes)
        {
            if (bytes.Length != 32)
                throw new ArgumentException("Field element encoding must be exactly 32 bytes.", nameof(bytes));
            return new(new BigInteger(bytes, isUnsigned: true, isBigEndian: true));
        }

        public static FieldElement FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 32)
                throw new ArgumentException("Field element encoding must be exactly 32 bytes.", nameof(bytes));
            return new(new BigInteger(bytes, isUnsigned: true, isBigEndian: true));
        }

        public bool Equals(FieldElement other) => _value == other._value;
        public override bool Equals(object? obj) => obj is FieldElement fe && Equals(fe);
        public override int GetHashCode() => _value.GetHashCode();
        public static bool operator ==(FieldElement a, FieldElement b) => a._value == b._value;
        public static bool operator !=(FieldElement a, FieldElement b) => a._value != b._value;
        public override string ToString() => _value.ToString("X64");
    }
}
