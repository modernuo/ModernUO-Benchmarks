using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Server;

public class LocalizationEntry
{
    private static readonly Regex _textRegex = new(
        @"~(\d+)[_\w]+~",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant
    );

    public int Number { get; }
    public string[] TextSlices { get; private set; }
    
    public string Formatter { get; private set; }

    public LocalizationEntry(int number, string text, string formatter)
    {
        Number = number;
        ParseText(text);
        Formatter = formatter;
    }

    private void ParseText(string text)
    {
        var prevIndex = 0;
        var queue = new Queue<string>();
        var matches = 0;
        foreach (Match match in _textRegex.Matches(text))
        {
            matches++;

            if (prevIndex < match.Index)
            {
                var substr = text[prevIndex..match.Index];

                queue.Enqueue(substr);
            }

            // queue.Enqueue(int.Parse(match.Groups[1].Value));
            queue.Enqueue(null);
            prevIndex = match.Index + match.Length;
        }

        if (prevIndex < text.Length - 1)
        {
            queue.Enqueue(prevIndex == 0 ? text : text[prevIndex..]);
        }

        TextSlices = queue.ToArray();
    }
}
