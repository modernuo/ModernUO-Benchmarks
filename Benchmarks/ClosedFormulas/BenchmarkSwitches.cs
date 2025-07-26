using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ClosedFormulas;

[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkSwitches
{
    private static TimeSpan BoatDecayDelay = TimeSpan.FromDays(9.0);
    private static DateTime Now = new(2000, 1, 1);
    private static DateTime TimeOfDecay = Now + BoatDecayDelay * 0.3;

    [Benchmark]
    public int IfStatements()
    {
        var start = Now - (TimeOfDecay - BoatDecayDelay);

        if (start < TimeSpan.FromHours(1.0))
        {
            return 1043010; // This structure is like new.
        }

        if (start < TimeSpan.FromDays(2.0))
        {
            return 1043011; // This structure is slightly worn.
        }

        if (start < TimeSpan.FromDays(3.0))
        {
            return 1043012; // This structure is somewhat worn.
        }

        if (start < TimeSpan.FromDays(4.0))
        {
            return 1043013; // This structure is fairly worn.
        }

        if (start < TimeSpan.FromDays(5.0))
        {
            return 1043014; // This structure is greatly worn.
        }

        return 1043015; // This structure is in danger of collapsing.
    }

    // [Benchmark]
    // public int SwitchExpressions()
    // {
    //     return (int)((Now - (TimeOfDecay - BoatDecayDelay)).Ticks / TimeSpan.TicksPerHour) switch
    //     {
    //         < 1   => 1043010, // This structure is like new.
    //         < 48  => 1043011, // This structure is slightly worn.
    //         < 72  => 1043012, // This structure is somewhat worn.
    //         < 96  => 1043013, // This structure is fairly worn.
    //         < 120 => 1043014, // This structure is greatly worn.
    //         _     => 1043015  // This structure is in danger of collapsing.
    //     };
    // }

    [Benchmark]
    public int SwitchExpressionTS()
    {
        var start = Now - (TimeOfDecay - BoatDecayDelay);
        return start switch
        {
            _ when start < TimeSpan.FromHours(1) => 1043010, // This structure is like new.
            _ when start < TimeSpan.FromDays(2)  => 1043011, // This structure is slightly worn.
            _ when start < TimeSpan.FromDays(3)  => 1043012, // This structure is somewhat worn.
            _ when start < TimeSpan.FromDays(4)  => 1043013, // This structure is fairly worn.
            _ when start < TimeSpan.FromDays(5)  => 1043014, // This structure is greatly worn.
            _     => 1043015  // This structure is in danger of collapsing.
        };
    }
}