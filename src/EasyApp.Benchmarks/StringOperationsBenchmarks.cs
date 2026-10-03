// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using System;
using BenchmarkDotNet.Attributes;

namespace EasyApp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class StringOperationsBenchmarks
{
    private string _testString = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testString = new string('a', 10000);
    }

    [Benchmark]
    public string Concat()
    {
        return string.Concat(_testString, _testString);
    }

    [Benchmark]
    public string StringBuilderConcat()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(_testString);
        builder.Append(_testString);
        return builder.ToString();
    }

    [Benchmark]
    public int SubstringLength()
    {
        return _testString.Substring(0, 100).Length;
    }
}
