// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace EasyApp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CollectionBenchmarks
{
    private List<int> _listData = null!;
    private int[] _arrayData = null!;
    private const int DataSize = 10000;

    [GlobalSetup]
    public void Setup()
    {
        _arrayData = Enumerable.Range(0, DataSize).ToArray();
        _listData = Enumerable.Range(0, DataSize).ToList();
    }

    [Benchmark]
    public int ArrayIteration()
    {
        int sum = 0;
        foreach (var item in _arrayData)
        {
            sum += item;
        }
        return sum;
    }

    [Benchmark]
    public int ListIteration()
    {
        int sum = 0;
        foreach (var item in _listData)
        {
            sum += item;
        }
        return sum;
    }

    [Benchmark]
    public int ListLinqSum()
    {
        return _listData.Sum();
    }

    [Benchmark]
    public bool ListContains()
    {
        return _listData.Contains(5000);
    }
}
