using Alpaca.Markets;

namespace Limitless
{
    internal class TradeController
    {
        private Secrets secrets;
        internal Configuration Config { get; private set; }
        internal IAlpacaTradingClient? TradingClient { get; private set; }

        public TradeController(Configuration configuration, Secrets secret)
        {
            Config = configuration;
            secrets = secret;
        }

        internal async Task Run()
        {
            if (Config.SimulateLiveMarket)
            {
                TradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(secrets.Key, secrets.Secret));
            }
            else
            {
                TradingClient = Environments.Live.GetAlpacaTradingClient(new SecretKey(secrets.Key, secrets.Secret));
            }

            // Just testing a buy and sell for now

            IMarketBehavior marketBehavior = new MarketBehaviorAlpaca(Config, TradingClient);

            var buyOrder = await marketBehavior.Buy("TSLA", 1, DateTime.Now, 800.0M);

            await Task.Delay(3000);

            var filledOrder = await marketBehavior.GetOrder(buyOrder);

            var sellOrder = await marketBehavior.Sell("TSLA", 1, DateTime.Now, (decimal)filledOrder.AverageFillPrice);
        }
    }
}
