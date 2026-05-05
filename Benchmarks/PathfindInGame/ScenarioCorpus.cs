using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PathfindInGame;

public static class ScenarioCorpus
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static PathfindScenario[] LoadJsonl(string path)
    {
        var scenarios = new List<PathfindScenario>();

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var scenario = JsonSerializer.Deserialize<PathfindScenario>(line, _jsonOptions);
            if (scenario != null)
            {
                scenarios.Add(scenario);
            }
        }

        return scenarios.ToArray();
    }
}
