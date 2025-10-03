using Alpaca.Markets;

namespace Limitless
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

        bool BuyCondition()
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

        bool SellCondition()
        {
            if (_mostRecentQuote == null || State != TraderState.Holding) { return false; }

            var estimatePrice = GetEstimatedPrice(Symbol);
            if (estimatePrice < _activeStopLossPerSharePrice || estimatePrice > _activeTakeProfitPerSharePrice)
            {
                return true;
            }

            return false;
        }

        public override async Task ActionTick(DateTime currentTime)
        {
            if (GoDormantCondition() && State != TraderState.Dormant && State != TraderState.Closing && State != TraderState.Closed)
            {
                await GoDormant();
                return;
            }

            switch (State)
            {
                case TraderState.None:
                    State = TraderState.Dormant;
                    break;
                case TraderState.WaitingToBuy:
                    {                  
                        if (BuyCondition())
                        {
                            var allowedQuantity = GetHighestQuantityBuyAllowed(Symbol, _launchSettings.MaximumPricePerBuy);
                            if (allowedQuantity > 0.01M)
                            {
                                var estimatedPrice = GetEstimatedPrice(Symbol);
                                var estimatedCost = estimatedPrice * allowedQuantity;

                                var order = await Buy(Symbol, allowedQuantity, _currentTime, GetEstimatedPrice(Symbol));

                                if (order != null)
                                {
                                    _orderBeingConfirmed = order;
                                    State = TraderState.ConfirmingOrder;
                                    // todo : Perform the confirmation during the same action tick
                                }
                            }
                        }
                        break;
                    }
                case TraderState.Holding:
                    {
                        if (SellCondition())
                        {
                            var order = await Sell(Symbol, _quantityHeld, _currentTime, GetEstimatedPrice(Symbol));

                            if (order != null)
                            {
                                _orderBeingConfirmed = order;
                                State = TraderState.ConfirmingOrder;
                                // todo : Perform the confirmation during the same action tick
                            }
                        }
                        break;
                    }
                case TraderState.Cooldown:
                    {
                        CooldownCycleActions();
                        break;
                    }
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
                        break;
                    }
                case TraderState.Closing:
                    {
                        if (_orderBeingConfirmed == null || _activeAttemptsToFullyClose >= _launchSettings.TraderAttemptsToFullyClose)
                        {
                            State = TraderState.Closed;
                            _activeAttemptsToFullyClose = 0;
                        }
                        else                         
                        {
                            var order = await _marketBehavior.GetOrder(_orderBeingConfirmed);
                            if (order != null && order.OrderStatus == OrderStatus.Filled)
                            {
                                await ConfirmOrder(State, order);
                            }
                            ++_activeAttemptsToFullyClose;
                        }
                    }
                    break;
                case TraderState.Closed:
                    {
                        if (WakeUpFromDormantCondition())
                        {
                            await WakeUpFromDormant();
                        }
                        break;
                    }
                case TraderState.Retired:
                    break;
                case TraderState.Error:
                    break;
                case TraderState.WaitingForEvent:
                    break;
                case TraderState.Dormant:
                    {
                        if (WakeUpFromDormantCondition())
                        {
                            await WakeUpFromDormant();
                        }
                        break;
                    }
                default:
                    State = TraderState.Dormant;
                    break;
            }
        }
    }
}
