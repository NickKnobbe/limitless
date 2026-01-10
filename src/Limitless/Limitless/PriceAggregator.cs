using Alpaca.Markets;

namespace Limitless
{
    public class PriceAggregator
    {
        private readonly Configuration _launchSettings;
        private IAlpacaDataClient dataClient;

        private readonly Dictionary<string, List<IBar>> _symbolToBars = new();
        private readonly Dictionary<string, List<IQuote>> _symbolToQuotes = new();
        private readonly Dictionary<string, decimal> _symbolToBeginningOfDayPrice = new();

        public PriceAggregator(Configuration launchSettings, IAlpacaDataClient client)
        {
            _launchSettings = launchSettings;
            dataClient = client;
        }

        async Task<Dictionary<string, List<TData>>?> PaginateRequest<TData, TRequest>(TRequest request, 
            Func<TRequest, Task<IMultiPage<TData>>> requestFunction, Func<TRequest, string, Task<IMultiPage<TData>>> requestPaginatedFunction)
        {
            var allItems = new List<TData>();
            string? nextPageToken = null;

            var symbolToPages = new Dictionary<string, List<TData>>();
            do
            {
                IMultiPage<TData>? currentPage = null;

                if (nextPageToken == null)
                {
                    currentPage = await requestFunction(request);
                }
                else
                {
                    currentPage = await requestPaginatedFunction(request, nextPageToken);
                }

                // Add bars to result
                foreach (var keyVal in currentPage.Items)
                {
                    if (!symbolToPages.ContainsKey(keyVal.Key))
                    {
                        symbolToPages.Add(keyVal.Key, new List<TData>());
                    }

                    symbolToPages[keyVal.Key].AddRange(currentPage.Items[keyVal.Key]);
                }

                // Set next page token
                nextPageToken = currentPage.NextPageToken;

            } while (!string.IsNullOrEmpty(nextPageToken));

            return symbolToPages;
        }

        public async Task<Dictionary<string, List<IBar>>?> LoadBars(IEnumerable<string> symbols, DateTime start, DateTime end)
        {
            var request = new HistoricalBarsRequest(symbols, start, end, BarTimeFrame.Minute);

            var results = await PaginateRequest(
                request,
                async req => { return await dataClient.GetHistoricalBarsAsync(req); },
                async (req, token) => { return await dataClient.GetHistoricalBarsAsync(req.WithPageToken(token)); }
                );

            return results;
        }

        public async Task<Dictionary<string, List<IQuote>>?> LoadQuotes(IEnumerable<string> symbols, DateTime start, DateTime end)
        {
            var request = new HistoricalQuotesRequest(symbols, start, end);

            var results = await PaginateRequest(
                request,
                async req => { return await dataClient.GetHistoricalQuotesAsync(req); },
                async (req, token) => { return await dataClient.GetHistoricalQuotesAsync(req.WithPageToken(token)); }
                );

            return results;
        }

        /// <summary>
        /// Loads quotes by loading bars and converting them to quotes. This is likely inaccurate.
        /// </summary>
        public async Task<Dictionary<string, List<IQuote>>> LoadQuotesFromBars(IEnumerable<string> symbols, DateTime start, DateTime end)
        {
            var bars = await LoadBars(symbols, start, end);

            var fakeQuotesMap = new Dictionary<string, List<IQuote>>();

            foreach (var keyValue in bars)
            {
                for (int i = 0; i < keyValue.Value.Count; ++i)
                {
                    var bar = keyValue.Value[i];
                    var quote = FakeQuote.FromBar(bar) as IQuote;
                    if (!fakeQuotesMap.ContainsKey(quote.Symbol))
                    {
                        fakeQuotesMap.Add(quote.Symbol, new List<IQuote>());
                    }
                    fakeQuotesMap[quote.Symbol].Add(quote);
                }
            }

            return fakeQuotesMap;
        }

        public async Task LoadStoreBars(IEnumerable<string> symbols, DateTime start, DateTime end, bool appendOnly)
        {
            var startTime = start;

            if (appendOnly)
            {
                var latest = GetEarliestOfLatestBars(symbols, start);

                if (latest != null && latest.TimeUtc > startTime)
                {
                    startTime = latest.TimeUtc;
                }
            }

            var barsBySymbol = await LoadBars(symbols, startTime, end);

            foreach (var symbol in symbols)
            {
                if (_symbolToBars.TryGetValue(symbol, out var bars))
                {
                    _symbolToBars[symbol].AddRange(barsBySymbol[symbol]);
                }
                else
                {
                    _symbolToBars.Add(symbol, barsBySymbol[symbol]);
                }
            }
        }

