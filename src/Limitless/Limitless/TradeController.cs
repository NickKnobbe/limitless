using Alpaca.Markets;
using TradingApp;

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

        public TradeController(Configuration configuration, Secrets secret)
        {
            Config = configuration;
            secrets = secret;
            Traders = new List<Trader>();
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
                await PriceAggregate.LoadBarsAsync(Config.Symbols, Config.BacktestTimeStart, Config.BacktestTimeEnd);
                MarketBehavior = new MarketBehaviorBacktest(Config);
            }
            else
            {
                PerceivedCurrentTime = DateTime.UtcNow;
                await PriceAggregate.LoadBarsAsync(Config.Symbols, Config.PriceAggregatorTimeStart, PerceivedCurrentTime);
                MarketBehavior = new MarketBehaviorAlpaca(Config, TradingClient);
            }

            foreach (var symbol in Config.Symbols)
            {
                Traders.Add(new Trader(this, Config, PriceAggregate, symbol, MarketBehavior));
            }

            await ProcessLoop();

            // Just testing a buy and sell for now

            //var buyOrder = await MarketBehavior.Buy("TSLA", 1, DateTime.Now, 800.0M);

            //var filledOrder = await MarketBehavior.GetOrder(buyOrder);

            //var sellOrder = await MarketBehavior.Sell("TSLA", 1, DateTime.Now, (decimal)filledOrder.AverageFillPrice);
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

            while (PerceivedCurrentTime < EndRunTime)
            {
                if (Config.SimulateLiveMarket)
                {
                    PerceivedCurrentTime = PerceivedCurrentTime + new TimeSpan(0, 0, 0, 0, 250);
                }
                else
                {
                    PerceivedCurrentTime = DateTime.Now;
                }

                for (int i = 0; i < Traders.Count; ++i)
                {
                    var trader = Traders[i];
                    var _mostRecentBar = PriceAggregate.GetLatestBar(trader.Symbol, PerceivedCurrentTime);
                    trader.UpdateMostRecentBar(_mostRecentBar);

                    await trader.ProcessTick(PerceivedCurrentTime);
                }

                if (PerceivedCurrentTime - PreviousActionTime > TimePerAction)
                {
                    PreviousActionTime = PerceivedCurrentTime;
                    for (int i = 0; i < Traders.Count; ++i)
                    {
                        var trader = Traders[i];
                        await trader.ActionTick(PerceivedCurrentTime);
                    }
                }
            }
        }
    }
}
