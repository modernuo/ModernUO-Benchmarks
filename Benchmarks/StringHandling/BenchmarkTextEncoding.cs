using System;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Buffers;
using Server.Text;

namespace Benchmarks.BenchmarkUtilities;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkTextEncoding
{
    public static readonly Encoding Unicode = TextEncoding.Unicode;

    public static string str =
        "This is a test string with some special characters: \u0001\u0002\u0003 - And then stuff like this: \u0001\u0002\u0003";

    public static string str2 =
        "This is a test string with some special characters: - And then stuff like this:";

    public static byte[] strBytes = Unicode.GetBytes(str);

    public static byte[] strBytes2 = Unicode.GetBytes(str2);

    [Benchmark]
    public void GetString()
    {
        GetString(strBytes, Unicode);
    }

    [Benchmark]
    public void GetStringNotSpecial()
    {
        GetString(strBytes2, Unicode);
    }

    [Benchmark]
    public void GetStringSpanHelpers()
    {
        GetStringSpanHelpers(strBytes, Unicode);
    }

    [Benchmark]
    public void GetStringSpanHelpersNotSpecial()
    {
        GetStringSpanHelpers(strBytes2, Unicode);
    }

    public static void GetStringSpanHelpers(ReadOnlySpan<byte> span, Encoding encoding)
    {
        var charCount = encoding.GetMaxCharCount(span.Length);

        char[] rentedChars = null;
        var chars = charCount <= 256
            ? stackalloc char[charCount]
            : rentedChars = STArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var length = encoding.GetChars(span, chars);
            chars = chars[..length];

            var index = chars.IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);
            if (index == -1)
            {
                return;
            }

            using var sb = charCount <= 256 ? new ValueStringBuilder(stackalloc char[charCount]) : ValueStringBuilder.Create(charCount);
            while (index != -1)
            {
                sb.Append(chars[..index]);
                chars = chars[(index + 1)..];
                index = chars.IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);
            }

            if (chars.Length > 0)
            {
                sb.Append(chars);
            }

            return;
        }
        finally
        {
            STArrayPool<char>.Shared.Return(rentedChars);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSafeChar(ushort c) => c is >= 0x20 and < 0xFFFE;

    public static void GetString(ReadOnlySpan<byte> span, Encoding encoding)
    {
        string s = encoding.GetString(span);

        ReadOnlySpan<char> chars = s.AsSpan();

        using var sb = new ValueStringBuilder(stackalloc char[256]);
        var hasDoneAnyReplacements = false;

        for (int i = 0, last = 0; i < chars.Length; i++)
        {
            if (!IsSafeChar(chars[i]) && i == chars.Length - 1)
            {
                hasDoneAnyReplacements = true;
                sb.Append(chars.Slice(last, i - last));
                last = i + 1; // Skip the unsafe char
            }
        }
    }
}