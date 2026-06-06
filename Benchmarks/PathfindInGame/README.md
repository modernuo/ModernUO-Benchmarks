# PathfindInGame benchmarks

End-to-end pathfinding benchmark for ModernUO. Boots the full UOContent
fixture (real `Map`/`TileMatrix`/`World` state, real `.mul` tile data) and
measures `BitmapAStarAlgorithm.Instance.Find` against a JSONL scenario corpus.

This benchmark complements the sibling `NPCPathing/` benchmark (which measures
A\* node-management cost in isolation against a synthetic always-walkable grid)
by exercising the per-cell static walkability cache and the production
`MovementImpl.Check` slow path. Cells with multi-Z surfaces, source-Z
mismatches, multis, items, mobiles, and z-collision all participate exactly
as they would on a live shard.

## Submodule prerequisite

The `ModernUO/` submodule pins to a SHA on the `feat/ai-pathfinding-optimization`
branch. Until that branch merges upstream, you'll need either write access to
`modernuo/ModernUO.git` (push the branch) or a fork URL configured in
`.gitmodules`. `git submodule update --init` will fail with "remote ref does
not exist" otherwise.

## Running

From the repo root:

```
dotnet run --project Benchmarks/PathfindInGame/PathfindInGame.csproj -c Release -- --filter "*"
```

Quick smoke (a single iteration, no statistics):

```
dotnet run --project Benchmarks/PathfindInGame/PathfindInGame.csproj -c Release -- --filter "*" --job dry
```

Results land in `BenchmarkDotNet.Artifacts/`.

## MaxSearchNodes sweep

`MaxSearchNodesBenchmarks` sweeps the A\* node-expansion budget
(`BitmapAStarAlgorithm.MaxSearchNodes`, the `pathfinding.maxSearchNodes` shard
setting, default 1000) over three shapes to size it without perf regressions:

- **open** — short open-terrain path; A\* terminates on goal-found, so this is
  budget-insensitive (the control — raising the budget must not slow it).
- **detour** — a ~33-step walled-off route around the Britain Inn; returns NULL
  below ~500 expansions, found above it.
- **fail** — an unreachable upstairs goal; the search runs to the full budget, so
  this is the worst-case per-`Find` cost. Rises with budget then plateaus (~1500)
  once the 38-tile window is exhausted.

```
dotnet run --project Benchmarks/PathfindInGame/PathfindInGame.csproj -c Release -- --filter "*MaxSearchNodesBenchmarks*"
```

**Prerequisite:** the `ModernUO/` submodule must be at a commit that includes the
`BitmapAStarAlgorithm.MaxSearchNodes` field (the `pathfinding.maxSearchNodes`
change). Bump the submodule first, or the project won't compile.

## First-run auto-bake

The bench fixture (`BenchmarkFixture.EnsureWalkabilityCacheBaked`) checks
that each map referenced by the corpus has a fresh `<mapId>.swb` file with a
matching tile-data hash. If the file is missing, stale, or malformed, the
fixture calls `Server.Engines.Pathing.Cache.WalkabilityCacheBaker.BakeMap`
in-process. First run takes ~15 s per map; subsequent runs reuse the cache
file and start instantly.

Override the cache directory with `MODERNUO_PATHFINDING_DATA_DIR`. Override
the UO client data with `MODERNUO_TEST_DATA_DIR` (defaults to
`C:\Ultima Online Classic` on Windows).

## Scenario corpus

`Corpus/baseline.jsonl` — five hand-picked Trammel scenarios covering the
production cost surface:

| Idx | Name | Path | Why |
|-----|------|------|-----|
| 0 | trammel_open_plain | (1496,1628,10) → (1530,1660,10) | Long open-terrain path; cache hits dominate |
| 1 | trammel_dense_forest_yew | (585,845,0) → (615,875,0) | Tier 2 chunks with obstacle statics (trees) |
| 2 | trammel_corridor_brit_sewer | (1418,1696,-32) → (1450,1720,-32) | Tight corridor with stairs, dense multi-Z |
| 3 | trammel_multifloor_brit_inn | (1494,1626,20) → (1502,1632,20) | Multi-floor inn — exercises Tier 4 strata |
| 4 | trammel_britain_causeway | (1478,1647,0) → (1502,1647,0) | Causeway over courtyard — multi-Z paver region |

Add scenarios by appending lines to `baseline.jsonl`. To capture real
production scenarios, use the in-server `[PathRecord on` admin command;
output goes to `<server-base>/pathfinds.jsonl`.

## Provider matrix

`PathProvider` enumerates the production-relevant cache configurations:

- **`Cold`** — no `.swb` loaded, cache cleared each iteration. Reference
  baseline showing the runtime-baker-only cost an operator gets if they
  never run `[BuildPathingCache`. First-pathfind-after-boot worst case.
- **`PrecomputedColdTier4`** — `.swb` loaded in `[GlobalSetup]`, cache cleared
  each iteration. Measures "first pathfind after boot" — chunks reload from
  the file each iteration, Tier 4 strata absorb the multi-Z / source-Z
  fallthroughs that the runtime baker would have left as slow-path dispatches.
- **`PrecomputedWarmTier4`** — `.swb` loaded + cache resident across
  iterations. Production steady-state shape: file-loaded chunks resident,
  every fallthrough absorbed by Tier 4. Lowest mean per-call latency we ship.

3 providers × 5 scenarios = 15 entries.

## Cumulative perf story

