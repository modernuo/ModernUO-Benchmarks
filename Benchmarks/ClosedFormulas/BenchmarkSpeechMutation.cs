using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Text;

namespace ClosedFormulas;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkSpeechMutation
{
    private static readonly string Sentence = "The quick brown fox jumps over the lazy dog";

    [Benchmark]
    public void ManualLoopMutation()
    {
        using var sb = new ValueStringBuilder(stackalloc char[Sentence.Length]);

        for (var i = 0; i < Sentence.Length; ++i)
        {
            sb.Append(Sentence[i] != ' ' ? Mobile.DefaultGhostChars.RandomElement() : ' ');
        }
    }

    [Benchmark]
    public void SpanLoopMutation()
    {
        ReadOnlySpan<char> ghostChars = Mobile.DefaultGhostChars.AsSpan();

        Span<char> chars = stackalloc char[Sentence.Length];

        var textSpan = Sentence.AsSpan();
        for (var i = 0; i < textSpan.Length; ++i)
        {
            chars[i] = textSpan[i] != ' ' ? ghostChars.RandomElement() : ' ';
        }
    }
}