using BenchmarkDotNet.Running;
using Server;

var timerInsertionTest = BenchmarkRunner.Run<BenchmarkTimerInserts>();
var timerExecutionTest = BenchmarkRunner.Run<BenchmarkTimerExecutions>();
