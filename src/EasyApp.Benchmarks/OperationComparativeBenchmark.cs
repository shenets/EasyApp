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
/// Benchmark class for comparing two implementations of SetBusinessRulesExternalIdsOperation.
/// This benchmark measures performance differences between the original and optimized version.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class OperationComparativeBenchmark
{
    private ISetBusinessRulesExternalIdsOperation<TestContext> _operation1 = null!;
    private ISetBusinessRulesExternalIdsOperation<TestContext> _operation2 = null!;
    private TestContext _testContext = null!;
    private TestContext _categoryTestContext = null!;
    private TestContext _priorityTestContext = null!;
    private TestContext _tradeTestContext = null!;
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

        // Initialize first implementation
        _operation1 = new SetBusinessRulesExternalIdsOperation<TestContext>(
            _logger,
            new Lazy<ICategoryRepository>(_categoryRepository),
            new Lazy<IPrioritiesRepository>(_prioritiesRepository),
            new Lazy<ITradeRepository>(_tradeRepository));

        // Initialize second implementation
        _operation2 = new SetBusinessRulesExternalIdsOperation2<TestContext>(
            new Lazy<ICategoryRepository>(_categoryRepository),
            new Lazy<IPrioritiesRepository>(_prioritiesRepository),
            new Lazy<ITradeRepository>(_tradeRepository));

        // Test context with all fields
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

        // Category update context
        _categoryTestContext = new TestContext
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

        // Priority update context
        _priorityTestContext = new TestContext
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

        // Trade update context
        _tradeTestContext = new TestContext
        {
            Action = ActionEnum.CreateWorkOrderDownStream,
            IsDownStream = true,
            ClientIdentification = new ClientIdentification { SubscriberId = 1, ExternalClientId = null },
            BusinessRulesTarget = new WorkOrderDto
            {
                Id = 1,
                Trade = "Electrical",
                Description = "Trade Test"
            }
        };
    }

    #region Implementation 1 (Original with Logger)

    [Benchmark(Description = "V1: Full Operation")]
    public async Task Operation1_FullAsync()
    {
        await _operation1.ExecuteAsync(_testContext).ConfigureAwait(false);
    }

    [Benchmark(Description = "V1: Category Update")]
    public async Task Operation1_CategoryUpdateAsync()
    {
        await _operation1.ExecuteAsync(_categoryTestContext).ConfigureAwait(false);
    }

    [Benchmark(Description = "V1: Priority Update")]
    public async Task Operation1_PriorityUpdateAsync()
    {
        await _operation1.ExecuteAsync(_priorityTestContext).ConfigureAwait(false);
    }

    [Benchmark(Description = "V1: Trade Update")]
    public async Task Operation1_TradeUpdateAsync()
    {
        await _operation1.ExecuteAsync(_tradeTestContext).ConfigureAwait(false);
    }

    #endregion

    #region Implementation 2 (Optimized without Logger)

    [Benchmark(Description = "V2: Full Operation")]
    public async Task Operation2_FullAsync()
    {
        await _operation2.ExecuteAsync(_testContext).ConfigureAwait(false);
    }

    [Benchmark(Description = "V2: Category Update")]
    public async Task Operation2_CategoryUpdateAsync()
    {
        await _operation2.ExecuteAsync(_categoryTestContext).ConfigureAwait(false);
    }

    [Benchmark(Description = "V2: Priority Update")]
    public async Task Operation2_PriorityUpdateAsync()
    {
        await _operation2.ExecuteAsync(_priorityTestContext).ConfigureAwait(false);
    }

    [Benchmark(Description = "V2: Trade Update")]
    public async Task Operation2_TradeUpdateAsync()
    {
        await _operation2.ExecuteAsync(_tradeTestContext).ConfigureAwait(false);
    }

    #endregion

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
