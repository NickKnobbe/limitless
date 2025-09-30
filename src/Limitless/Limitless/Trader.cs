using Alpaca.Markets;
using Limitless;

namespace TradingApp
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
        Error = 8
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

        protected int _quantityHeld = 0;
        protected int _cooldownCountdown = 0;
        protected IOrder? _orderBeingConfirmed;
        protected decimal _estimatedHeldValue = 0.0M;
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

        }

        public virtual async Task ActionTick(DateTime currentTime)
        {
            
        }

        public virtual void Cooldown()
        {
            State = TraderState.Cooldown;
            _cooldownCountdown = _launchSettings.TraderCooldownTickDuration;
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

            _previousBar = bar;
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

        public virtual void OrderFilledConfirmation(TraderState traderState, dynamic updatedOrder)
        {
            // Implement in derived classes
        }
    }
}