        public async Task LoadStoreQuotes(IEnumerable<string> symbols, DateTime start, DateTime end, bool appendOnly)
        {
            var startTime = start;

            if (appendOnly)
            {
                var latest = GetEarliestOfLatestQuotes(symbols, start);

                if (latest != null && latest.TimestampUtc > startTime)
                {
                    startTime = latest.TimestampUtc;
                }
            }

            var quotesBySymbol = await LoadQuotes(symbols, startTime, end);

            foreach (var symbol in symbols)
            {
                if (_symbolToQuotes.TryGetValue(symbol, out var quotes))
                {
                    _symbolToQuotes[symbol].AddRange(quotesBySymbol[symbol]);
                }
                else
                {
                    _symbolToQuotes.Add(symbol, quotesBySymbol[symbol]);
                }
            }
        }

        /// <summary>
        /// Loads and stores quotes by loading bars and converting them to quotes. This is likely inaccurate.
        /// </summary>
        public async Task LoadStoreQuotesFromBars(IEnumerable<string> symbols, DateTime start, DateTime end, bool appendOnly)
        {
            var startTime = start;

            if (appendOnly)
            {
                var latest = GetEarliestOfLatestQuotes(symbols, start);

                if (latest != null && latest.TimestampUtc > startTime)
                {
                    startTime = latest.TimestampUtc;
                }
            }

            var quotesBySymbol = await LoadQuotesFromBars(symbols, startTime, end);

            foreach (var symbol in symbols)
            {
                if (_symbolToQuotes.TryGetValue(symbol, out var quotes))
                {
                    _symbolToQuotes[symbol].AddRange(quotesBySymbol[symbol]);
                }
                else
                {
                    _symbolToQuotes.Add(symbol, quotesBySymbol[symbol]);
                }
            }
        }

        private IBar? GetEarliestOfLatestBars(IEnumerable<string> symbols, DateTime time)
        {
            IBar? result = null;

            var leastLatestTime = time;

            foreach (var symbol in symbols)
            {
                var nextItem = GetLatestBar(symbol);
                if (nextItem.TimeUtc < leastLatestTime || result == null)
                {
                    leastLatestTime = nextItem.TimeUtc;
                    result = nextItem;
                }
            }

            return result;
        }

        private IQuote? GetEarliestOfLatestQuotes(IEnumerable<string> symbols, DateTime time)
        {
            IQuote? result = null;

            var leastLatestTime = time;

            foreach (var symbol in symbols)
            {
                var nextItem = GetLatestQuote(symbol);
                if (nextItem.TimestampUtc < leastLatestTime || result == null)
                {
                    leastLatestTime = nextItem.TimestampUtc;
                    result = nextItem;
                }
            }

            return result;
        }

        public IBar? GetLatestBar(string symbol)
        {
            _symbolToBars.TryGetValue(symbol, out var bars);

            if (bars == null || bars.Count < 1) { return null; }

            return bars[bars.Count - 1];
        }

        public IQuote? GetLatestQuote(string symbol)
        {
            _symbolToQuotes.TryGetValue(symbol, out var quotes);

            if (quotes == null || quotes.Count < 1) { return null; }

            return quotes[quotes.Count - 1];
        }

        public IBar? GetLatestBarBefore(string symbol, DateTime dateTime)
        {
            _symbolToBars.TryGetValue(symbol, out var bars);

            if (bars == null || bars.Count < 1) { return null; }

            for (int i = bars.Count - 1; i >= 0; --i)
            {
                if (bars[i].TimeUtc < dateTime)
                {
                    return bars[i];
                }
            }
            return bars[0];
        }

        public IQuote? GetLatestQuoteBefore(string symbol, DateTime dateTime)
        {
            _symbolToQuotes.TryGetValue(symbol, out var quotes);

            if (quotes == null || quotes.Count < 1) { return null; }

            for (int i = quotes.Count - 1; i >= 0; --i)
            {
                if (quotes[i].TimestampUtc < dateTime)
                {
                    return quotes[i];
                }
            }
            return quotes[0];
        }

        public IQuote? GetDayOpeningQuote(string symbol, DateOnly day)
        {
            var time = _launchSettings.RegularMarketHoursStart;
            var openingDateTime = new DateTime(day, time) + new TimeSpan(0, 0, 1);
            return GetLatestQuoteBefore(symbol, openingDateTime);
        }

        public IQuote? GetDayOpeningQuote(string symbol, DateTime dateTime)
        {
            return GetDayOpeningQuote(symbol, DateOnly.FromDateTime(dateTime));
        }

        public decimal GetSMA(string symbol, int smaDays, DateTime time)
        {
            var beginTime = time - new TimeSpan(smaDays, 0, 0, 0);
            decimal runningTotal = 0.0M;
            int runningTotalCount = 0;

            _symbolToBars.TryGetValue(symbol, out var bars);

            if (bars == null || bars.Count < 1) { return 0.0M; }

            IBar bar;
            for (int i = 0; i < bars.Count; ++i)
            {
                bar = bars[i];

                if (bar.TimeUtc > time)
                {
                    break;
                }

                if (bar.TimeUtc >= beginTime)
                {
                    runningTotal += bar.Close;
                    ++runningTotalCount;
                }
            }

            if (runningTotalCount < 1)
            {
                return 0.0M;
            }

            return runningTotal / runningTotalCount;
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
