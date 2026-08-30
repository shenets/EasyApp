using System.Runtime.CompilerServices;

namespace Shared
{
    public interface IApiClient
    {
        IAsyncEnumerable<string> FetchDataAsync(CancellationToken cancellationToken);
    }

    public class WeatherApiClient : IApiClient
    {
        private readonly HttpClient _http;

        public WeatherApiClient(HttpClient http) => _http = http;

        public async IAsyncEnumerable<string> FetchDataAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _http.GetAsync("https://api.weatherapi.com/v1/current.json?key=demo&q=London", cancellationToken).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            yield return $"Weather: {json}";
        }
    }

    public class CurrencyApiClient : IApiClient
    {
        private readonly HttpClient _http;

        public CurrencyApiClient(HttpClient http)
        {
            _http = http;
        }

        public async IAsyncEnumerable<string> FetchDataAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _http.GetAsync("https://api.exchangerate.host/latest?base=USD&symbols=EUR,PLN", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            yield return $"Currency rates: {json}";
        }
    }

    public class ApiAggregator
    {
        private readonly IEnumerable<IApiClient> _clients;

        public ApiAggregator(IEnumerable<IApiClient> clients) => _clients = clients;

        public async IAsyncEnumerable<string> AggregateAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            List<Task<List<string>>> tasks = _clients
                .Select(client => client.FetchDataAsync(cancellationToken).ToListAsync(cancellationToken).AsTask())
                .ToList();

            while (tasks.Count > 0)
            {
                Task<List<string>> completed = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(completed);

                foreach (string? item in await completed)
                    yield return item;
            }
        }
    }
}
