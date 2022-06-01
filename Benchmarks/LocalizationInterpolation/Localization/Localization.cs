using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server;

public static class Localization
{
    public const string FallbackLanguage = "enu";
    private static Dictionary<int, LocalizationEntry> _fallbackEntries;
    private static Dictionary<string, Dictionary<int, LocalizationEntry>> _localizations = new();

    public static void Initialize()
    {
        _fallbackEntries = new Dictionary<int, LocalizationEntry>();
        _localizations[FallbackLanguage] = _fallbackEntries;

        _fallbackEntries[1073841] = new LocalizationEntry(
            1073841,
            "Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~ stones",
            "Contents: {0}/{1} items, {2} stones"
        );
    }

    public static bool TryGetLocalization(int number, out LocalizationEntry entry) =>
        TryGetLocalization(FallbackLanguage, number, out entry);

    public static bool TryGetLocalization(string lang, int number, out LocalizationEntry entry)
    {
        if (lang == FallbackLanguage || !_localizations.TryGetValue(lang, out var entries))
        {
            entries = _fallbackEntries;
        }

        return entries.TryGetValue(number, out entry);
    }

    public static string GetText(int number, string lang = FallbackLanguage) =>
        TryGetLocalization(lang, number, out var entry) ? entry.TextSlices[0] : null;

    public static string Formatted(
        int number,
        [InterpolatedStringHandlerArgument("number")]
        ref LocalizationInterpolationHandler handler
    ) => handler.ToStringAndClear();

    public static string Formatted(
        int number,
        string lang,
        [InterpolatedStringHandlerArgument("number", "lang")]
        ref LocalizationInterpolationHandler handler
    ) => handler.ToStringAndClear();
}
