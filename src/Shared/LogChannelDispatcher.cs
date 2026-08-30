using System.Threading.Channels;
using Shared.FileReader;

namespace Shared
{
    public class RowChannelDispatcher
    {
        private readonly Channel<IRow> _channel = Channel.CreateBounded<IRow>(100);

        public void StartConsumers(int count, Func<IRow, Task> handler, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Begin StartConsumersAsync");

            for (int i = 0; i < count; i++)
            {
                _ = Task.Run(async () =>
                {
                    Console.WriteLine("Begin StartConsumersAsync Task.Run");

                    await foreach (IRow? line in _channel.Reader.ReadAllAsync().WithCancellation(cancellationToken))
                    {
                        await handler(line).ConfigureAwait(false);
                    }

                    Console.WriteLine("End StartConsumersAsync Task.Run");
                })
                .ConfigureAwait(false);
            }

            Console.WriteLine("End StartConsumersAsync");
        }

        public async Task EnqueueAsync(IAsyncEnumerable<IRow> lines)
        {
            Console.WriteLine("Begin EnqueueAsync");

            await foreach (IRow line in lines)
            {
                await _channel.Writer.WriteAsync(line).ConfigureAwait(false);
            }

            _channel.Writer.Complete();

            Console.WriteLine("End EnqueueAsync");
        }
    }

    public class LogChannelDispatcher
    {
        private readonly Channel<string> _channel = Channel.CreateBounded<string>(100);

        public Task StartConsumersAsync(int count, Func<string, Task> handler, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Begin StartConsumersAsync");

            for (int i = 0; i < count; i++)
            {
                _ = Task.Run(async () =>
                {
                    Console.WriteLine("Begin StartConsumersAsync Task.Run");

                    await foreach (string? line in _channel.Reader.ReadAllAsync().WithCancellation(cancellationToken))
                    {
                        await handler(line).ConfigureAwait(false);
                    }

                    Console.WriteLine("End StartConsumersAsync Task.Run");
                })
                .ConfigureAwait(false);
            }

            Console.WriteLine("End StartConsumersAsync");

            return Task.CompletedTask;
        }

        public async Task EnqueueAsync(IAsyncEnumerable<string> lines)
        {
            Console.WriteLine("Begin EnqueueAsync");

            await foreach (string line in lines)
            {
                await _channel.Writer.WriteAsync(line).ConfigureAwait(false);
            }

            _channel.Writer.Complete();

            Console.WriteLine("End EnqueueAsync");
        }
    }
}
