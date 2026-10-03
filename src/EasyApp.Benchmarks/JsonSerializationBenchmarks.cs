// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using System;
using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace EasyApp.Benchmarks;

public class SampleData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class JsonSerializationBenchmarks
{
    private SampleData _sampleData = null!;
    private string _jsonString = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sampleData = new SampleData
        {
            Id = 42,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true
        };

        _jsonString = JsonSerializer.Serialize(_sampleData);
    }

    [Benchmark]
    public string JsonSerialize()
    {
        return JsonSerializer.Serialize(_sampleData);
    }

    [Benchmark]
    public SampleData JsonDeserialize()
    {
        return JsonSerializer.Deserialize<SampleData>(_jsonString)!;
    }

    [Benchmark]
    public string JsonSerializeWithOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Serialize(_sampleData, options);
    }
}
