using Alpaca.Markets;
using Alpaca.Markets.Extensions;
using System.Collections.Generic;

namespace Limitless
{
    public class PriceAggregator
    {
        private readonly Configuration _launchSettings;
        private IAlpacaDataClient dataClient;

        private readonly Dictionary<string, List<IBar>> _symbolToBars = new();
        private readonly Dictionary<string, decimal> _symbolToBeginningOfDayPrice = new();

        public PriceAggregator(Configuration launchSettings, IAlpacaDataClient client)
        {
            _launchSettings = launchSettings;
            dataClient = client;
        }

        public async Task<Dictionary<string, List<IBar>>> GetAllHistoricalBarsAsync(IEnumerable<string> symbols, DateTime start, DateTime end)
        {
            var request = new HistoricalBarsRequest(symbols, start, end, BarTimeFrame.Minute);

            var allBars = new List<IBar>();
            string? nextPageToken = null;

            var symbolToPages = new Dictionary<string, List<IBar>>();

            do
            {
                IMultiPage<IBar>? currentPage = null;

                if (nextPageToken == null)
                {
                    currentPage = await dataClient.GetHistoricalBarsAsync(request);
                }
                else
                {
                    currentPage = await dataClient.GetHistoricalBarsAsync(request.WithPageToken(nextPageToken));
                }

                // Add bars to result
                foreach (var keyVal in currentPage.Items)
                {
                    if (!symbolToPages.ContainsKey(keyVal.Key))
                    {
                        symbolToPages.Add(keyVal.Key, new List<IBar>());
                    }

                    symbolToPages[keyVal.Key].AddRange(currentPage.Items[keyVal.Key]);
                }

                // Set next page token
                nextPageToken = currentPage.NextPageToken;

            } while (!string.IsNullOrEmpty(nextPageToken));

            return symbolToPages;
        }

        public async Task LoadBarsAsync(IEnumerable<string> symbols, DateTime start, DateTime end)
        {
            var barsBySymbol = await GetAllHistoricalBarsAsync(symbols, start, end);

            foreach (var symbol in symbols)
            {
                if (barsBySymbol.TryGetValue(symbol, out var bars))
                {
                    _symbolToBars[symbol] = new List<IBar>(bars);
                }
            }
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

            var barsBySymbol = await GetAllHistoricalBarsAsync(symbols, mostOutOfDateTime, end);

            foreach (var symbol in symbols)
            {
                if (barsBySymbol.TryGetValue(symbol, out var newBars))
                {
                    if (!_symbolToBars.ContainsKey(symbol))
                    {
                        _symbolToBars[symbol] = new List<IBar>();
                    }

                    _symbolToBars[symbol].AddRange(newBars);
                }
            }
        }

        public IBar? GetLatestBar(string symbol, DateTime time)
        {
            _symbolToBars.TryGetValue(symbol, out var bars);

            if (bars == null || bars.Count < 1) { return null; }

            return bars[bars.Count - 1];
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
