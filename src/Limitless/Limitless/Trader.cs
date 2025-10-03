using Alpaca.Markets;

namespace Limitless 
{
    public enum TraderState
    {
        None = 0,
        WaitingToBuy = 1,
        Holding = 2,
        Cooldown = 3,
        ConfirmingOrder = 4,
        Closing = 5,
        Closed = 6,
        Retired = 7,
        Error = 8,
        WaitingForEvent = 9,
        Dormant = 10
    }

    public class Trader
    {
        protected TradeController _owner;
        protected Configuration _launchSettings;
        protected IMarketBehavior _marketBehavior;

        protected IBar? _previousBar;
        protected IBar? _mostRecentBar;
        protected IQuote? _previousQuote;
        protected IQuote? _mostRecentQuote;
        protected IQuote? _quoteOnPreviousTick;
        protected PriceAggregator _priceAggregator;

        protected decimal _quantityHeld = 0.0M;
        protected int _cooldownCountdown = 0;
        protected IOrder? _orderBeingConfirmed;
        protected decimal _averageHeldSharePriceEstimate = 0.0M;
        protected decimal _activeTakeProfitPerSharePrice = decimal.MaxValue;
        protected decimal _activeStopLossPerSharePrice = decimal.MinValue;
        protected decimal _estimatedPAndLDelta = 0.0M;
        protected decimal _allSoldAmounts = 0.0M;
        protected decimal _allBoughtAmounts = 0.0M;

        protected DateTime _currentTime;
        protected DateTime _previousActionTime;
        protected TimeSpan _timePerAction;

        protected int _activeAttemptsToFullyClose = 0;

        public string Symbol { get; set; }
        public IQuote? SymbolOpeningQuote { get; set; }
        public TraderState State { get; protected set; } = TraderState.None;

        public Trader(
            TradeController owner,
            Configuration launchSettings,
            PriceAggregator priceAggregator,
            string symbol,
            IMarketBehavior marketBehavior)
        {
            _owner = owner;
            _launchSettings = launchSettings;
            _timePerAction = new TimeSpan(0, 0, 0, 0, _launchSettings.ActionTickMs);
            Symbol = symbol;
            _marketBehavior = marketBehavior;
            _priceAggregator = priceAggregator;
        }

        public virtual void Activate(DateTime percievedCurrentTime, IQuote? mostRecentQuote, IBar? mostRecentBar)
        {
            _cooldownCountdown = _launchSettings.TraderCooldownTickDuration;
            _currentTime = percievedCurrentTime;
            _previousBar = null;
            _mostRecentBar = mostRecentBar;
            _previousQuote = null;
            _mostRecentQuote = mostRecentQuote;
            _quoteOnPreviousTick = mostRecentQuote;
            _quantityHeld = 0;
            _orderBeingConfirmed = null;
            _averageHeldSharePriceEstimate = 0.0M;

            // todo : Retrieve the stock position

            if (State != TraderState.Retired)
            {
                State = TraderState.WaitingToBuy;
            }

        }

        public virtual async Task ProcessTick(DateTime currentTime)
        {
            _currentTime = currentTime;
        }

        public virtual async Task ActionTick(DateTime currentTime)
        {
            _currentTime = currentTime;
        }

        public virtual void PostSellActions()
        {
            State = TraderState.Cooldown;
            _cooldownCountdown = _launchSettings.TraderCooldownTickDuration;
        }

        protected virtual void CooldownCycleActions()
        {
            --_cooldownCountdown;
            if (_cooldownCountdown == 0)
            {
                State = TraderState.WaitingToBuy;
            }
        }

        public virtual decimal CalculatePAndL()
        {
            return _allSoldAmounts - _allBoughtAmounts + _averageHeldSharePriceEstimate * _quantityHeld;
        }

        protected virtual bool GoDormantCondition()
        {
            if (TimeOnly.FromDateTime(_currentTime) > _launchSettings.DailyActiveTimeEnd)
            {
                return true;
            }
            return false;
        }

        protected virtual bool WakeUpFromDormantCondition()
        {
            if (TimeOnly.FromDateTime(_currentTime) > _launchSettings.DailyActiveTimeStart)
            {
                return true;
            }
            return false;
        }

        protected virtual async Task GoDormant()
        {
            await AttemptClose();
            if (_quantityHeld == 0)
            {
                State = TraderState.Dormant;
            }
        }

        protected virtual async Task WakeUpFromDormant()
        {
            if (_quantityHeld > 0)
            {
                State = TraderState.Holding;
            }
            else
            {
                State = TraderState.WaitingToBuy;
            }
            _activeAttemptsToFullyClose = 0;
        }

        public virtual void UpdateMostRecentQuote(IQuote quote)
        {
            if (quote == null)
            {
                Console.WriteLine("Mishandled quote data!");
                return;
            }

            _previousQuote = _mostRecentQuote;
            _mostRecentQuote = quote;
            if (_quantityHeld > 0)
            {
                _averageHeldSharePriceEstimate = GetEstimatedPrice(Symbol);
            }
        }

        public virtual void UpdateMostRecentBar(IBar bar)
        {
            if (bar == null)
            {
                Console.WriteLine("Mishandled bar data!");
                return;
            }

            _previousBar = _mostRecentBar;
            _mostRecentBar = bar;
        }

        public virtual decimal BidAskMid(IQuote quote)
        {
            if (quote == null) { return 0.0M; }

            return (quote.AskPrice + quote.BidPrice) / 2.0M;
        }

        public virtual void Retire(string msg)
        {
            State = TraderState.Retired;
            Console.WriteLine($"{_currentTime} Retiring trader for {Symbol}. {msg}");
        }

