using System;
using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.StringHandling;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BenchmarkInvalidCharSearch
{
    // Test strings of ~30 characters (typical UO protocol field size)

    // Valid ASCII only - most common case
    private const string ValidAsciiString = "PlayerCharacterName12345";

    // Valid Latin1 with extended chars (0xA0-0xFF) - no filtering needed
    private const string ValidLatin1String = "Björk Guðmundsdóttir";

    // Invalid - contains C0 control code (0x01)
    private const string InvalidC0String = "Player\x01Name";

    // Invalid - contains C1 control code (0x85)
    private const string InvalidC1String = "Player\x85Name";

    // Pre-encoded bytes
    private static readonly byte[] ValidAsciiBytes = Encoding.Latin1.GetBytes(ValidAsciiString);
    private static readonly byte[] ValidLatin1Bytes = Encoding.Latin1.GetBytes(ValidLatin1String);
    private static readonly byte[] InvalidC0Bytes = Encoding.Latin1.GetBytes(InvalidC0String);
    private static readonly byte[] InvalidC1Bytes = Encoding.Latin1.GetBytes(InvalidC1String);

    // Pre-encoded chars (simulating what GetChars would return)
    private static readonly char[] ValidAsciiChars = ValidAsciiString.ToCharArray();
    private static readonly char[] ValidLatin1Chars = ValidLatin1String.ToCharArray();
    private static readonly char[] InvalidC0Chars = InvalidC0String.ToCharArray();
    private static readonly char[] InvalidC1Chars = InvalidC1String.ToCharArray();

    // SearchValues for invalid bytes (C0: 0x00-0x1F, C1: 0x80-0x9F)
    private static readonly SearchValues<byte> InvalidLatin1Bytes = SearchValues.Create(
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
        0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97,
        0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F
    );

    // SearchValues for invalid ASCII bytes (C0 only: 0x00-0x1F)
    private static readonly SearchValues<byte> InvalidAsciiBytes = SearchValues.Create(
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
    );

    // SearchValues for invalid chars (C0 + C1 + Unicode non-chars)
    private static readonly SearchValues<char> InvalidUnicodeChars = SearchValues.Create(
        // C0 control codes (0x00-0x1F)
        '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
        '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F',
        '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17',
        '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F',
        // C1 control codes (0x80-0x9F)
        '\x80', '\x81', '\x82', '\x83', '\x84', '\x85', '\x86', '\x87',
        '\x88', '\x89', '\x8A', '\x8B', '\x8C', '\x8D', '\x8E', '\x8F',
        '\x90', '\x91', '\x92', '\x93', '\x94', '\x95', '\x96', '\x97',
        '\x98', '\x99', '\x9A', '\x9B', '\x9C', '\x9D', '\x9E', '\x9F',
        // Unicode non-characters
        '\uFFFE', '\uFFFF'
    );

    #region Byte Search - Latin1 (C0 + C1)

    [Benchmark]
    public int SearchValues_Bytes_ValidAscii() => ValidAsciiBytes.AsSpan().IndexOfAny(InvalidLatin1Bytes);

    [Benchmark]
    public int SearchValues_Bytes_ValidLatin1() => ValidLatin1Bytes.AsSpan().IndexOfAny(InvalidLatin1Bytes);

    [Benchmark]
    public int SearchValues_Bytes_InvalidC0() => InvalidC0Bytes.AsSpan().IndexOfAny(InvalidLatin1Bytes);

    [Benchmark]
    public int SearchValues_Bytes_InvalidC1() => InvalidC1Bytes.AsSpan().IndexOfAny(InvalidLatin1Bytes);

    #endregion

    #region Byte Search - ASCII (C0 only)

    [Benchmark]
    public int SearchValues_AsciiBytes_ValidAscii() => ValidAsciiBytes.AsSpan().IndexOfAny(InvalidAsciiBytes);

    [Benchmark]
    public int SearchValues_AsciiBytes_InvalidC0() => InvalidC0Bytes.AsSpan().IndexOfAny(InvalidAsciiBytes);

    [Benchmark]
    public int IndexOfAnyInRange_AsciiBytes_ValidAscii() => ValidAsciiBytes.AsSpan().IndexOfAnyInRange((byte)0x00, (byte)0x1F);

    [Benchmark]
    public int IndexOfAnyInRange_AsciiBytes_InvalidC0() => InvalidC0Bytes.AsSpan().IndexOfAnyInRange((byte)0x00, (byte)0x1F);

    #endregion

    #region Char Search - Current (ExceptInRange 0x20-0xFFFD) - MISSES C1!

    [Benchmark]
    public int ExceptInRange_Chars_ValidAscii() => ValidAsciiChars.AsSpan().IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);

    [Benchmark]
    public int ExceptInRange_Chars_ValidLatin1() => ValidLatin1Chars.AsSpan().IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);

    [Benchmark]
    public int ExceptInRange_Chars_InvalidC0() => InvalidC0Chars.AsSpan().IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);

    [Benchmark]
    public int ExceptInRange_Chars_InvalidC1() => InvalidC1Chars.AsSpan().IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);

    #endregion

    #region Char Search - SearchValues (C0 + C1 + non-chars) - CORRECT

    [Benchmark]
    public int SearchValues_Chars_ValidAscii() => ValidAsciiChars.AsSpan().IndexOfAny(InvalidUnicodeChars);

    [Benchmark]
    public int SearchValues_Chars_ValidLatin1() => ValidLatin1Chars.AsSpan().IndexOfAny(InvalidUnicodeChars);

    [Benchmark]
    public int SearchValues_Chars_InvalidC0() => InvalidC0Chars.AsSpan().IndexOfAny(InvalidUnicodeChars);

    [Benchmark]
    public int SearchValues_Chars_InvalidC1() => InvalidC1Chars.AsSpan().IndexOfAny(InvalidUnicodeChars);

    #endregion
}
