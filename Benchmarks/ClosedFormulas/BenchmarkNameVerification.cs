using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ClosedFormulas;

[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkNameVerification
{
    [Benchmark]
    public bool ValidateName()
    {
        return Validate("JohnDoe-Dick", 2, 16, true, false, true, 1, SpaceDashPeriodQuote, Disallowed, StartDisallowed);
    }

    [Benchmark]
    public bool ValidateNameSV()
    {
        return ValidateSV("JohnDoe-Dick", 2, 16, true, false, true, 1, SpaceDashPeriodQuoteSV, Disallowed, StartDisallowedSV, StartDisallowedSV);
    }

     public static readonly SearchValues<char> AlphaNumeric = SearchValues.Create(
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
        'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
        'u', 'v', 'w', 'x', 'y', 'z'
    );

    public static readonly SearchValues<char> Alphabetic = SearchValues.Create(
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
        'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
        'u', 'v', 'w', 'x', 'y', 'z'
    );

    public static readonly SearchValues<char> Numeric = SearchValues.Create(
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    );

    public static readonly char[] SpaceDashPeriodQuote = [' ', '-', '.', '\''];

    public static readonly SearchValues<char> SpaceDashPeriodQuoteSV = SearchValues.Create(' ', '-', '.', '\'');

    public static readonly string[] StartDisallowed =
    [
        "seer",
        "counselor",
        "gm",
        "admin",
        "lady",
        "lord"
    ];

    public static readonly SearchValues<string> StartDisallowedSV = SearchValues.Create(
        StartDisallowed,
        StringComparison.OrdinalIgnoreCase
    );

    public static readonly string[] Disallowed =
    [
        "jigaboo",
        "chigaboo",
        "wop",
        "kyke",
        "kike",
        "tit",
        "spic",
        "prick",
        "piss",
        "lezbo",
        "lesbo",
        "felatio",
        "dyke",
        "dildo",
        "chinc",
        "chink",
        "cunnilingus",
        "cum",
        "cocksucker",
        "cock",
        "clitoris",
        "clit",
        "ass",
        "hitler",
        "penis",
        "nigga",
        "nigger",
        "klit",
        "kunt",
        "jiz",
        "jism",
        "jerkoff",
        "jackoff",
        "goddamn",
        "fag",
        "blowjob",
        "bitch",
        "asshole",
        "dick",
        "pussy",
        "snatch",
        "cunt",
        "twat",
        "shit",
        "fuck",
        "tailor",
        "smith",
        "scholar",
        "rogue",
        "novice",
        "neophyte",
        "merchant",
        "medium",
        "master",
        "mage",
        "lb",
        "journeyman",
        "grandmaster",
        "fisherman",
        "expert",
        "chef",
        "carpenter",
        "british",
        "blackthorne",
        "blackthorn",
        "beggar",
        "archer",
        "apprentice",
        "adept",
        "gamemaster",
        "frozen",
        "squelched",
        "invulnerable",
        "osi",
        "origin"
    ];

    public static readonly SearchValues<string> DisallowedSearchValues = SearchValues.Create(
        Disallowed,
        StringComparison.OrdinalIgnoreCase
    );

    public static bool ValidateSV(
        ReadOnlySpan<char> name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
        bool noExceptionsAtStart, int maxExceptions, SearchValues<char> exceptions, ReadOnlySpan<string> disallowed,
        SearchValues<string> disallowedSV, SearchValues<string> startDisallowedSV
    )
    {
        if (name.Length == 0 || name.Length < minLength || name.Length > maxLength)
        {
            return false;
        }

        if (exceptions == null)
        {
            // We don't have exceptions, so we might be limited to letters or numbers
            var allowed = allowLetters switch
            {
                true when allowDigits  => AlphaNumeric,
                true                   => Alphabetic,
                false when allowDigits => Numeric,
                _                      => null // Everything is allowed! Use `Utility.FixHtml()` to stop weird behavior
            };

            if (allowed != null && name.ContainsAnyExcept(allowed))
            {
                return false;
            }
        }
        else
        {
            // We have exceptions, and at least one of the letters/digits flag is false:
            var notAllowed = allowLetters switch
            {
                true when !allowDigits  => Numeric,
                false when allowDigits => Alphabetic,
                _                      => null
            };

            if (notAllowed != null && name.ContainsAny(notAllowed))
            {
                return false;
            }

            if (ContainsExceptions(name, exceptions, noExceptionsAtStart, maxExceptions))
            {
                return false;
            }
        }

        if (disallowedSV != null && disallowed.Length > 0 && ContainsDisallowedWord(name, disallowed, disallowedSV))
        {
            return false;
        }

        return startDisallowedSV == null || name.IndexOfAny(startDisallowedSV) != 0;
    }

    public static bool ContainsExceptions(
        ReadOnlySpan<char> name, SearchValues<char> exceptions, bool noExceptionsAtStart, int maxExceptions
    )
    {
        if (!noExceptionsAtStart && maxExceptions is <= -1 or >= int.MaxValue)
        {
            return false;
        }

        var index = name.IndexOfAny(exceptions);

        while (index != -1)
        {
            if (noExceptionsAtStart)
            {
                if (index == 0)
                {
                    return true;
                }

                noExceptionsAtStart = false;
            }

            if (maxExceptions-- <= 0)
            {
                return true;
            }

            if (index + 1 < name.Length)
            {
                name = name[(index + 1)..];
                index = name.IndexOfAny(exceptions);
            }
            else
            {
                index = -1;
            }
        }

        return false;
    }

    public static bool ContainsDisallowedWord(ReadOnlySpan<char> name, ReadOnlySpan<string> disallowed, SearchValues<string> disallowedSV)
    {
        var index = name.IndexOfAny(disallowedSV);

        while (index != -1)
        {
            var isStartBoundary = index == 0 || !char.IsLetterOrDigit(name[index - 1]);

            if (isStartBoundary)
            {
                for (var i = 0; i < disallowed.Length; i++)
                {
                    var word = disallowed[i].AsSpan();
                    if (index + word.Length > name.Length || !name.Slice(index, word.Length).Equals(word, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // End boundary
                    if (index + word.Length == name.Length || !char.IsLetterOrDigit(name[index + word.Length]))
                    {
                        return true;
                    }
                }
            }

            if (index + 1 < name.Length)
            {
                name = name[(index + 1)..];
                index = name.IndexOfAny(disallowedSV);
            }
            else
            {
                index = -1;
            }
        }

        return false;
    }

    public static bool Validate(string name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
        bool noExceptionsAtStart, int maxExceptions, char[] exceptions, string[] disallowed, string[] startDisallowed)
    {
        if (name == null || name.Length < minLength || name.Length > maxLength)
        {
            return false;
        }

        var exceptCount = 0;

        name = name.ToLower();

        if (!allowLetters || !allowDigits ||
            exceptions.Length > 0 && (noExceptionsAtStart || maxExceptions < int.MaxValue))
        {
            for (var i = 0; i < name.Length; ++i)
            {
                var c = name[i];

                if (c >= 'a' && c <= 'z')
                {
                    if (!allowLetters)
                    {
                        return false;
                    }

                    exceptCount = 0;
                }
                else if (c >= '0' && c <= '9')
                {
                    if (!allowDigits)
                    {
                        return false;
                    }

                    exceptCount = 0;
                }
                else
                {
                    var except = false;

                    for (var j = 0; !except && j < exceptions.Length; ++j)
                    {
                        if (c == exceptions[j])
                        {
                            except = true;
                        }
                    }

                    if (!except || i == 0 && noExceptionsAtStart)
                    {
                        return false;
                    }

                    if (exceptCount++ == maxExceptions)
                    {
                        return false;
                    }
                }
            }
        }

        for (var i = 0; i < disallowed.Length; ++i)
        {
            var indexOf = name.IndexOf(disallowed[i], StringComparison.OrdinalIgnoreCase);

            if (indexOf == -1)
            {
                continue;
            }

            var badPrefix = indexOf == 0;

            for (var j = 0; !badPrefix && j < exceptions.Length; ++j)
            {
                badPrefix = name[indexOf - 1] == exceptions[j];
            }

            if (!badPrefix)
            {
                continue;
            }

            var badSuffix = indexOf + disallowed[i].Length >= name.Length;

            for (var j = 0; !badSuffix && j < exceptions.Length; ++j)
            {
                badSuffix = name[indexOf + disallowed[i].Length] == exceptions[j];
            }

            if (badSuffix)
            {
                return false;
            }
        }

        for (var i = 0; i < startDisallowed.Length; ++i)
        {
            if (name.StartsWith(startDisallowed[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}