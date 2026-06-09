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

The `ModernUO/` submodule pins to a SHA on `modernuo/ModernUO` `main` (the
pathfinding step-cache work merged upstream in #2478). `git submodule update
--init` works against the public remote — no fork URL or write access required.

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

### Measured

| MaxSearchNodes | open (0) | detour (1) | fail (2) | open alloc | detour alloc | fail alloc |
|---:|--:|--:|--:|--:|--:|--:|
| 300  |  2.10 µs |  22.1 µs |  22.3 µs | 32 B | 0 B  | 0 B  |
| 500  |  2.16 µs |  23.2 µs |  33.5 µs | 32 B | 64 B | 0 B  |
| 1000 |  2.17 µs |  22.5 µs |  79.4 µs | 32 B | 64 B | 0 B  |
| 1500 |  4.27 µs* | 45.2 µs* | 183.8 µs* | 32 B | 64 B | 72 B |
| 2000 |  4.44 µs* | 44.8 µs* | 186.8 µs* | 32 B | 64 B | 72 B |

\* The 1500/2000 rows are ~2× inflated by thermal/clock drift during the later
`[Params]` runs — the `open` control is budget-invariant by construction yet
doubled, so discount that whole column ~2× (or re-run those two params isolated).

**Conclusions:**

- **`open` is budget-insensitive** (~2 µs) — the cap never touches the common case;
  successful searches terminate on goal-found.
- **`detour` needs ≥ 500** to navigate the ~33-step walled-off indoor route; at 300
  it bails and returns `null`. Default 1000 gives 2× margin.
- **`fail` (unreachable) cost rises then plateaus** at window-exhaustion (~1500–1700
  nodes). At 1000 the worst-case failed search is ~79 µs because it bails *before*
  exhausting; pushing to 1500+ raises it to the ~185 µs (≈ ~90 µs de-thermalled)
  plateau for **zero** solving benefit. So **1000 is near-optimal** — above the
  ~500 needed to solve indoor routes, below the window-exhaustion cost ceiling.

**Allocation note.** The only *intentional* allocation in `Find` is the returned
`Direction[]` path (`open` ~6 steps → 32 B, `detour` ~33 steps → 64 B; `detour@300`
= 0 B because it returns `null`). The internal A\* buffers (`_nodes`, `_nodeStates`,
`_path`, `_openQueue`) are `static` singletons — zero-alloc. The search loop itself
is zero-alloc: a single warm *failing* `Find` allocates 0 B at every budget
(300→3000), verified with `GC.GetAllocatedBytesForCurrentThread`. The surprising
`fail` 72 B at ≥ 1500 is therefore **not** a result array (that scenario returns
`null` at all budgets) and **not** the search loop — it's an incidental cache
first-touch (a peripheral `WalkabilityChunk`/strata materialized when the failing
search runs to the window edge, only reached at high budget). Tiny and transient;
it's one more reason to keep the budget modest.

## Multi-pathfinding (synthesizer) benchmark

`MultiPathfindBenchmarks` measures `BitmapAStarAlgorithm.Find` over routes that
detour around placed houses, with the pathfinding cache **on** vs **off**:

- **Cache on** — each multi-covered cell (footprint + 1-cell halo) returns
  `Fallthrough_Multi` and is served by the single-pass `ComputeMultiMaskAt`
  synthesizer (one mask build per cell).
- **Cache off** — each such cell takes the slow path's **8×** per-cell
  `CheckMovement`.

The fixture places a row of guild houses (`MultiScenarios`, via the lightweight
`BenchMulti : BaseMulti`); the routes are validated by a one-time `[MultiHealth]`
stderr audit that asserts each finds a path **and** expands multi cells
(`MultiLocalHits > 0`) — a route that returns NO PATH or hits zero multi cells is
a degenerate fixture and excluded.

```
dotnet run --project Benchmarks/PathfindInGame/PathfindInGame.csproj -c Release -- --filter "*MultiPathfindBenchmarks*"
```

**Requires the `ModernUO/` submodule at the synthesizer branch**
(`server/pathfinding-multi-tests`). Against plain `main` both arms route multi
cells through the slow path and the delta collapses to ~0 (a useful baseline
sanity check, not the win).

### Measured (synthesizer submodule, ShortRun warm)

| Route | multi cells | Cache off (slow path) | Cache on (synthesizer) | Speedup |
|-------|------------:|----------------------:|-----------------------:|--------:|
| `around_w` (48 steps) | 246 | 317.9 µs | 210.5 µs | **1.51×** |
| `around_c` (29 steps) | 154 | 205.2 µs | 141.1 µs | **1.45×** |

Allocations are identical across arms (just the returned `Direction[]` path: 72 B
/ 56 B) — the synthesizer adds no GC pressure. The win is collapsing the per-cell
8× `CheckMovement` to a single multi-aware mask build, so it scales with the count
of multi-covered cells a search expands.

### Per-cell cost breakdown (`MultiSynthesisMicroBenchmarks`)

Why is the end-to-end win "only" ~1.5× and not the 2–6× the static cache shows?
Because per multi-covered cell the synthesizer is a cheaper *computation*, not a
cached *lookup*. Per-cell, on a covered footprint cell of each multi:

| Multi (footprint) | Synthesize (1 build) | Slow path (8× CheckMovement) | Per-cell speedup |
|-------------------|---------------------:|-----------------------------:|-----------------:|
| GuildHouse (15×15) | 737 ns | 857 ns   | 1.16× |
| Tower (24×16)      | 783 ns | 1,139 ns | 1.45× |
| Keep (24×24)       | 771 ns | 1,011 ns | 1.31× |
| Castle (31×32)     | 789 ns | 1,194 ns | 1.51× |

Two findings:

1. **The synthesizer cost is ~flat (~780 ns) across multi sizes** — at one cell it
   resolves only that cell's tile stack, so the multi's overall size/height doesn't
   matter. The slow path's 8× `CheckMovement` instead **grows with multi
   complexity** (more tiles + Z-levels per cell): Castle/Tower ~1,150–1,200 ns vs
   GuildHouse 857 ns. So **bigger/taller multis win more** (1.16× for a guild house
   up to 1.51× for a castle) — a route hugging a castle beats one around a cottage.
2. **The synthesizer is the dominant cache-on cost** (~88% of a multi-heavy `Find`:
   246 cells × ~750 ns ≈ 185 µs of the 210 µs `around_w` arm). It's still a live
   ~780 ns compute, not a ~12 ns cache hit. **That is the headroom for baking multi
   masks from `multi.mul`:** a baked per-`multiID` lookup would replace the ~780 ns
   synthesis with a static-cache-style hit, lifting the multi win from ~1.5× toward
   the static cache's 2–6×+. It applies to fixed multis (boats, classic/contest
   houses, camps — immutable MCL in the art file); foundations (per-instance runtime
   `DesignState`) and boundary cells still resolve live.

## First-run auto-bake

The bench fixture (`BenchmarkFixture.EnsureBakedFiles`) checks that each map
referenced by the corpus has a fresh, fingerprint-valid `<mapId>.swb` file by
trying to open it (`StepCache.TryOpenLazyReader` → `StepCacheFile.OpenForLazy`
validates the embedded TileData fingerprint). If it can't open, the fixture bakes
in-process via `Server.Engines.Pathing.Cache.StepCache.BakeMap`. First run takes
~15 s per map; subsequent runs reuse the cache file and start instantly.

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

### Per-call cost

The companion per-call micro-benchmark (`StaticWalkabilityCache`) has been
removed; its cache-vs-slow-path breakdown is superseded by the end-to-end
numbers above. A single warm cache hit is roughly an order of magnitude cheaper
than a slow-path `MovementImpl.Check` direction call, and A*'s `Find` expands
hundreds of cells per pathfind, so the per-call savings compound into the
path-level deltas.

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
`Server.Engines.Pathing.Cache.StepCacheFile`. The bake side lives in
`Server.Engines.Pathing.Cache.StepCache.BakeMap`.
