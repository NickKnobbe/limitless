using Alpaca.Markets;
using TradingApp;

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

        public override void OrderFilledConfirmation(TraderState traderState, dynamic updatedOrder)
        {
            if (updatedOrder == null) return;

            if (updatedOrder.OrderSide == OrderSide.Buy)
            {
                _quantityHeld = updatedOrder.Quantity;
                _allBoughtAmounts += updatedOrder.FilledAvgPrice * updatedOrder.Quantity;
                Console.WriteLine($"{_currentTime} Bought {_quantityHeld} {Symbol} @ {updatedOrder.FilledAvgPrice}");
            }
            else if (updatedOrder.OrderSide == OrderSide.Sell)
            {
                _allSoldAmounts += updatedOrder.FilledAvgPrice * updatedOrder.Quantity;
                Console.WriteLine($"{_currentTime} Sold {_quantityHeld} {Symbol} @ {updatedOrder.FilledAvgPrice}");
                _quantityHeld = 0;
                _estimatedHeldValue = 0;
            }
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
