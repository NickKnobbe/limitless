using Alpaca.Markets;

namespace Limitless
{
    internal class TradeController
    {
        private Secrets secrets;
        internal IEnvironment CurrentEnvironment = Environments.Paper;
        internal Configuration Config { get; private set; }
        internal IAlpacaTradingClient? TradingClient { get; private set; }
        internal IAlpacaDataClient? DataClient { get; private set; }
        internal IMarketBehavior? MarketBehavior { get; private set; }
        internal PriceAggregator PriceAggregate { get; private set; }

        public TradeController(Configuration configuration, Secrets secret)
        {
            Config = configuration;
            secrets = secret;
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
            MarketBehavior = new MarketBehaviorAlpaca(Config, TradingClient);
            PriceAggregate = new PriceAggregator(Config, DataClient);

            await PriceAggregate.LoadBarsAsync(Config.Symbols, Config.BacktestTimeStart, Config.BacktestTimeEnd);

            // Just testing a buy and sell for now

            var buyOrder = await MarketBehavior.Buy("TSLA", 1, DateTime.Now, 800.0M);

            await Task.Delay(3000);

            var filledOrder = await MarketBehavior.GetOrder(buyOrder);

            var sellOrder = await MarketBehavior.Sell("TSLA", 1, DateTime.Now, (decimal)filledOrder.AverageFillPrice);
        }
    }
}
