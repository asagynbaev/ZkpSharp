using System.Buffers.Binary;
using System.Text;
using ZkpSharp.Chains.Solana.Internal;

namespace ZkpSharp.Chains.Solana.Tests;

public class BorshTests
{
    [Fact]
    public void WriteU8_EncodesSingleByte()
    {
        var bytes = new AnchorBorshWriter().WriteU8(0x42).ToArray();
        Assert.Equal(new byte[] { 0x42 }, bytes);
    }

    [Fact]
    public void WriteBool_EncodesAsZeroOrOne()
    {
        Assert.Equal(new byte[] { 1 }, new AnchorBorshWriter().WriteBool(true).ToArray());
        Assert.Equal(new byte[] { 0 }, new AnchorBorshWriter().WriteBool(false).ToArray());
    }

    [Fact]
    public void WriteU64_EncodesLittleEndian()
    {
        var bytes = new AnchorBorshWriter().WriteU64(0x0102030405060708).ToArray();
        Assert.Equal(new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }, bytes);
    }

    [Fact]
    public void WriteI64_EncodesNegativeLittleEndian()
    {
        var bytes = new AnchorBorshWriter().WriteI64(-1).ToArray();
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, bytes);
    }

    [Fact]
    public void WriteString_LengthPrefixedUtf8()
    {
        var bytes = new AnchorBorshWriter().WriteString("hi").ToArray();
        // 4-byte LE length = 2, then "hi"
        Assert.Equal(new byte[] { 2, 0, 0, 0, (byte)'h', (byte)'i' }, bytes);
    }

    [Fact]
    public void WriteFixedBytes_VerbatimNoLength()
    {
        var input = new byte[] { 1, 2, 3, 4 };
        var bytes = new AnchorBorshWriter().WriteFixedBytes(input, 4).ToArray();
        Assert.Equal(input, bytes);
    }

    [Fact]
    public void WriteFixedBytes_WrongLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AnchorBorshWriter().WriteFixedBytes(new byte[] { 1 }, 4));
    }

    [Fact]
    public void Roundtrip_FullDidAnchorPayload()
    {
        // Build a synthetic DidAnchor body (no discriminator) and read it back.
        var didHash = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var owner = Enumerable.Range(32, 32).Select(i => (byte)i).ToArray();
        var root = Enumerable.Range(64, 32).Select(i => (byte)i).ToArray();

        var bytes = new AnchorBorshWriter()
            .WriteU8(1)
            .WriteFixedBytes(didHash, 32)
            .WriteFixedBytes(owner, 32)
            .WriteFixedBytes(root, 32)
            .WriteU64(7)
            .WriteI64(1_700_000_000)
            .WriteI64(1_700_000_500)
            .ToArray();

        var reader = new AnchorBorshReader(bytes);
        Assert.Equal((byte)1, reader.ReadU8());
        Assert.Equal(didHash, reader.ReadFixedBytes(32));
        Assert.Equal(owner, reader.ReadPubkey());
        Assert.Equal(root, reader.ReadFixedBytes(32));
        Assert.Equal(7UL, reader.ReadU64());
        Assert.Equal(1_700_000_000L, reader.ReadI64());
        Assert.Equal(1_700_000_500L, reader.ReadI64());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Roundtrip_String_HandlesUtf8()
    {
        var bytes = new AnchorBorshWriter().WriteString("привет 🌍").ToArray();
        var reader = new AnchorBorshReader(bytes);
        Assert.Equal("привет 🌍", reader.ReadString());
    }

    [Fact]
    public void ReadU32_ParsesLittleEndian()
    {
        var bytes = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        var reader = new AnchorBorshReader(bytes);
        Assert.Equal(0x12345678u, reader.ReadU32());
    }

    [Fact]
    public void Skip_AdvancesReader()
    {
        var bytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var reader = new AnchorBorshReader(bytes);
        reader.Skip(2);
        Assert.Equal((byte)0xCC, reader.ReadU8());
    }
}
