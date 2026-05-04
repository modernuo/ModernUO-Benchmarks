using System.Text;
using BenchmarkDotNet.Running;
using Benchmarks.BenchmarkUtilities;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

BenchmarkRunner.Run<BenchmarkStringHelpers>();