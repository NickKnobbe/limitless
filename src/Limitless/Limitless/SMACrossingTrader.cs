using Alpaca.Markets;

namespace Limitless
{
    internal class SMACrossingTrader : Trader
    {
        // Enter when price breaks above X-day SMA, and watch for a bullish MACD crossover. 
        private const int SMA_DAYS = 10;

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

        bool BuyCondition()
        {
            if (_mostRecentQuote == null || State != TraderState.WaitingToBuy) { return false; }

            var sma = _priceAggregator.GetSMA(Symbol, SMA_DAYS, _currentTime);
            if (_mostRecentQuote != null && _previousQuote != null && BidAskMid(_previousQuote) < sma && BidAskMid(_mostRecentQuote) > sma)
            {
                return true;
            }

            return false;
        }

        bool SellCondition()
        {
            if (_mostRecentQuote == null || State != TraderState.Holding) { return false; }

            var estimatePrice = BidAskMid(_mostRecentQuote);
            if (estimatePrice < _activeStopLossPerSharePrice || estimatePrice > _activeTakeProfitPerSharePrice)
            {
                return true;
            }

            return false;
        }

        public override async Task ActionTick(DateTime currentTime)
        {
            switch (State)
            {
                case TraderState.None:
                    State = TraderState.WaitingToBuy;
                    break;
                case TraderState.WaitingToBuy:
                    {                  
                        if (BuyCondition())
                        {
                            var allowedQuantity = GetHighestQuantityBuyAllowed(Symbol, _launchSettings.MaximumPricePerBuy);
                            if (allowedQuantity > 0.01M)
                            {
                                var estimatedCost = GetEstimatedPrice(Symbol) * allowedQuantity;
                                var order = await _marketBehavior.Buy(Symbol, allowedQuantity, _currentTime, GetEstimatedPrice(Symbol));

                                if (order != null)
                                {
                                    _orderBeingConfirmed = order;
                                    State = TraderState.ConfirmingOrder;
                                    // todo : Perform the confirmation during the same action tick
                                }
                            }
                        }
                    }
                    break;
                case TraderState.Holding:
                    {
                        if (SellCondition())
                        {
                            var order = await _marketBehavior.Sell(Symbol, _quantityHeld, _currentTime, GetEstimatedPrice(Symbol));

                            if (order != null)
                            {
                                _orderBeingConfirmed = order;
                                State = TraderState.ConfirmingOrder;
                                // todo : Perform the confirmation during the same action tick
                            }
                        }
                    }
                    break;
                case TraderState.Cooldown:
                    {
                        CooldownCycleActions();
                    }
                    break;
                case TraderState.ConfirmingOrder:
                    {
                        if (_orderBeingConfirmed == null)
                        {
                            // Problem problem
                        }
                        else
                        {
                            var order = await _marketBehavior.GetOrder(_orderBeingConfirmed);
                            if (order != null && order.OrderStatus == OrderStatus.Filled)
                            {
                                await ConfirmOrder(State, order);
                            }
                        }
                    }
                    break;
                case TraderState.Closing:
                    break;
                case TraderState.Closed:
                    break;
                case TraderState.Retired:
                    break;
                case TraderState.Error:
                    break;
                case TraderState.WaitingForEvent:
                    break;
                default:
                    State = TraderState.WaitingToBuy;
                    break;
            }
        }
    }
}
