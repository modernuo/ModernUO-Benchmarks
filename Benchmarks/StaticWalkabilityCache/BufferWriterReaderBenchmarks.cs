using System;
using System.Buffers.Binary;
using System.IO;
using BenchmarkDotNet.Attributes;
using Server;

namespace StaticWalkabilityCacheBench;

/// <summary>
/// Compares ModernUO's <see cref="BufferWriter"/>/<see cref="BufferReader"/> against
/// .NET's built-in <see cref="BinaryWriter"/>/<see cref="BinaryReader"/> and the
/// span-based <see cref="BinaryPrimitives"/> path. The MUO buffer pair hasn't been
/// re-benchmarked against built-ins since .NET 7; this stamps a fresh number for
/// .NET 10 and confirms the buffer path is still the right default for the bake/load
/// pipeline (it currently writes ~2,592 sbyte/byte arrays per Tier 3 chunk).
///
/// Workload: mixed primitive sequence representative of the serialization shape
/// MUO actually uses — header fields (u32 magic, u32 version, u32 mapId, etc.) plus
/// a bulk byte array. Run for both write (build the byte sequence) and read (consume
/// it back).
/// </summary>
[MemoryDiagnoser]
public class BufferWriterReaderBenchmarks
{
    // Match the bake's record shapes loosely: a header chunk + a bulk array write.
    // Keeps the numbers comparable to the actual hot path.
    private const int BulkArraySize = 2048;
    private byte[] _bulkPayload;

    // Pre-built encoded byte buffer used by all Read* benches.
    private byte[] _encoded;

    // Pre-allocated scratch reused by all Write* benches so per-op allocations
    // are visible without setup noise.
    private byte[] _scratch;

    [GlobalSetup]
    public void Setup()
    {
        _bulkPayload = new byte[BulkArraySize];
        new Random(42).NextBytes(_bulkPayload);

        // Encode the canonical sequence once so all Read* benches share a deterministic input.
        var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(0x53574200u); // magic
            bw.Write(6u);          // version
            bw.Write(1u);          // mapId
            bw.Write(0xFEEDF00Dul); // tileDataHash
            bw.Write((ulong)DateTime.UtcNow.Ticks); // bakeTimestamp
            bw.Write((ulong)48); // chunkIndexOffset
            bw.Write((short)1234);
            bw.Write((sbyte)-7);
            bw.Write((byte)42);
            bw.Write(true);
            bw.Write(_bulkPayload);
        }
        _encoded = ms.ToArray();

