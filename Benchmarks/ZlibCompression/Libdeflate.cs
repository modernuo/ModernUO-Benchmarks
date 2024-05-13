using System.Runtime.InteropServices;
using CallConv = System.Runtime.CompilerServices.CallConvCdecl;

namespace ZlibCompression;

public enum ZlibError
{
    Okay = 0,
    Error
}

public enum ZlibQuality
{
    Default = 6
}

public static class Libdeflate
{
    private static readonly nint Compressor;
    private static readonly nint Decompressor;

    static Libdeflate()
    {
        Compressor = NativeMethods.libdeflate_alloc_compressor(6);
        Decompressor = NativeMethods.libdeflate_alloc_decompressor();

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        return;

        static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            NativeMethods.libdeflate_free_compressor(Compressor);
            NativeMethods.libdeflate_free_decompressor(Decompressor);
        }
    }

    public static int MaxPackSize(int inputLength)
    {
        return (int)NativeMethods.libdeflate_zlib_compress_bound(Compressor, (nuint)inputLength);
    }

    public static ZlibError Pack(Span<byte> dest, ref int destLength, ReadOnlySpan<byte> source, ZlibQuality quality)
    {
        return Pack(dest, ref destLength, source, source.Length, quality);
    }

    public static ZlibError Pack(Span<byte> dest, ref int destLength, ReadOnlySpan<byte> source, int sourceLength, ZlibQuality quality)
    {
        var result = NativeMethods.libdeflate_zlib_compress(Compressor, in MemoryMarshal.GetReference(source), (nuint)sourceLength,
            ref MemoryMarshal.GetReference(dest), (nuint)destLength);

        if (result == 0)
        {
            return ZlibError.Error;
        }

        destLength = (int)result;
        return ZlibError.Okay;
    }

    public static ZlibError Unpack(Span<byte> dest, ref int destLength, ReadOnlySpan<byte> source, int sourceLength)
    {
        var result = NativeMethods.libdeflate_zlib_decompress(Decompressor, in MemoryMarshal.GetReference(source),
            (nuint)sourceLength, ref MemoryMarshal.GetReference(dest), (nuint)destLength, out var bytesWritten);

        if (result != LibdeflateResult.Success)
        {
            return ZlibError.Error;
        }

        destLength = (int)bytesWritten;
        return ZlibError.Okay;
    }
}

internal enum LibdeflateResult
{
    Success = 0,
    BadData = 1,
    ShortOutput = 2,
    InsufficientSpace = 3,
}

internal static partial class NativeMethods
{
    private const string DllName = "libdeflate";

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConv)])]
    public static partial nint libdeflate_alloc_compressor(int compression_level);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConv)])]
    public static partial nuint libdeflate_zlib_compress(nint compressor, in byte @in, nuint in_nbytes, ref byte @out, nuint out_nbytes_avail);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConv)])]
    public static partial nuint libdeflate_zlib_compress_bound(nint compressor, nuint in_nbytes);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConv)])]
    public static partial void libdeflate_free_compressor(nint compressor);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConv)])]
    public static partial nint libdeflate_alloc_decompressor();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConv)])]
    public static partial LibdeflateResult libdeflate_zlib_decompress(nint decompressor, in byte @in, nuint in_nbytes, ref byte @out, nuint out_nbytes_avail, out nuint actual_out_nbytes_ret);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConv)])]
    public static partial void libdeflate_free_decompressor(nint decompressor);
}
