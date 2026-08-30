// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace EasyApp.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddJob(Job.Default.WithWarmupCount(3).WithIterationCount(5));

        var _ = BenchmarkRunner.Run(typeof(Program).Assembly, config);
    }
}
