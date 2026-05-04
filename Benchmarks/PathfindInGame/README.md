# PathfindInGame benchmarks

In-game pathfinding benchmark for ModernUO. Boots the full UOContent fixture (real `Map`/`TileMatrix`/`World` state) and measures `FastAStarAlgorithm.Instance.Find` against a JSONL scenario corpus.

This benchmark differs from the sibling `NPCPathing/` benchmark, which measures A\* node management cost in isolation against a synthetic always-walkable grid. `PathfindInGame` exercises the per-successor `MovementImpl.Check` path that handles slope, static stack, multi tiles, items, mobiles, and z-collision — the actual production hot path.

## Running

From the repo root:

```
dotnet run --project Benchmarks/PathfindInGame/PathfindInGame.csproj -c Release -- --filter "*"
```

For quick iteration:

```
dotnet run --project Benchmarks/PathfindInGame/PathfindInGame.csproj -c Release -- --filter "*" --job short
```

For a smoke test (single iteration, no statistics):

```
dotnet run --project Benchmarks/PathfindInGame/PathfindInGame.csproj -c Release -- --filter "*" --job dry
```

Results land in `BenchmarkDotNet.Artifacts/` under the working directory.

## Scenario corpus

Loaded from `Corpus/baseline.jsonl`. Each line is one `PathfindScenario` (see `PathfindScenario.cs`). Add scenarios by appending lines.

The starter corpus has 4 hand-picked Trammel coordinates. Note that the test map fixture (`TestMapDefinitions`) registers map dimensions but does NOT load real tile data from `Distribution/Data/*.mul`. As a result, the current baseline numbers reflect A\* execution against a sparse map where most cells fail validation, causing the queue to drain to a null result. This produces stable relative measurements but does NOT reflect production cost on a populated server.

To get realistic baseline numbers, capture real scenarios from a running production-shaped server (see next section) and replace the corpus.

## Capturing scenarios from a running server

The ModernUO repo (on branch `feat/ai-pathfinding-optimization`) ships a `PathfindRecorder` at `Projects/UOContent/Engines/Pathing/PathfindRecorder.cs`. To capture real production scenarios:

1. Boot a server pointed at a populated save.
2. As an Administrator, run `[PathRecord on`.
3. Let NPCs/pets path for a while (engage in combat, recall pets, walk through dense areas).
4. Run `[PathRecord off` to flush.
5. The output is at `<server-base>/pathfinds.jsonl` by default (override with `pathfinding.recorder.path` in `Configuration/`).
6. Copy or append the JSONL into `Benchmarks/PathfindInGame/Corpus/baseline.jsonl`.

## Baseline numbers (FastAStarAlgorithm)

Captured on:

```
BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.8246)
12th Gen Intel Core i9-12900K 3.20GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  ShortRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Runtime=.NET 10.0
```

Results (`--job short` + default job, all 4 scenarios, zero allocations):

```
| Method         | Job       | IterationCount | LaunchCount | WarmupCount | scenarioIndex | Mean     | Error     | StdDev   | Allocated |
|--------------- |---------- |--------------- |------------ |------------ |-------------- |---------:|----------:|---------:|----------:|
| FastAStar_Find | .NET 10.0 | Default        | Default     | Default     | 0             | 86.71 ns |  0.755 ns | 0.707 ns |         - |
| FastAStar_Find | ShortRun  | 3              | 1           | 3           | 0             | 86.29 ns | 16.393 ns | 0.899 ns |         - |
| FastAStar_Find | .NET 10.0 | Default        | Default     | Default     | 1             | 88.47 ns |  0.940 ns | 0.834 ns |         - |
| FastAStar_Find | ShortRun  | 3              | 1           | 3           | 1             | 86.70 ns | 33.235 ns | 1.822 ns |         - |
| FastAStar_Find | .NET 10.0 | Default        | Default     | Default     | 2             | 87.47 ns |  1.105 ns | 1.033 ns |         - |
| FastAStar_Find | ShortRun  | 3              | 1           | 3           | 2             | 85.55 ns | 17.452 ns | 0.957 ns |         - |
| FastAStar_Find | .NET 10.0 | Default        | Default     | Default     | 3             | 85.60 ns |  0.700 ns | 0.620 ns |         - |
| FastAStar_Find | ShortRun  | 3              | 1           | 3           | 3             | 89.51 ns |  8.423 ns | 0.462 ns |         - |
```

These numbers are the baseline for comparing future pathfinding optimizations
(static walkability cache, JPS+, residual cache, flow fields). Re-run on the
same hardware after each phase ships and append the comparison table.
