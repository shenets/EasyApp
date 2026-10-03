// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace EasyApp.Benchmarks;

/// <summary>
/// Benchmark class for measuring the performance of SetBusinessRulesExternalIdsOperation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class OperationBenchmark
{
    private ISetBusinessRulesExternalIdsOperation<TestContext> _operation = null!;
    private TestContext _testContext = null!;
    private IBrokerLogger _logger = null!;
    private ICategoryRepository _categoryRepository = null!;
    private IPrioritiesRepository _prioritiesRepository = null!;
    private ITradeRepository _tradeRepository = null!;

    [GlobalSetup]
    public void Setup()
    {
        _logger = new BrokerLogger();
        _categoryRepository = new CategoryRepository();
        _prioritiesRepository = new PrioritiesRepository();
        _tradeRepository = new TradeRepository();

        _operation = new SetBusinessRulesExternalIdsOperation<TestContext>(
            _logger,
            new Lazy<ICategoryRepository>(_categoryRepository),
            new Lazy<IPrioritiesRepository>(_prioritiesRepository),
            new Lazy<ITradeRepository>(_tradeRepository));

        _testContext = new TestContext
        {
            Action = ActionEnum.CreateWorkOrderUpStream,
            IsDownStream = false,
            ClientIdentification = new ClientIdentification { SubscriberId = 1, ExternalClientId = 100 },
            BusinessRulesTarget = new WorkOrderDto
            {
                Id = 1,
                Category = "High",
                Priority = "Critical",
                Trade = "Plumbing",
                Description = "Test Work Order"
            }
        };
    }

    [Benchmark]
    public async Task ExecuteOperationAsync()
    {
        await _operation.ExecuteAsync(_testContext).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ExecuteOperationWithCategoryAsync()
    {
        var context = new TestContext
        {
            Action = ActionEnum.UpdateCategoryUpStream,
            IsDownStream = false,
            ClientIdentification = new ClientIdentification { SubscriberId = 1, ExternalClientId = 100 },
            BusinessRulesTarget = new WorkOrderDto
            {
                Id = 1,
                Category = "Medium",
                Description = "Update Category Test"
            }
        };

        await _operation.ExecuteAsync(context).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ExecuteOperationWithPriorityAsync()
    {
        var context = new TestContext
        {
            Action = ActionEnum.UpdatePriorityUpStream,
            IsDownStream = false,
            ClientIdentification = new ClientIdentification { SubscriberId = 1, ExternalClientId = 100 },
            BusinessRulesTarget = new WorkOrderDto
            {
                Id = 1,
                Priority = "High",
                Description = "Update Priority Test"
            }
        };

        await _operation.ExecuteAsync(context).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ExecuteOperationWithAllFieldsAsync()
    {
        var context = new TestContext
        {
            Action = ActionEnum.CreateWorkOrderDownStream,
            IsDownStream = true,
            ClientIdentification = new ClientIdentification { SubscriberId = 1, ExternalClientId = null },
            BusinessRulesTarget = new WorkOrderDto
            {
                Id = 1,
                Category = "High",
                Priority = "Critical",
                Trade = "Electrical",
                Description = "Full Test Work Order"
            }
        };

        await _operation.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Test context implementation for benchmarking.
    /// </summary>
    private class TestContext : IBusinessRulesContext, IActionContext, IExternalClientContext, IRequestIdentityContext
    {
        public ActionEnum Action { get; set; }
        public bool IsDownStream { get; set; }
        public ClientIdentification ClientIdentification { get; set; } = null!;
        public WorkOrderDto BusinessRulesTarget { get; set; } = null!;
    }
}
