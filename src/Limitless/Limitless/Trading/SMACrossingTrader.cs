using Alpaca.Markets;

namespace Limitless.Trading
{
    internal class SMACrossingTrader : Trader
    {
        // Enter when price breaks above X-day SMA, and watch for a bullish MACD crossover. 
        private const int SMA_DAYS = 20;

        public SMACrossingTrader(
            TradeController owner,
            Configuration launchSettings,
            PriceAggregator priceAggregator,
            string symbol,
            IMarketBehavior marketBehavior)
            : base(owner, launchSettings, priceAggregator, symbol, marketBehavior)
        {
        }

        public override async Task ProcessTick(DateTime currentTime)
        {
            _currentTime = currentTime;
        }

        protected override bool BuyCondition()
        {
            // Buy when the current price crosses above the X-day SMA
            // and the stock's price is greater than its opening price.
            if (_mostRecentQuote == null || State != TraderState.WaitingToBuy) { return false; }

            var openingQuote = _priceAggregator.GetDayOpeningQuote(Symbol, _currentTime);
            var recentBAM = BidAskMid(_mostRecentQuote);

            if (openingQuote == null || recentBAM < BidAskMid(openingQuote)) { return false; }

            var sma = _priceAggregator.GetSMA(Symbol, SMA_DAYS, _currentTime);
            if (_mostRecentQuote != null && _previousQuote != null && BidAskMid(_previousQuote) < sma && BidAskMid(_mostRecentQuote) > sma)
            {
                return true;
            }

            return false;
        }

        protected override bool SellCondition()
        {
            if (_mostRecentQuote == null || State != TraderState.Holding) { return false; }

            var estimatePrice = GetEstimatedPrice(Symbol);
            if (estimatePrice < _activeStopLossPerSharePrice || estimatePrice > _activeTakeProfitPerSharePrice)
            {
                return true;
            }

            return false;
        }
    }
}
