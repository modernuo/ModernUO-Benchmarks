using System;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.BenchmarkUtilities;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BenchmarkStringHelpers
{
    // private readonly string[] names =
    // {
    //     "Kamron", "Owyn", "Luthius", "Jaedan", "Vorspire", "other people",
    //     "Kamron-2", "Owyn-2", "Luthius-2", "Jaedan-2", "Vorspire-2", "other people too"
    // };

    // private int length;

    // [GlobalSetup]
    // public void Setup()
    // {
        // var chrs = ArrayPool<char>.Shared.Rent(65535);
        // ArrayPool<char>.Shared.Return(chrs);
        // length = 0;
        //
        // for (var i = 0; i < names.Length; i++)
        // {
        //     length += names.Length;
        // }
        //
        // length += 2 * (names.Length - 1) + 3;
    // }

    private static string name = "ModernUO";
    private static int age = 10;
    private static int clients = 100;
    private static int items = 5000000;
    private static int mobiles = 100000;
    private static int mem = 24386123;

    private static byte[][] partsu8 =
    {
        "ModernUO, Name="u8.ToArray(),
        ", Age="u8.ToArray(),
        ", Clients="u8.ToArray(),
        ", Items="u8.ToArray(),
        ", Chars="u8.ToArray(),
        ", Mem="u8.ToArray(),
        "K, Ver=2"u8.ToArray()
    };

    private static byte[][] partsu16 =
    {
        Encoding.Unicode.GetBytes("ModernUO, Name="),
        Encoding.Unicode.GetBytes(", Age="),
        Encoding.Unicode.GetBytes(", Clients="),
        Encoding.Unicode.GetBytes(", Items="),
        Encoding.Unicode.GetBytes(", Chars="),
        Encoding.Unicode.GetBytes(", Mem="),
        Encoding.Unicode.GetBytes("K, Ver=2")
    };

    private static byte[] buffer = new byte[0x1000];

    [Benchmark]
    public int BenchmarkEncodingUTF8()
    {
        var str =
            $"ModernUO, Name={name}, Age={age}, Clients={clients}, Items={items}, Chars={mobiles}, Mem={mem}K, Ver=2";

        return Encoding.UTF8.GetBytes(str, buffer);
    }

    [Benchmark]
    public int BenchmarkTryFormatUTF8()
    {
        var span = buffer.AsSpan();

        var s = partsu8[0];
        s.CopyTo(span);
        var totalCopied = s.Length;

        totalCopied += Encoding.UTF8.GetBytes(name, span[totalCopied..]);
        (s = partsu8[1]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        Utf8Formatter.TryFormat(age, span[totalCopied..], out var bytesWritten);
        totalCopied += bytesWritten;
        (s = partsu8[2]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        Utf8Formatter.TryFormat(clients, span[totalCopied..], out bytesWritten);
        totalCopied += bytesWritten;
        (s = partsu8[3]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        Utf8Formatter.TryFormat(items, span[totalCopied..], out bytesWritten);
        totalCopied += bytesWritten;
        (s = partsu8[4]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        Utf8Formatter.TryFormat(mobiles, span[totalCopied..], out bytesWritten);
        totalCopied += bytesWritten;
        (s = partsu8[5]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        Utf8Formatter.TryFormat(mem, span[totalCopied..], out bytesWritten);
        totalCopied += bytesWritten;
        (s = partsu8[6]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        return totalCopied;
    }
    
    [Benchmark]
    public int BenchmarkEncodingUTF16LE()
    {
        var str =
            $"ModernUO, Name={name}, Age={age}, Clients={clients}, Items={items}, Chars={mobiles}, Mem={mem}K, Ver=2";

        return Encoding.Unicode.GetBytes(str, buffer);
    }
    
    [Benchmark]
    public int BenchmarkCastUTF16LE()
    {
        var str =
            $"ModernUO, Name={name}, Age={age}, Clients={clients}, Items={items}, Chars={mobiles}, Mem={mem}K, Ver=2";

        var bytes = MemoryMarshal.Cast<char, byte>(str);
        bytes.CopyTo(buffer);

        return bytes.Length;
    }

    [Benchmark]
    public int BenchmarkTryFormatUTF16LE()
    {
        var span = buffer.AsSpan();

        var s = partsu16[0];
        s.CopyTo(span);
        var totalCopied = s.Length;
        
        totalCopied += Encoding.BigEndianUnicode.GetBytes(name, span[totalCopied..]);
        (s = partsu16[1]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        var chrs = MemoryMarshal.Cast<byte, char>(span[totalCopied..]);
        age.TryFormat(chrs, out var chrsWritten);
        totalCopied += chrsWritten * 2;
        (s = partsu16[2]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;
        
        chrs = MemoryMarshal.Cast<byte, char>(span[totalCopied..]);
        clients.TryFormat(chrs, out chrsWritten);
        totalCopied += chrsWritten * 2;
        (s = partsu16[3]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;
        
        chrs = MemoryMarshal.Cast<byte, char>(span[totalCopied..]);
        items.TryFormat(chrs, out chrsWritten);
        totalCopied += chrsWritten * 2;
        (s = partsu16[4]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;
        
        chrs = MemoryMarshal.Cast<byte, char>(span[totalCopied..]);
        mobiles.TryFormat(chrs, out chrsWritten);
        totalCopied += chrsWritten * 2;
        (s = partsu16[5]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;
        
        chrs = MemoryMarshal.Cast<byte, char>(span[totalCopied..]);
        mem.TryFormat(chrs, out chrsWritten);
        totalCopied += chrsWritten * 2;
        (s = partsu16[6]).CopyTo(span[totalCopied..]);
        totalCopied += s.Length;

        return totalCopied;
    }

    // [Benchmark]
    // public string BenchmarkStringBuilder()
    // {
    //     var sb = new StringBuilder();
    //     for (var i = 0; i < names.Length; i++)
    //     {
    //         if (i > 0)
    //         {
    //             sb.Append(i == names.Length - 1 ? ", and" : ", ");
    //         }
    //
    //         sb.Append(names[i]);
    //     }
    //
    //     return sb.ToString();
    // }
    //
    // [Benchmark]
    // public string BenchmarkValueStringBuilderWithStack()
    // {
    //     using var sb = new ValueStringBuilder(stackalloc char[length]);
    //     for (var i = 0; i < names.Length; i++)
    //     {
    //         if (i > 0)
    //         {
    //             sb.Append(i == names.Length - 1 ? ", and" : ", ");
    //         }
    //
    //         sb.Append(names[i]);
    //     }
    //
    //     return sb.ToString();
    // }
    //
    // [Benchmark]
    // public string BenchmarkValueStringBuilderWithRentedBuffer()
    // {
    //     using var sb = new ValueStringBuilder(stackalloc char[32]);
    //     for (var i = 0; i < names.Length; i++)
    //     {
    //         if (i > 0)
    //         {
    //             sb.Append(i == names.Length - 1 ? ", and" : ", ");
    //         }
    //
    //         sb.Append(names[i]);
    //     }
    //
    //     return sb.ToString();
    // }
}
