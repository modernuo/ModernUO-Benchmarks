using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Server.Tests;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class TestWhileInterlocked
{
    public const uint EntityOffset = 0x40000000;
    public const uint MaxEntitySerial = 0x7EEEEEEE;
    private uint _entitySerial;

    public const uint ResetVirtualSerial = MaxEntitySerial;
    public const uint MaxVirtualSerial = 0x7FFFFFFF;
    private static uint _nextVirtualSerial = ResetVirtualSerial;

    private readonly Dictionary<Serial, bool> _entities = [];

    public Serial NewEntity
    {
        get
        {
            var value = _entitySerial > MaxEntitySerial ? EntityOffset : _entitySerial;
            _entitySerial = value + 1;
            return (Serial)value;

            // for (uint i = 0; i < MaxEntitySerial; i++)
            // {
            //     last++;
            //
            //     if (last > max)
            //     {
            //         last = (Serial)EntityOffset;
            //     }
            //
            //     if (!_entities.ContainsKey(last))
            //     {
            //         return _entitySerial = last;
            //     }
            // }

            // return Serial.MinusOne;
        }
    }

    // public static Serial NewVirtual
    // {
    //     get
    //     {
    //         var value = _nextVirtualSerial > MaxVirtualSerial ? ResetVirtualSerial : _nextVirtualSerial;
    //         _nextVirtualSerial = value + 1;
    //         return (Serial)value;
    //     }
    // }

    // public static Serial NewVirtual
    // {
    //     get
    //     {
    //         // Guarantee unique serials without locking
    //         uint newValue;
    //         uint value;
    //         do
    //         {
    //             value = _nextVirtualSerial;
    //             newValue = value == MaxVirtualSerial ? ResetVirtualSerial : value + 1;
    //
    //             // Atomically set NextVirtualSerial to newValue if it hasn't changed
    //         } while (Interlocked.CompareExchange(ref _nextVirtualSerial, newValue, value) != value);
    //
    //         return (Serial)value;
    //     }
    // }

    [Benchmark]
    public Serial TestWhileNewEntitySerial()
    {
        var result = Serial.MinusOne;
        for (var i = 0; i < 50000; i++)
        {
            result = NewEntity;
        }

        return result;
    }

    // [Benchmark]
    // public Serial TestWhileNewVirtualSerial()
    // {
    //     var result = Serial.MinusOne;
    //     for (var i = 0; i < 50000; i++)
    //     {
    //         result = NewVirtual;
    //     }
    //
    //     return result;
    // }
}