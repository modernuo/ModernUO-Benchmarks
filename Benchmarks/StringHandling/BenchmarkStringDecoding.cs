using System;
using System.Buffers;
using System.Text;
using System.Text.Unicode;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.StringHandling;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BenchmarkStringDecoding
{
    // Realistic UO protocol data sizes
    // Short: Player names (~15 chars)
    // Medium: Chat messages (~60 chars)
    // Long: Gump HTML text (~300 chars)

    private const string ShortAscii = "PlayerName1234";
    private const string MediumAscii = "Hello everyone! Welcome to Britannia. Let's go on an adventure today!";
    private const string LongAscii = "This is a longer piece of text that might appear in a gump or book. It contains multiple sentences and represents the kind of content you might see in quest descriptions, item properties, or other game text that needs to be displayed to players in the user interface.";

    private const string ShortLatin1 = "Björk Müller";
    private const string MediumLatin1 = "Café résumé naïve Ångström über größe Señor façade";
    private const string LongLatin1 = "În această zonă veți găsi multe comori ascunse. Atenție la monștrii periculoși! Räuberhöhle está más allá de las montañas. El camino es peligroso pero las recompensas són magnífiques. Bonne chance à tous les aventuriers!";

    // ASCII bytes (valid as both ASCII and UTF-8)
    private static readonly byte[] ShortAsciiBytes = Encoding.ASCII.GetBytes(ShortAscii);
    private static readonly byte[] MediumAsciiBytes = Encoding.ASCII.GetBytes(MediumAscii);
    private static readonly byte[] LongAsciiBytes = Encoding.ASCII.GetBytes(LongAscii);

    // UTF-8 bytes with multi-byte sequences (Latin1 extended chars)
    private static readonly byte[] ShortUtf8Bytes = Encoding.UTF8.GetBytes(ShortLatin1);
    private static readonly byte[] MediumUtf8Bytes = Encoding.UTF8.GetBytes(MediumLatin1);
    private static readonly byte[] LongUtf8Bytes = Encoding.UTF8.GetBytes(LongLatin1);

    // UTF-16 Big Endian (UO protocol uses this)
    private static readonly byte[] ShortUnicodeBEBytes = Encoding.BigEndianUnicode.GetBytes(ShortLatin1);
    private static readonly byte[] MediumUnicodeBEBytes = Encoding.BigEndianUnicode.GetBytes(MediumLatin1);
    private static readonly byte[] LongUnicodeBEBytes = Encoding.BigEndianUnicode.GetBytes(LongLatin1);

    // UTF-16 Little Endian
    private static readonly byte[] ShortUnicodeLEBytes = Encoding.Unicode.GetBytes(ShortLatin1);
    private static readonly byte[] MediumUnicodeLEBytes = Encoding.Unicode.GetBytes(MediumLatin1);
    private static readonly byte[] LongUnicodeLEBytes = Encoding.Unicode.GetBytes(LongLatin1);

    #region ASCII data: Encoding.ASCII vs Encoding.UTF8 vs Utf8.ToUtf16

    [Benchmark]
    public string AsciiData_AsciiEncoding_Short() => Encoding.ASCII.GetString(ShortAsciiBytes);

    [Benchmark]
    public string AsciiData_AsciiEncoding_Medium() => Encoding.ASCII.GetString(MediumAsciiBytes);

    [Benchmark]
    public string AsciiData_AsciiEncoding_Long() => Encoding.ASCII.GetString(LongAsciiBytes);

    [Benchmark]
    public string AsciiData_Utf8Encoding_Short() => Encoding.UTF8.GetString(ShortAsciiBytes);

    [Benchmark]
    public string AsciiData_Utf8Encoding_Medium() => Encoding.UTF8.GetString(MediumAsciiBytes);

    [Benchmark]
    public string AsciiData_Utf8Encoding_Long() => Encoding.UTF8.GetString(LongAsciiBytes);

    [Benchmark]
    public string AsciiData_Utf8ToUtf16_Short() => Utf8ToUtf16String(ShortAsciiBytes);

    [Benchmark]
    public string AsciiData_Utf8ToUtf16_Medium() => Utf8ToUtf16String(MediumAsciiBytes);

    [Benchmark]
    public string AsciiData_Utf8ToUtf16_Long() => Utf8ToUtf16String(LongAsciiBytes);

    #endregion

    #region UTF-8 multi-byte data: Encoding.UTF8 vs Utf8.ToUtf16

    [Benchmark]
    public string Utf8Data_Utf8Encoding_Short() => Encoding.UTF8.GetString(ShortUtf8Bytes);

    [Benchmark]
    public string Utf8Data_Utf8Encoding_Medium() => Encoding.UTF8.GetString(MediumUtf8Bytes);

    [Benchmark]
    public string Utf8Data_Utf8Encoding_Long() => Encoding.UTF8.GetString(LongUtf8Bytes);

    [Benchmark]
    public string Utf8Data_Utf8ToUtf16_Short() => Utf8ToUtf16String(ShortUtf8Bytes);

    [Benchmark]
    public string Utf8Data_Utf8ToUtf16_Medium() => Utf8ToUtf16String(MediumUtf8Bytes);

    [Benchmark]
    public string Utf8Data_Utf8ToUtf16_Long() => Utf8ToUtf16String(LongUtf8Bytes);

    #endregion

    #region UTF-16 BE (UO Protocol): Encoding.BigEndianUnicode

    [Benchmark]
    public string UnicodeBE_Encoding_Short() => Encoding.BigEndianUnicode.GetString(ShortUnicodeBEBytes);

    [Benchmark]
    public string UnicodeBE_Encoding_Medium() => Encoding.BigEndianUnicode.GetString(MediumUnicodeBEBytes);

    [Benchmark]
    public string UnicodeBE_Encoding_Long() => Encoding.BigEndianUnicode.GetString(LongUnicodeBEBytes);

    #endregion

    #region UTF-16 LE: Encoding.Unicode

    [Benchmark]
    public string UnicodeLE_Encoding_Short() => Encoding.Unicode.GetString(ShortUnicodeLEBytes);

    [Benchmark]
    public string UnicodeLE_Encoding_Medium() => Encoding.Unicode.GetString(MediumUnicodeLEBytes);

    [Benchmark]
    public string UnicodeLE_Encoding_Long() => Encoding.Unicode.GetString(LongUnicodeLEBytes);

    #endregion

    #region GetChars to pre-allocated Span (for safe string filtering path)

    [Benchmark]
    public int AsciiData_AsciiGetChars_Short()
    {
        Span<char> buffer = stackalloc char[ShortAsciiBytes.Length];
        return Encoding.ASCII.GetChars(ShortAsciiBytes, buffer);
    }

    [Benchmark]
    public int AsciiData_Utf8GetChars_Short()
    {
        Span<char> buffer = stackalloc char[ShortAsciiBytes.Length];
        return Encoding.UTF8.GetChars(ShortAsciiBytes, buffer);
    }

    [Benchmark]
    public int AsciiData_Utf8ToUtf16Span_Short()
    {
        Span<char> buffer = stackalloc char[ShortAsciiBytes.Length];
        Utf8.ToUtf16(ShortAsciiBytes, buffer, out _, out int charsWritten);
        return charsWritten;
    }

    [Benchmark]
    public int Utf8Data_Utf8GetChars_Short()
    {
        Span<char> buffer = stackalloc char[ShortUtf8Bytes.Length * 2];
        return Encoding.UTF8.GetChars(ShortUtf8Bytes, buffer);
    }

    [Benchmark]
    public int Utf8Data_Utf8ToUtf16Span_Short()
    {
        Span<char> buffer = stackalloc char[ShortUtf8Bytes.Length * 2];
        Utf8.ToUtf16(ShortUtf8Bytes, buffer, out _, out int charsWritten);
        return charsWritten;
    }

    [Benchmark]
    public int UnicodeBE_GetChars_Short()
    {
        Span<char> buffer = stackalloc char[ShortUnicodeBEBytes.Length];
        return Encoding.BigEndianUnicode.GetChars(ShortUnicodeBEBytes, buffer);
    }

    #endregion

    #region Helper Methods

    private static string Utf8ToUtf16String(ReadOnlySpan<byte> bytes)
    {
        // Max chars needed: same as byte count for ASCII, potentially more for multi-byte
        int maxChars = bytes.Length;
        Span<char> buffer = maxChars <= 256
            ? stackalloc char[maxChars]
            : new char[maxChars * 2];

        var status = Utf8.ToUtf16(bytes, buffer, out _, out int charsWritten);
        if (status != OperationStatus.Done)
        {
            // Fallback for buffer too small (multi-byte sequences)
            buffer = new char[bytes.Length * 2];
            Utf8.ToUtf16(bytes, buffer, out _, out charsWritten);
        }

        return new string(buffer[..charsWritten]);
    }

    #endregion
}
