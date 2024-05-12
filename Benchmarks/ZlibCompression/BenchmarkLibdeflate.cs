using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Gumps.MockedGumps;
using Server;
using Server.Buffers;
using Server.Gumps;
using Server.Network;
using System.Buffers;
using ZlibCompression;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BenchmarkLibdeflate
{
    private static readonly byte[] _packetBuffer = GC.AllocateUninitializedArray<byte>(0x10000);
    private static readonly byte[] _packBuffer = GC.AllocateUninitializedArray<byte>(0x20000);

    private static byte[] LayoutData;
    private static byte[] StringsData;

    [GlobalSetup]
    public void Prepare()
    {
        FakeGump gump = new FakeGump("TEST");
        DynamicGumpBuilder gumpBuilder = new DynamicGumpBuilder();
        gump.PublicBuildLayout(ref gumpBuilder);
        gumpBuilder.FinalizeLayout();

        LayoutData = gumpBuilder.LayoutData.ToArray();
        StringsData = gumpBuilder.StringsData.ToArray();

        gumpBuilder.Dispose();
    }

    [Benchmark]
    public void ZlibComp()
    {
        var writer = new SpanWriter(_packetBuffer);

        WritePackedOld(LayoutData, ref writer);
        WritePackedOld(StringsData, ref writer);

        writer.Dispose();
    }

    [Benchmark]
    public void LibdeflateComp()
    {
        var writer = new SpanWriter(_packetBuffer);

        WritePackedNew(LayoutData, ref writer);
        WritePackedNew(StringsData, ref writer);

        writer.Dispose();
    }

    private static void FakeCreatePacketOld(FakeGump gump, ref SpanWriter writer)
    {
        writer.Write((byte)0xDD); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(gump.Serial);
        writer.Write(gump.TypeID);
        writer.Write(gump.X);
        writer.Write(gump.Y);

        DynamicGumpBuilder gumpBuilder = new DynamicGumpBuilder();
        gump.PublicBuildLayout(ref gumpBuilder);
        gumpBuilder.FinalizeLayout();

        WritePackedOld(gumpBuilder.LayoutData, ref writer);
        WritePackedOld(gumpBuilder.StringsData, ref writer);

        gumpBuilder.Dispose();

        writer.WritePacketLength();
    }


    private static void FakeCreatePacketNew(FakeGump gump, ref SpanWriter writer)
    {
        writer.Write((byte)0xDD); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(gump.Serial);
        writer.Write(gump.TypeID);
        writer.Write(gump.X);
        writer.Write(gump.Y);

        DynamicGumpBuilder gumpBuilder = new DynamicGumpBuilder();
        gump.PublicBuildLayout(ref gumpBuilder);
        gumpBuilder.FinalizeLayout();

        WritePackedNew(gumpBuilder.LayoutData, ref writer);
        WritePackedNew(gumpBuilder.StringsData, ref writer);

        gumpBuilder.Dispose();

        writer.WritePacketLength();
    }

    public static void WritePackedOld(ReadOnlySpan<byte> span, ref SpanWriter writer)
    {
        var length = span.Length;

        if (length == 0)
        {
            writer.Write(0);
            return;
        }

        var wantLength = Zlib.MaxPackSize(length);
        var packBuffer = _packBuffer;
        byte[] rentedBuffer = null;

        if (wantLength > packBuffer.Length)
        {
            packBuffer = rentedBuffer = STArrayPool<byte>.Shared.Rent(wantLength);
        }

        var packLength = wantLength;

        var error = Zlib.Pack(packBuffer, ref packLength, span, length, Server.ZlibQuality.Default);

        if (error != Server.ZlibError.Okay)
        {
            throw new Exception();
        }

        writer.Write(4 + packLength);
        writer.Write(length);
        writer.Write(packBuffer.AsSpan(0, packLength));

        if (rentedBuffer != null)
        {
            STArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    public static void WritePackedNew(ReadOnlySpan<byte> span, ref SpanWriter writer)
    {
        var length = span.Length;

        if (length == 0)
        {
            writer.Write(0);
            return;
        }

        var wantLength = Libdeflate.MaxPackSize(length);
        var packBuffer = _packBuffer;
        byte[] rentedBuffer = null;

        if (wantLength > packBuffer.Length)
        {
            packBuffer = rentedBuffer = STArrayPool<byte>.Shared.Rent(wantLength);
        }

        var packLength = wantLength;

        var error = Libdeflate.Pack(packBuffer, ref packLength, span, length, ZlibCompression.ZlibQuality.Default);

        if (error != ZlibCompression.ZlibError.Okay)
        {
            throw new Exception();
        }

        writer.Write(4 + packLength);
        writer.Write(length);
        writer.Write(packBuffer.AsSpan(0, packLength));

        if (rentedBuffer != null)
        {
            STArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }
}
