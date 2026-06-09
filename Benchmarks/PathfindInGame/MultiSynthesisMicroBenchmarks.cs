#nullable enable

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Engines.Pathing.Cache;
using Server.Items;
using Server.Systems.FeatureFlags;
using CalcMoves = Server.Movement.Movement;

namespace PathfindInGame;

/// <summary>
/// Per-cell cost breakdown for multi pathfinding, across multi sizes (GuildHouse, Tower, Keep,
/// Castle), to (a) explain the ~1.5x end-to-end win and (b) size the headroom for a "bake multi
/// masks from multi.mul" optimization.
///
/// - <see cref="Synthesize_MultiCell"/> — the synthesizer's per-cell cost (one multi-aware
///   8-direction mask build). This is exactly what a baked per-multiID mask lookup would replace.
/// - <see cref="SlowPath8x_MultiCell"/> — the old per-cell cost (8x CheckMovement) the synthesizer
///   replaced. The synth/slow ratio per cell drives the end-to-end win; if it grows with multi
///   size/height (more tiles per cell → costlier CheckMovement), big multis win bigger.
///
/// Each multi is placed in an unused band (y=1700), away from the route benchmark's houses
/// (y=1620). The measured cell is the FIRST covered footprint cell (scanned from the MCL), so a
/// big multi with an open courtyard still measures a real multi cell. Requires the synthesizer
/// submodule.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class MultiSynthesisMicroBenchmarks
{
    public enum MultiKind
    {
        Guild = 0x74,
        Tower = 0x7A,
        Keep = 0x7C,
        Castle = 0x7E,
    }

    // multiID -> placement origin. Spaced wide enough that even the Castle (~31x31) footprints
    // never overlap (so each cell measures exactly one multi's contribution).
    private static readonly (MultiKind Kind, int X, int Y)[] _multis =
    {
        (MultiKind.Guild, 1400, 1700),
        (MultiKind.Tower, 1430, 1700),
        (MultiKind.Keep, 1480, 1700),
        (MultiKind.Castle, 1560, 1700),
    };

    private static readonly Dictionary<MultiKind, BenchMulti> _placed = new();
    private static bool _placedOnce;

    private Map _map = null!;
    private StubCreature _stub = null!;
    private int _x;
    private int _y;
    private sbyte _z;
    private Point3D _loc;

    [Params(MultiKind.Guild, MultiKind.Tower, MultiKind.Keep, MultiKind.Castle)]
    public MultiKind Kind;

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkFixture.EnsureInitialized();
        ContentFeatureFlags.BitmapPathfindingCache = true;
        _map = Map.Maps[MultiScenarios.MapId];

        if (!_placedOnce)
        {
            foreach (var m in _multis)
            {
                var multi = new BenchMulti((int)m.Kind);
                multi.MoveToWorld(new Point3D(m.X, m.Y, _map.GetAverageZ(m.X, m.Y)), _map);
                _placed[m.Kind] = multi;
            }
            _placedOnce = true;
        }

        var (cx, cy) = FirstCoveredCell(_placed[Kind]);
        _x = cx;
        _y = cy;
        _z = (sbyte)_map.GetAverageZ(_x, _y);
        _loc = new Point3D(_x, _y, _z);

        _stub = new StubCreature();
        _stub.MoveToWorld(_loc, _map);

        // One-time coverage sanity to stderr: confirm the measured cell really carries multi tiles
        // (else the synth/slow comparison would be a static-cell control, not a multi cell).
        var mcl = _placed[Kind].Components;
        var lx = _x - _placed[Kind].X + mcl.Center.X;
        var ly = _y - _placed[Kind].Y + mcl.Center.Y;
        var multiTiles = lx >= 0 && ly >= 0 && lx < mcl.Width && ly < mcl.Height ? mcl.Tiles[lx][ly].Length : 0;
        System.Console.Error.WriteLine(
            $"[MicroCell] {Kind} footprint {mcl.Width}x{mcl.Height} -> cell ({_x},{_y}) multiTiles={multiTiles}"
        );

        // Warm TileData / MCL resolution so the measured calls aren't paying first-touch.
        StepProbe.ComputeMultiMaskAt(_map, _x, _y, _z);
        for (var d = 0; d < 8; d++)
        {
            CalcMoves.CheckMovement(_stub, _map, _loc, (Direction)d, out _);
        }
    }

    private static (int X, int Y) FirstCoveredCell(BaseMulti multi)
    {
        var mcl = multi.Components;
        for (var ly = 0; ly < mcl.Height; ly++)
        {
            for (var lx = 0; lx < mcl.Width; lx++)
            {
                if (mcl.Tiles[lx][ly].Length > 0)
                {
                    return (multi.X - mcl.Center.X + lx, multi.Y - mcl.Center.Y + ly);
                }
            }
        }
        return (multi.X, multi.Y);
    }

    /// <summary>The synthesizer's whole-cell cost: one multi-aware 8-direction mask build.</summary>
    [Benchmark]
    public StepMask Synthesize_MultiCell() => StepProbe.ComputeMultiMaskAt(_map, _x, _y, _z);

    /// <summary>The slow path's whole-cell cost the synthesizer replaced: 8x CheckMovement.</summary>
    [Benchmark(Baseline = true)]
    public int SlowPath8x_MultiCell()
    {
        var ok = 0;
        for (var d = 0; d < 8; d++)
        {
            if (CalcMoves.CheckMovement(_stub, _map, _loc, (Direction)d, out _))
            {
                ok++;
            }
        }
        return ok;
    }
}
