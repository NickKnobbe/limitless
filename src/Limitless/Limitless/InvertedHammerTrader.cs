using Alpaca.Markets;

namespace Limitless
{
    public class InvertedHammerTrader : Trader
    {
        private const decimal BodyToRangeMaxRatio = 0.25M;
        private const decimal MinUpperShadowPercent = 0.6M;
        private const decimal MaxLowerShadowPercent = 0.1M;
        private const decimal MinBarRange = 0.5M;

        public InvertedHammerTrader(
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
        }

        public override async Task ActionTick(DateTime currentTime)
        {
        }



        private bool IsBullishInvertedHammer(IBar bar)
        {
            decimal range = bar.High - bar.Low;
            if (range < MinBarRange)
                return false;

            decimal body = Math.Abs(bar.Close - bar.Open);
            decimal upperShadow = bar.High - Math.Max(bar.Open, bar.Close);
            decimal lowerShadow = Math.Min(bar.Open, bar.Close) - bar.Low;

            bool smallBody = body / range <= BodyToRangeMaxRatio;
            bool longUpperShadow = upperShadow / range >= MinUpperShadowPercent;
            bool smallLowerShadow = lowerShadow / range <= MaxLowerShadowPercent;

            return smallBody && longUpperShadow && smallLowerShadow;
        }
    }
}
