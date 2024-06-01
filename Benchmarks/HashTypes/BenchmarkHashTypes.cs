using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Standart.Hash.xxHash;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkHashTypes
{
    private List<Type> _types;
    private Type _typeToCheck;
    private string _typeName;
    private ulong _typeHash;
    private Dictionary<ulong, Type> _typeDictHash;
    private Dictionary<string, Type> _typeDictString;

    [GlobalSetup]
    public void SetUp()
    {
        var currentAssembly = Assembly.GetEntryAssembly();
        var cwd = Path.GetDirectoryName(currentAssembly.Location);
        var ass = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Join(cwd, "./UOContent.dll"));
        _types = ass?.GetTypes().ToList();

        _typeToCheck = _types[^1];
        _typeName = string.Intern(_typeToCheck.FullName!);
        _typeHash = xxHash3.ComputeHash(_typeName);
        _typeDictHash = new Dictionary<ulong, Type>
        {
            [_typeHash] = _typeToCheck
        };
        
        _typeDictString = new Dictionary<string, Type>
        {
            [_typeName] = _typeToCheck
        };
    }

    // SetTypeRef in RunUO
    [Benchmark]
    public ulong BenchmarkIndexLookup() => (ulong)_types.IndexOf(_typeToCheck);

    // Basic hash you can find online, no intrinsic optimizations
    [Benchmark]
    public int BenchmarkBasicHash()
    {
        var name = _typeToCheck.FullName;
        var hash = name!.Length;
    
        unchecked
        {
            var span = name.AsSpan();
    
            for (var i = 0; i < span.Length; i++)
            {
                hash = (hash * 397) ^ span[i];
            }
        }
    
        return hash;
    }

    [Benchmark]
    public ulong BenchmarkXXHash() => xxHash3.ComputeHash(_typeToCheck.FullName);

    // Compare Dictionary<ulong, T> to Dictionary<string, T>
    [Benchmark]
    public Type BenchmarkDictionaryHash() => _typeDictHash.TryGetValue(_typeHash, out var type) ? type : null;
    
    [Benchmark]
    public Type BenchmarkDictionaryString() => _typeDictString.TryGetValue(_typeToCheck.FullName!, out var type) ? type : null;
}
