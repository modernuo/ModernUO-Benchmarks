using BenchmarkDotNet.Running;
using Server.Tests;

var runner = BenchmarkRunner.Run<TestWhileInterlocked>();