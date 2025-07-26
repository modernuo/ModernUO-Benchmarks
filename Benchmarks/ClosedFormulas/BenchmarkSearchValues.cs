using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ClosedFormulas;

[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkSearchValues
{
    private static char[] ForbiddenCharsArray = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
    private static SearchValues<char> ForbiddenCharsSV = SearchValues.Create("<>:\"/\\|?*");
    private static string username = "some.person@somedomain.com";

    private static bool IsForbiddenChar(char c)
    {
        for (var i = 0; i < ForbiddenCharsArray.Length; ++i)
        {
            if (c == ForbiddenCharsArray[i])
            {
                return true;
            }
        }

        return false;
    }

    [Benchmark]
    public bool ForLoopUsernameSafe()
    {
        var isSafe = true;
        for (var i = 0; isSafe && i < username.Length; ++i)
        {
            isSafe = username[i] >= 0x20 && username[i] < 0x7F && !IsForbiddenChar(username[i]);
        }

        return isSafe;
    }

    [Benchmark]
    public bool SVUsernameSafe()
    {
        var unSpan = username.AsSpan();
        return !unSpan.ContainsAnyExceptInRange((char)0x20, (char)0x7E) &&
               !unSpan.ContainsAny(ForbiddenCharsSV);
    }
}