using System;
using MoveImpl = Server.Movement.MovementImpl;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NPCPathing;

namespace Server.PathAlgorithms.FastAStar;

public class FastAStar_PQueue
{
    public struct PathNode
    {
        public int cost, total;
        public int parent;
        public int z;
    }

    private const int MaxDepth = 300;
    private const int AreaSize = 38;

    private const int NodeCount = AreaSize * AreaSize * PlaneCount;

    private const int PlaneOffset = 128;
    private const int PlaneCount = 13;
    private const int PlaneHeight = 20;

    private static readonly Direction[] _path = new Direction[AreaSize * AreaSize];
    private static readonly PathNode[] _nodes = new PathNode[NodeCount];
    private static readonly byte[] _nodeStates = new byte[NodeCount];
    private static readonly int[] _successors = new int[8];
    private static readonly PriorityQueue<int, int> _openQueue = new(AreaSize * AreaSize);

    private static int _xOffset;
    private static int _yOffset;

    private Point3D _goal;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Heuristic(int x, int y, int z)
    {
        x -= _goal.X - _xOffset;
        y -= _goal.Y - _yOffset;
        z -= _goal.Z;

        x *= 11;
        y *= 11;

        return x * x + y * y + z * z;
    }

    public Direction[] Find(Point3D start, Point3D goal)
    {
        if (!Utility.InRange(start, goal, AreaSize))
        {
            return null;
        }

        Array.Clear(_nodeStates);

        _goal = goal;

        _xOffset = (start.X + goal.X - AreaSize) / 2;
        _yOffset = (start.Y + goal.Y - AreaSize) / 2;

        var fromNode = GetIndex(start.X, start.Y, start.Z);
        var destNode = GetIndex(goal.X, goal.Y, goal.Z);

        _nodes[fromNode].cost = 0;
        _nodes[fromNode].total = Heuristic(start.X - _xOffset, start.Y - _yOffset, start.Z);
        _nodes[fromNode].parent = -1;
        _nodes[fromNode].z = start.Z;

        _openQueue.Enqueue(fromNode, _nodes[fromNode].total);
        _nodeStates[fromNode] = 1;

        int backtrack = 0, depth = 0;

        var path = _path;
        var vals = _successors;

        while (_openQueue.Count > 0)
        {
            if (++depth > MaxDepth)
            {
                break;
            }

            if (!_openQueue.TryDequeue(out var bestNode, out var bestTotal))
            {
                break;
            }

            // Duplicate, lower priority
            if (_nodeStates[bestNode] == 2 || _nodes[bestNode].total != bestTotal)
            {
                continue;
            }

            MoveImpl.Goal = goal;

            var count = GetSuccessors(bestNode);

            MoveImpl.AlwaysIgnoreDoors = false;
            MoveImpl.IgnoreMovableImpassables = false;
            MoveImpl.Goal = Point3D.Zero;

            if (count == 0)
            {
                continue;
            }

            for (var i = 0; i < count; ++i)
            {
                var newNode = vals[i];

                if (_nodeStates[newNode] == 2)
                {
                    continue;
                }

                var moveCost = i % 2 == 1 ? 14 : 10;
                var newCost = _nodes[bestNode].cost + moveCost;
                var newTotal = newCost + Heuristic(
                    newNode % AreaSize,
                    newNode / AreaSize % AreaSize,
                    _nodes[newNode].z
                );

                if (_nodeStates[newNode] == 0 || newTotal < _nodes[newNode].total)
                {
                    _nodes[newNode].parent = bestNode;
                    _nodes[newNode].cost = newCost;
                    _nodes[newNode].total = newTotal;
                    _openQueue.Enqueue(newNode, newTotal);
                    _nodeStates[newNode] = 1;
                }

                if (newNode != destNode)
                {
                    continue;
                }

                var pathCount = 0;
                var parent = _nodes[newNode].parent;

                while (parent != -1)
                {
                    path[pathCount++] = PathAlgorithm.GetDirection(
                        parent % AreaSize,
                        parent / AreaSize % AreaSize,
                        newNode % AreaSize,
                        newNode / AreaSize % AreaSize
                    );
                    newNode = parent;
                    parent = _nodes[newNode].parent;

                    if (newNode == fromNode)
                    {
                        break;
                    }
                }

                var dirs = new Direction[pathCount];

                while (pathCount > 0)
                {
                    dirs[backtrack++] = path[--pathCount];
                }

                _openQueue.Clear();
                return dirs;
            }
        }

        _openQueue.Clear();
        return null;
    }

    private static int GetIndex(int x, int y, int z)
    {
        x -= _xOffset;
        y -= _yOffset;
        z += PlaneOffset;
        z /= PlaneHeight;

        return x + y * AreaSize + z * AreaSize * AreaSize;
    }

    public static int GetSuccessors(int p)
    {
        var px = p % AreaSize;
        var py = p / AreaSize % AreaSize;
        var pz = _nodes[p].z;

        var p3D = new Point3D(px + _xOffset, py + _yOffset, pz);

        var count = 0;

        for (var i = 0; i < 8; ++i)
        {
            int x;
            int y;
            switch (i)
            {
                default: // 0
                    x = 0;
                    y = -1;
                    break;
                case 1:
                    x = 1;
                    y = -1;
                    break;
                case 2:
                    x = 1;
                    y = 0;
                    break;
                case 3:
                    x = 1;
                    y = 1;
                    break;
                case 4:
                    x = 0;
                    y = 1;
                    break;
                case 5:
                    x = -1;
                    y = 1;
                    break;
                case 6:
                    x = -1;
                    y = 0;
                    break;
                case 7:
                    x = -1;
                    y = -1;
                    break;
            }

            x += px;
            y += py;

            if (x is < 0 or >= AreaSize || y is < 0 or >= AreaSize)
            {
                continue;
            }

            if (MovementScenarios.CalcMoves(p3D, (Direction)i, out var z))
            {
                var idx = GetIndex(x + _xOffset, y + _yOffset, z);

                if (idx >= 0 && idx < NodeCount)
                {
                    _nodes[idx].z = z;
                    _successors[count++] = idx;
                }
            }
        }

        return count;
    }
}