        _scratch = new byte[_encoded.Length + 256];
    }

    // ── WRITE benchmarks ───────────────────────────────────────────────────────

    [Benchmark, BenchmarkCategory("Write")]
    public int Write_BufferWriter()
    {
        // Pass a pre-allocated buffer so the writer doesn't grow during the op.
        var bw = new BufferWriter(_scratch, prefixStr: false);
        bw.Write(0x53574200u);
        bw.Write(6u);
        bw.Write(1u);
        bw.Write(0xFEEDF00Dul);
        bw.Write((ulong)DateTime.UtcNow.Ticks);
        bw.Write((ulong)48);
        bw.Write((short)1234);
        bw.Write((sbyte)-7);
        bw.Write((byte)42);
        bw.Write(true);
        bw.Write(_bulkPayload);
        return (int)bw.Position;
    }

    [Benchmark, BenchmarkCategory("Write")]
    public int Write_BinaryWriter()
    {
        using var ms = new MemoryStream(_scratch);
        using var bw = new BinaryWriter(ms);
        bw.Write(0x53574200u);
        bw.Write(6u);
        bw.Write(1u);
        bw.Write(0xFEEDF00Dul);
        bw.Write((ulong)DateTime.UtcNow.Ticks);
        bw.Write((ulong)48);
        bw.Write((short)1234);
        bw.Write((sbyte)-7);
        bw.Write((byte)42);
        bw.Write(true);
        bw.Write(_bulkPayload);
        return (int)ms.Position;
    }

    [Benchmark, BenchmarkCategory("Write")]
    public int Write_BinaryPrimitives_Span()
    {
        // Hand-rolled span writer — establishes a lower bound for the encoding cost
        // (no virtual dispatch, no growth, no string handling).
        var dst = _scratch.AsSpan();
        var pos = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(pos), 0x53574200u); pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(pos), 6u);          pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(dst.Slice(pos), 1u);          pos += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(pos), 0xFEEDF00Dul); pos += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(pos), (ulong)DateTime.UtcNow.Ticks); pos += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(pos), 48u);         pos += 8;
        BinaryPrimitives.WriteInt16LittleEndian(dst.Slice(pos), 1234);         pos += 2;
        dst[pos++] = unchecked((byte)-7);
        dst[pos++] = 42;
        dst[pos++] = 1; // true
        _bulkPayload.AsSpan().CopyTo(dst.Slice(pos)); pos += _bulkPayload.Length;
        return pos;
    }

    // ── READ benchmarks ────────────────────────────────────────────────────────

    [Benchmark, BenchmarkCategory("Read")]
    public long Read_BufferReader()
    {
        var br = new BufferReader(_encoded);
        var sum = 0L;
        sum += br.ReadUInt();
        sum += br.ReadUInt();
        sum += br.ReadUInt();
        sum += unchecked((long)br.ReadULong());
        sum += unchecked((long)br.ReadULong());
        sum += unchecked((long)br.ReadULong());
        sum += br.ReadShort();
        sum += br.ReadSByte();
        sum += br.ReadByte();
        sum += br.ReadBool() ? 1 : 0;
        var bulk = new byte[BulkArraySize];
        br.Read(bulk.AsSpan());
        return sum + bulk[0];
    }

    [Benchmark, BenchmarkCategory("Read")]
    public long Read_BinaryReader()
    {
        using var ms = new MemoryStream(_encoded);
        using var br = new BinaryReader(ms);
        var sum = 0L;
        sum += br.ReadUInt32();
        sum += br.ReadUInt32();
        sum += br.ReadUInt32();
        sum += unchecked((long)br.ReadUInt64());
        sum += unchecked((long)br.ReadUInt64());
        sum += unchecked((long)br.ReadUInt64());
        sum += br.ReadInt16();
        sum += br.ReadSByte();
        sum += br.ReadByte();
        sum += br.ReadBoolean() ? 1 : 0;
        var bulk = br.ReadBytes(BulkArraySize);
        return sum + bulk[0];
    }

    [Benchmark, BenchmarkCategory("Read")]
    public long Read_BinaryPrimitives_Span()
    {
        var src = (ReadOnlySpan<byte>)_encoded;
        var pos = 0;
        var sum = 0L;
        sum += BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(pos)); pos += 4;
        sum += BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(pos)); pos += 4;
        sum += BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(pos)); pos += 4;
        sum += unchecked((long)BinaryPrimitives.ReadUInt64LittleEndian(src.Slice(pos))); pos += 8;
        sum += unchecked((long)BinaryPrimitives.ReadUInt64LittleEndian(src.Slice(pos))); pos += 8;
        sum += unchecked((long)BinaryPrimitives.ReadUInt64LittleEndian(src.Slice(pos))); pos += 8;
        sum += BinaryPrimitives.ReadInt16LittleEndian(src.Slice(pos)); pos += 2;
        sum += unchecked((sbyte)src[pos++]);
        sum += src[pos++];
        sum += src[pos++] != 0 ? 1 : 0;
        // Bulk payload — no need to copy; just touch the first byte to be fair to the
        // other variants (which actually copy into a managed array).
        var bulk = new byte[BulkArraySize];
        src.Slice(pos, BulkArraySize).CopyTo(bulk);
        return sum + bulk[0];
    }
}
