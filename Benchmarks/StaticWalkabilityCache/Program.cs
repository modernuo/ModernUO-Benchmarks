using BenchmarkDotNet.Running;

namespace StaticWalkabilityCacheBench;

public static class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
