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
        WaitingForEvent = 9
    }

    public class Trader
    {
        protected TradeController _owner;
        protected Configuration _launchSettings;
        public string Symbol { get; set; }
        protected IMarketBehavior _marketBehavior;

        public TraderState State { get; protected set; } = TraderState.None;
        protected IBar? _previousBar;
        protected IBar? _mostRecentBar;
        protected IQuote? _previousQuote;
        protected IQuote? _mostRecentQuote;
        protected IQuote? _quoteOnPreviousTick;
        protected PriceAggregator _priceAggregator;

        protected decimal _quantityHeld = 0.0M;
        protected int _cooldownCountdown = 0;
        protected IOrder? _orderBeingConfirmed;
        protected decimal _estimatedHeldValue = 0.0M;
        protected decimal _previousBuySharePrice = 0.0M;
        protected decimal _activeStopLossPerSharePrice = 0.0M;
        protected decimal _activeTakeProfitPerSharePrice = 0.0M;
        protected decimal _estimatedPAndLDelta = 0.0M;
        protected decimal _allSoldAmounts = 0.0M;
        protected decimal _allBoughtAmounts = 0.0M;

        protected DateTime _currentTime;
        protected DateTime _previousActionTime;
        protected TimeSpan _timePerAction;

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

        public virtual void Activate()
        {
            _cooldownCountdown = _launchSettings.TraderCooldownTickDuration;
            if (State != TraderState.Retired)
            {
                State = TraderState.WaitingToBuy;
            }

            _previousBar = null;
            _mostRecentBar = null;
            _previousQuote = null;
            _mostRecentQuote = null;
            _quoteOnPreviousTick = null;
            _quantityHeld = 0;
            _orderBeingConfirmed = null;
            _estimatedHeldValue = 0.0M;
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
            return _allSoldAmounts - _allBoughtAmounts + _estimatedHeldValue;
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
                    sellOrder = await _marketBehavior.Sell(Symbol, _quantityHeld, _currentTime, _mostRecentQuote.BidPrice);
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
                    }

                    State = TraderState.Closed;
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
                _previousBuySharePrice = (decimal)updatedOrder.AverageFillPrice;
                _quantityHeld += updatedOrder.FilledQuantity;
                _estimatedHeldValue += priceBought;
                _allBoughtAmounts += priceBought;
                _activeStopLossPerSharePrice = priceBought * _launchSettings.StopLossProportion;
                _activeTakeProfitPerSharePrice = priceBought * _launchSettings.TakeProfitProportion;
                Console.WriteLine($"{_currentTime} Bought {Symbol} @ {updatedOrder.AverageFillPrice} * {updatedOrder.FilledQuantity} = {priceBought}, P&L {CalculatePAndL()}");
                State = TraderState.Holding;
            }
            else if (updatedOrder.OrderSide == OrderSide.Sell)
            {
                var priceSold = (decimal)updatedOrder.AverageFillPrice * updatedOrder.FilledQuantity;
                _allSoldAmounts += (decimal)(updatedOrder.AverageFillPrice * updatedOrder.FilledQuantity);
                _quantityHeld = 0;
                _estimatedHeldValue = 0.0M;
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
            return _mostRecentQuote.AskPrice;
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

        public string GetSummary()
        {
            return $"{_currentTime} Trader for {Symbol} is holding {_quantityHeld} shares worth an estimated {_estimatedHeldValue}. Trader P&L: {CalculatePAndL()}";
        }
    }
}