        public virtual async Task AttemptClose()
        {
            if (_quantityHeld > 0)
            {
                IOrder? sellOrder = null;
                if (_mostRecentQuote != null)
                {
                    sellOrder = await Sell(Symbol, _quantityHeld, _currentTime, _mostRecentQuote.BidPrice);
                }

                if (sellOrder == null)
                {
                    Console.WriteLine($"{_currentTime} Failed to close sell of {_quantityHeld} shares of {Symbol}.");
                }
                else
                {
                    State = TraderState.Closing;
                    _orderBeingConfirmed = sellOrder;

                    var updatedOrder = await _marketBehavior.GetOrder(_orderBeingConfirmed);
                    if (updatedOrder != null && updatedOrder.OrderStatus == OrderStatus.Filled)
                    {
                        OrderFilledConfirmation(State, updatedOrder);
                        _orderBeingConfirmed = null;
                        State = TraderState.Closed;
                        _activeAttemptsToFullyClose = 0;
                    }
                    else
                    {
                        ++_activeAttemptsToFullyClose;
                    }
                }
            }
        }

        public virtual async Task ConfirmOrder(TraderState traderState, IOrder order)
        {
            var updatedOrder = await _marketBehavior.GetOrder(order);
            if (updatedOrder != null && updatedOrder.OrderStatus == OrderStatus.Filled)
            {
                OrderFilledConfirmation(State, updatedOrder);
            }
        }

        public virtual void OrderFilledConfirmation(TraderState traderState, IOrder updatedOrder)
        {
            if (updatedOrder?.AverageFillPrice == null) return;

            if (updatedOrder.OrderSide == OrderSide.Buy)
            {
                var priceBought = (decimal)updatedOrder.AverageFillPrice * updatedOrder.FilledQuantity;
                decimal previousAverageSharePrice = _averageHeldSharePriceEstimate;
                decimal previousQuantityHeld = _quantityHeld;
                _quantityHeld += updatedOrder.FilledQuantity;

                var previousPortionQuantityHeld = 0.0M;
                if (_quantityHeld != 0)
                {
                    previousPortionQuantityHeld = previousQuantityHeld / _quantityHeld;
                }
                var incomingPortionQuantityHeld = 1.0M;
                if (_quantityHeld != 0)
                {
                    incomingPortionQuantityHeld -= previousPortionQuantityHeld;
                }

                // Weighted average
                _averageHeldSharePriceEstimate = previousAverageSharePrice * previousPortionQuantityHeld + (decimal)updatedOrder.AverageFillPrice * incomingPortionQuantityHeld;

                _allBoughtAmounts += priceBought;
                _activeStopLossPerSharePrice = (decimal)updatedOrder.AverageFillPrice * _launchSettings.StopLossProportion;
                _activeTakeProfitPerSharePrice = (decimal)updatedOrder.AverageFillPrice * _launchSettings.TakeProfitProportion;
                Console.WriteLine($"{_currentTime} Bought {Symbol} @ {updatedOrder.AverageFillPrice} * {updatedOrder.FilledQuantity} = {priceBought}, P&L {CalculatePAndL()}");
                State = TraderState.Holding;
            }
            else if (updatedOrder.OrderSide == OrderSide.Sell)
            {
                var priceSold = (decimal)updatedOrder.AverageFillPrice * updatedOrder.FilledQuantity;
                _allSoldAmounts += (decimal)(updatedOrder.AverageFillPrice * updatedOrder.FilledQuantity);
                _quantityHeld = 0;
                _averageHeldSharePriceEstimate = 0.0M;
                Console.WriteLine($"{_currentTime} Sold {Symbol} @ {updatedOrder.AverageFillPrice} * {updatedOrder.FilledQuantity} = {priceSold}, P&L {CalculatePAndL()}");
                PostSellActions();
            }
        }

        public virtual decimal GetEstimatedPrice(string symbol)
        {
            if (_mostRecentQuote == null)
            {
                if (_mostRecentBar == null)
                {
                    return 0.0M;
                }
                return _mostRecentBar.Close;
            }
            return BidAskMid(_mostRecentQuote);
        }

        public virtual decimal GetHighestQuantityBuyAllowed(string symbol, decimal buyLimit)
        {
            if (_mostRecentQuote == null)
            {
                return 0.0M;
            }

            var qty = buyLimit / _mostRecentQuote.AskPrice;

            return Math.Floor(qty);
        }

        protected virtual async Task<IOrder?> Buy(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice)
        {
            return await _marketBehavior.Buy(symbol, quantity, timestamp, estimatedPrice);
        }

        protected virtual async Task<IOrder?> Sell(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice)
        {
            return await _marketBehavior.Sell(symbol, quantity, timestamp, estimatedPrice);
        }

        protected virtual async Task<IOrder?> TakeProfitStopLoss(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice, decimal takeProfitLimitPrice, decimal stopLossPrice)
        {
            return await _marketBehavior.TakeProfitStopLoss(symbol, quantity, timestamp, estimatedPrice, takeProfitLimitPrice, stopLossPrice);
        }

        public virtual async Task OnTakeProfitTriggered()
        {

        }

        public virtual async Task OnStopLossTriggered()
        {

        }

        public string GetSummary()
        {
            var stateStr = State.ToString();
            return $"{_currentTime} Trader for {Symbol} is holding {_quantityHeld} shares worth an estimated {_averageHeldSharePriceEstimate} * {_quantityHeld} = {_averageHeldSharePriceEstimate * _quantityHeld}. {Symbol} P&L: {CalculatePAndL()} {stateStr}";
        }
    }
}
