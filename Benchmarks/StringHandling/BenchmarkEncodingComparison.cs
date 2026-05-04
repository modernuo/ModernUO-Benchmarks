using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.BenchmarkUtilities;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BenchmarkEncodingComparison
{
    static BenchmarkEncodingComparison()
    {
        // Ensure code pages are available
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        CP1252 = Encoding.GetEncoding(1252);
        CP1252Bytes = CP1252.GetBytes(CP1252String);
    }

    // ~30 character strings matching UO protocol field sizes (username, character name, etc.)

    // Pure ASCII string (common case)
    public const string AsciiString = "PlayerCharacterName12345";

    // String with Latin1 extended chars (0xA0-0xFF) - accented letters
    public const string Latin1String = "Plàyér_Chàráctér_Nàmé123";

    // String with CP1252-specific chars (0x80-0x9F) - smart quotes, euro, etc.
    public const string CP1252String = "Player “Name” with €uro™";

    // Encodings
    public static readonly Encoding Ascii = Encoding.ASCII;
    public static readonly Encoding Latin1 = Encoding.Latin1;
    public static Encoding CP1252;

    // Pre-encoded bytes for decoding benchmarks
    public static readonly byte[] AsciiBytes = Ascii.GetBytes(AsciiString);
    public static readonly byte[] Latin1Bytes = Latin1.GetBytes(Latin1String);
    public static byte[] CP1252Bytes;

    // Hybrid approach: try ASCII first, fall back to CP1252
    public static int HybridEncode(string value, Span<byte> destination)
    {
        if (System.Text.Ascii.IsValid(value))
            return Encoding.ASCII.GetBytes(value, destination);
        return CP1252.GetBytes(value, destination);
    }

    public static string HybridDecode(ReadOnlySpan<byte> bytes)
    {
        if (System.Text.Ascii.IsValid(bytes))
            return Encoding.ASCII.GetString(bytes);
        return CP1252.GetString(bytes);
    }

    // #region Encoding (String → Bytes) - ASCII content
    //
    // [Benchmark]
    // public int Encode_Ascii_AsciiContent()
    // {
    //     Span<byte> buffer = stackalloc byte[AsciiString.Length];
    //     return Ascii.GetBytes(AsciiString, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_Latin1_AsciiContent()
    // {
    //     Span<byte> buffer = stackalloc byte[AsciiString.Length];
    //     return Latin1.GetBytes(AsciiString, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_CP1252_AsciiContent()
    // {
    //     Span<byte> buffer = stackalloc byte[AsciiString.Length];
    //     return CP1252.GetBytes(AsciiString, buffer);
    // }
    //
    // #endregion
    //
    // #region Encoding (String → Bytes) - Latin1 content (accented chars)
    //
    // [Benchmark]
    // public int Encode_Ascii_Latin1Content()
    // {
    //     Span<byte> buffer = stackalloc byte[Latin1String.Length];
    //     return Ascii.GetBytes(Latin1String, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_Latin1_Latin1Content()
    // {
    //     Span<byte> buffer = stackalloc byte[Latin1String.Length];
    //     return Latin1.GetBytes(Latin1String, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_CP1252_Latin1Content()
    // {
    //     Span<byte> buffer = stackalloc byte[Latin1String.Length];
    //     return CP1252.GetBytes(Latin1String, buffer);
    // }
    //
    // #endregion
    //
    // #region Encoding (String → Bytes) - CP1252 content (smart quotes, euro, etc.)
    //
    // [Benchmark]
    // public int Encode_Ascii_CP1252Content()
    // {
    //     Span<byte> buffer = stackalloc byte[CP1252String.Length];
    //     return Ascii.GetBytes(CP1252String, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_Latin1_CP1252Content()
    // {
    //     Span<byte> buffer = stackalloc byte[CP1252String.Length];
    //     return Latin1.GetBytes(CP1252String, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_CP1252_CP1252Content()
    // {
    //     Span<byte> buffer = stackalloc byte[CP1252String.Length];
    //     return CP1252.GetBytes(CP1252String, buffer);
    // }
    //
    // #endregion
    //
    // #region Decoding (Bytes → String) - ASCII content
    //
    // [Benchmark]
    // public string Decode_Ascii_AsciiContent()
    // {
    //     return Ascii.GetString(AsciiBytes);
    // }
    //
    // [Benchmark]
    // public string Decode_Latin1_AsciiContent()
    // {
    //     return Latin1.GetString(AsciiBytes);
    // }
    //
    // [Benchmark]
    // public string Decode_CP1252_AsciiContent()
    // {
    //     return CP1252.GetString(AsciiBytes);
    // }
    //
    // #endregion
    //
    // #region Decoding (Bytes → String) - Latin1 content
    //
    // [Benchmark]
    // public string Decode_Ascii_Latin1Content()
    // {
    //     return Ascii.GetString(Latin1Bytes);
    // }
    //
    // [Benchmark]
    // public string Decode_Latin1_Latin1Content()
    // {
    //     return Latin1.GetString(Latin1Bytes);
    // }
    //
    // [Benchmark]
    // public string Decode_CP1252_Latin1Content()
    // {
    //     return CP1252.GetString(Latin1Bytes);
    // }
    //
    // #endregion
    //
    // #region Decoding (Bytes → String) - CP1252 content
    //
    // [Benchmark]
    // public string Decode_Ascii_CP1252Content()
    // {
    //     return Ascii.GetString(CP1252Bytes);
    // }
    //
    // [Benchmark]
    // public string Decode_Latin1_CP1252Content()
    // {
    //     return Latin1.GetString(CP1252Bytes);
    // }
    //
    // [Benchmark]
    // public string Decode_CP1252_CP1252Content()
    // {
    //     return CP1252.GetString(CP1252Bytes);
    // }
    //
    // #endregion
    //
    // #region Hybrid Encoding (String → Bytes) - ASCII check + fallback
    //
    // [Benchmark]
    // public int Encode_Hybrid_AsciiContent()
    // {
    //     Span<byte> buffer = stackalloc byte[AsciiString.Length];
    //     return HybridEncode(AsciiString, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_Hybrid_Latin1Content()
    // {
    //     Span<byte> buffer = stackalloc byte[Latin1String.Length];
    //     return HybridEncode(Latin1String, buffer);
    // }
    //
    // [Benchmark]
    // public int Encode_Hybrid_CP1252Content()
    // {
    //     Span<byte> buffer = stackalloc byte[CP1252String.Length];
    //     return HybridEncode(CP1252String, buffer);
    // }
    //
    // #endregion
    //
    // #region Hybrid Decoding (Bytes → String) - ASCII check + fallback
    //
    // [Benchmark]
    // public string Decode_Hybrid_AsciiContent()
    // {
    //     return HybridDecode(AsciiBytes);
    // }
    //
    // [Benchmark]
    // public string Decode_Hybrid_Latin1Content()
    // {
    //     return HybridDecode(Latin1Bytes);
    // }
    //
    // [Benchmark]
    // public string Decode_Hybrid_CP1252Content()
    // {
    //     return HybridDecode(CP1252Bytes);
    // }
    //
    // #endregion

    #region Ascii.IsValid overhead measurement

    [Benchmark]
    public bool IsValid_AsciiContent()
    {
        return System.Text.Ascii.IsValid(AsciiString);
    }

    [Benchmark]
    public bool IsValid_Latin1Content()
    {
        return System.Text.Ascii.IsValid(Latin1String);
    }

    [Benchmark]
    public bool IsValid_AsciiBytes()
    {
        return System.Text.Ascii.IsValid(AsciiBytes);
    }

    [Benchmark]
    public bool IsValid_Latin1Bytes()
    {
        return System.Text.Ascii.IsValid(Latin1Bytes);
    }

    #endregion
}