Numbers below are `Mean` from a clean BDN run on the i9-12900K reference
hardware. Each row shows what shipped at each milestone. Every milestone
is a strict improvement over its predecessor for the production-shape
column (`PrecomputedWarmTier4`); the `Cold` column drifts up because each
milestone exposes the cache to more scenarios that the runtime baker now
has to work for.

| Milestone | S0 plain | S1 forest | S2 sewer | S3 inn | S4 causeway |
|-----------|---------:|----------:|---------:|-------:|------------:|
| **FastAStarAlgorithm** (no cache, baseline) | ~3,500 µs | ~4,000 µs | ~36 µs | ~2,300 µs | ~1,000 µs |
| Cache + Tier 0/1/2/3 file | 256 µs | 257 µs | 44 µs | 2,309 µs | 16.7 µs |
| `IsDefaultWalker` capability fix | 254 µs | 261 µs | 34 µs | 297 µs | 14.5 µs |
| zstd compression (no perf change, -90% disk) | 254 µs | 261 µs | 34 µs | 297 µs | 14.5 µs |
| **Tier 4 strata (current)** | **218 µs** | **209 µs** | **22 µs** | **198 µs** | **14 µs** |

S3 (multi-floor inn) is the headline: dropped from 2,309 µs to 198 µs once
Tier 4 strata absorb the per-source-Z answers that the cache previously
couldn't model. All scenarios now sit at or below 250 µs steady-state.

### Latest measured numbers (v6)

```
| Method         | Provider             | scenarioIndex |   Mean        | Allocated |
|--------------- |--------------------- |-------------- |--------------:|----------:|
| FastAStar_Find | Cold                 | 0             | 2,885.93 us   |   44,736 B |
| FastAStar_Find | Cold                 | 1             | 3,387.89 us   |   67,104 B |
| FastAStar_Find | Cold                 | 2             | 1,043.17 us   |   11,184 B |
| FastAStar_Find | Cold                 | 3             | 3,535.03 us   |   44,736 B |
| FastAStar_Find | Cold                 | 4             | 1,283.04 us   |   11,184 B |
| FastAStar_Find | PrecomputedColdTier4 | 0             |   273.55 us   |   33,224 B |
| FastAStar_Find | PrecomputedColdTier4 | 1             |   272.93 us   |   23,312 B |
| FastAStar_Find | PrecomputedColdTier4 | 2             |    54.81 us   |    2,992 B |
| FastAStar_Find | PrecomputedColdTier4 | 3             |   267.04 us   |   35,120 B |
| FastAStar_Find | PrecomputedColdTier4 | 4             |    56.99 us   |    6,656 B |
| FastAStar_Find | PrecomputedWarmTier4 | 0             |   218.23 us   |          - |
| FastAStar_Find | PrecomputedWarmTier4 | 1             |   209.22 us   |          - |
| FastAStar_Find | PrecomputedWarmTier4 | 2             |    22.22 us   |          - |
| FastAStar_Find | PrecomputedWarmTier4 | 3             |   198.20 us   |          - |
| FastAStar_Find | PrecomputedWarmTier4 | 4             |    13.82 us   |          - |
```

`PrecomputedWarmTier4` shows **zero allocations** across all scenarios — the
production steady-state path doesn't touch the GC. `PrecomputedColdTier4`'s
allocations come from the per-chunk `WalkabilityChunk` + `Strata` buffers
materialised on first-touch from the file; LRU eviction reclaims them.

### Per-call cost (companion `Benchmarks/StaticWalkabilityCache/`)

For the per-call breakdown of cache vs slow-path cost, see the sibling
`StaticWalkabilityCache` benchmark. Headline numbers from the same run:

```
| Method              |          Mean | Allocated |
|-------------------- |--------------:|----------:|
| CacheHit_Warm       |      12.08 ns |         - |  ← cache-hit hot path
| Cache_FullRoundTrip |      14.21 ns |         - |  ← full TryGetMask incl. guards
| MovementImpl_Direct |      66.11 ns |         - |  ← slow-path single direction
| Baker_Direct        |     259.09 ns |         - |  ← one cell ComputeMaskAt
| CacheHit_Cold       | 792,168.57 ns |   11,184 B|  ← Clear + first hit (chunk build)
```

A single cache hit costs **~5.5× less** than a single slow-path direction
call. A's Find typically expands hundreds of cells per pathfind, so the
per-call savings compound into the path-level deltas above.

## Disk footprint and bake time

All six facets, current format (v6 — interval-encoded Tier 4 strata):

| Map | File size | Tier 3 zstd ratio |
|-----|----------:|-------------------|
| 0 Felucca | 9.09 MB | ~91% saved |
| 1 Trammel | 9.11 MB | ~91% saved |
| 2 Ilshenar | 2.06 MB | ~89% saved |
| 3 Malas | 1.87 MB | ~92% saved |
| 4 Tokuno | 1.08 MB | ~88% saved |
| 5 TerMur | 2.26 MB | ~88% saved |
| **Total** | **25.49 MB** | — |

Bake time (all 6 facets, sequential): ~45 s on the reference hardware.

## Hardware reference

```
BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.8246)
12th Gen Intel Core i9-12900K 3.20GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
```

When re-running on different hardware, append a new section with the
machine info and the cumulative perf table re-captured against the same
five scenarios.

## File-format reference

The `<mapId>.swb` format is documented at the top of
`Server.Engines.Pathing.Cache.PrecomputedCacheFile`. Current version: 6.
The bake side lives in `Server.Engines.Pathing.Cache.WalkabilityCacheBaker`.
