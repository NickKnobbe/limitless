using Alpaca.Markets;
using System.Diagnostics;
using System.Text;

namespace Limitless
{
    public class TradeController
    {
        private Secrets secrets;
        internal IEnvironment CurrentEnvironment = Environments.Paper;
        internal Configuration Config { get; private set; }
        internal IAlpacaTradingClient? TradingClient { get; private set; }
        internal IAlpacaDataClient? DataClient { get; private set; }
        internal IMarketBehavior? MarketBehavior { get; private set; }
        internal PriceAggregator PriceAggregate { get; private set; }
        internal DateTime PerceivedCurrentTime { get; private set; }
        internal DateTime PreviousActionTime { get; private set; }
        internal TimeSpan TimePerAction { get; private set; }
        internal DateTime EndRunTime { get; private set; }
        internal List<Trader> Traders { get; private set; }
        internal int ActionTicksSinceStart { get; private set; }
        internal int ActionTicksSinceSummary { get; private set; }
        internal DateTime TimeSincePreviousActionTick { get; set; }

        public TradeController(Configuration configuration, Secrets secret)
        {
            Config = configuration;
            secrets = secret;
            Traders = new List<Trader>();
            ActionTicksSinceStart = 0;
            ActionTicksSinceSummary = 0;
        }

        internal async Task Run()
        {
            if (!Config.SimulateLiveMarket)
            {
                CurrentEnvironment = Environments.Live;
            }

            var secretKey = new SecretKey(secrets.Key, secrets.Secret);

            TradingClient = CurrentEnvironment.GetAlpacaTradingClient(secretKey);
            DataClient = CurrentEnvironment.GetAlpacaDataClient(secretKey);
            PriceAggregate = new PriceAggregator(Config, DataClient);
            TimePerAction = new TimeSpan(0, 0, 0, 0, Config.ActionTickMs);

            if (Config.SimulateLiveMarket)
            {
                PerceivedCurrentTime = Config.BacktestTimeStart;
                Console.WriteLine("Retrieving bar history data...");
                await PriceAggregate.LoadStoreBars(Config.Symbols, Config.BacktestTimeStart, Config.BacktestTimeEnd, false);
                Console.WriteLine("Retrieving quote history data...");
                await PriceAggregate.LoadStoreQuotesFromBars(Config.Symbols, Config.BacktestTimeStart, Config.BacktestTimeEnd, false);
                MarketBehavior = new MarketBehaviorBacktest(Config);
            }
            else
            {
                Console.WriteLine("Retrieving bar history data...");
                PerceivedCurrentTime = DateTime.UtcNow;
                await PriceAggregate.LoadStoreBars(Config.Symbols, Config.PriceAggregatorTimeStart, PerceivedCurrentTime, false);
                Console.WriteLine("Retrieving quote history data...");
                await PriceAggregate.LoadStoreQuotesFromBars(Config.Symbols, Config.PriceAggregatorTimeStart, PerceivedCurrentTime, false);
                MarketBehavior = new MarketBehaviorAlpaca(Config, TradingClient);
            }

            //var testMarketBehavior = new MarketBehaviorAlpaca(Config, TradingClient);

            //var order = await testMarketBehavior.TakeProfitStopLoss("TSLA", 1.0M, DateTime.Now, 430.0M, 450.0M, 410.0M);

            Console.WriteLine("Data retrieval complete.");

            foreach (var symbol in Config.Symbols)
            {
                var trader = new SMACrossingTrader(this, Config, PriceAggregate, symbol, MarketBehavior);
                Traders.Add(trader);
                trader.SymbolOpeningQuote = PriceAggregate.GetDayOpeningQuote(symbol, DateOnly.FromDateTime(PerceivedCurrentTime));
                var mostRecentBar = PriceAggregate.GetLatestBarBefore(trader.Symbol, PerceivedCurrentTime);
                var mostRecentQuote = PriceAggregate.GetLatestQuoteBefore(trader.Symbol, PerceivedCurrentTime);
                trader.Activate(PerceivedCurrentTime, mostRecentQuote, mostRecentBar);
            }

            await ProcessLoop();
        }

        internal async Task ProcessLoop()
        {
            if (Config.SimulateLiveMarket)
            {
                EndRunTime = Config.BacktestTimeEnd;
            }
            else
            {
                EndRunTime = Config.RunTimeEnd;
            }

            var summarySb = new StringBuilder();

            while (PerceivedCurrentTime < EndRunTime)
            {
                if (Config.SimulateLiveMarket)
                {
                    PerceivedCurrentTime += new TimeSpan(0, 0, 0, 0, 250);
                }
                else
                {
                    PerceivedCurrentTime = DateTime.Now;
                }

                foreach (var trader in Traders)
                {
                    await trader.ProcessTick(PerceivedCurrentTime);
                }

                if (PerceivedCurrentTime - PreviousActionTime > TimePerAction)
                {
                    if (!Config.SimulateLiveMarket)
                    {
                        // todo : Determine if history request quotes are recent enough to give a live run data as it progresses
                        await PriceAggregate.LoadStoreQuotes(Config.Symbols, PreviousActionTime, PerceivedCurrentTime + new TimeSpan(0, 0, 5), true);
                    }

                    UpdateTraderQuotes();

                    PreviousActionTime = PerceivedCurrentTime;
                    foreach (var trader in Traders)
                    {
                        await trader.ActionTick(PerceivedCurrentTime);
                    }

                    ++ActionTicksSinceStart;
                    ++ActionTicksSinceSummary;

                    if (ActionTicksSinceSummary >= Config.TraderSummaryPrintInterval)
                    {
                        summarySb.AppendLine($"Summary at action {ActionTicksSinceStart}:");
                        foreach (var trader in Traders)
                        {
                            summarySb.AppendLine(trader.GetSummary());
                        }
                        Console.WriteLine(summarySb.ToString());
                        ActionTicksSinceSummary = 0;
                        summarySb.Clear();
                    }
                }
            }
        }

        internal void UpdateTraderQuotes()
        {
            for (int i = 0; i < Traders.Count; ++i)
            {
                var trader = Traders[i];
                var mostRecentBar = PriceAggregate.GetLatestBarBefore(trader.Symbol, PerceivedCurrentTime);
                var mostRecentQuote = PriceAggregate.GetLatestQuoteBefore(trader.Symbol, PerceivedCurrentTime);

                if (mostRecentBar != null)
                {
                    trader.UpdateMostRecentBar(mostRecentBar);
                }

                if (mostRecentQuote != null)
                {
                    trader.UpdateMostRecentQuote(mostRecentQuote);
                }
            }
        }
    }
}
