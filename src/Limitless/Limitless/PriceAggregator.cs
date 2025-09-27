using Alpaca.Markets;

namespace Limitless
{
    public class PriceAggregator
    {
        private readonly Configuration _launchSettings;
        private IAlpacaDataClient dataClient;

        // Storage
        private readonly Dictionary<string, List<IBar>> _symbolToBars = new();
        private readonly Dictionary<string, decimal> _symbolToBeginningOfDayPrice = new();

        public PriceAggregator(Configuration launchSettings, IAlpacaDataClient client)
        {
            _launchSettings = launchSettings;
            dataClient = client;
        }

        private async Task<IReadOnlyList<IBar>> RequestGetBarsAsync(IEnumerable<string> symbols, DateTime start, DateTime end)
        {
            var request = new HistoricalBarsRequest(symbols, start, end, BarTimeFrame.Minute);
            
            Console.WriteLine($"Requesting aggregated bar data from {start:u} to {end:u} ...");

            var bars = await dataClient.ListHistoricalBarsAsync(request);

            Console.WriteLine("Request complete.");

            return bars.Items;
        }

        public async Task LoadBarsAsync(IEnumerable<string> symbols, DateTime start, DateTime end)
        {
            var barsBySymbol = await RequestGetBarsAsync(symbols, start, end);

            /*
            foreach (var symbol in symbols)
            {
                if (barsBySymbol.TryGetValue(symbol, out var bars))
                {
                    _symbolToBars[symbol] = new List<IBar>(bars);
                }
            }
            */
        }

        public async Task AppendBarsUntilTimeAsync(IEnumerable<string> symbols, DateTime end)
        {
            string? mostOutOfDateSymbol = null;
            DateTime mostOutOfDateTime = DateTime.UtcNow;

            foreach (var symbol in symbols)
            {
                if (_symbolToBars.TryGetValue(symbol, out var bars) && bars.Any())
                {
                    var latest = bars.Max(b => b.TimeUtc);
                    if (latest < mostOutOfDateTime)
                    {
                        mostOutOfDateTime = latest;
                        mostOutOfDateSymbol = symbol;
                    }
                }
            }

            Console.WriteLine($"MOST OUT OF DATE TIME: {mostOutOfDateTime:u}");

            var barsBySymbol = await RequestGetBarsAsync(symbols, mostOutOfDateTime, end);

            foreach (var symbol in symbols)
            {
                /*
                if (barsBySymbol.TryGetValue(symbol, out var newBars))
                {
                    if (!_symbolToBars.ContainsKey(symbol))
                    {
                        _symbolToBars[symbol] = new List<IBar>();
                    }

                    _symbolToBars[symbol].AddRange(newBars);
                }
                */
            }
        }

        public decimal? StockColumnValueAt(string symbol, string column, DateTime time)
        {
            if (!_symbolToBars.ContainsKey(symbol))
                return null;

            var bars = _symbolToBars[symbol];
            var filteredBars = bars.Where(b => b.TimeUtc <= time).ToList();

            if (!filteredBars.Any())
                return null;

            var closestTime = filteredBars.Max(b => b.TimeUtc);
            var resultBar = filteredBars.FirstOrDefault(b => b.TimeUtc == closestTime);

            return column.ToLower() switch
            {
                "open" => resultBar?.Open,
                "high" => resultBar?.High,
                "low" => resultBar?.Low,
                "close" => resultBar?.Close,
                "volume" => resultBar?.Volume,
                _ => null
            };
        }
    }
}
