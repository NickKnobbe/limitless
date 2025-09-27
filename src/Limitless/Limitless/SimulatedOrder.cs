using Alpaca.Markets;

namespace Limitless
{
    internal class SimulatedOrder : IOrder
    {
        public Guid OrderId { get; set; } = Guid.NewGuid();

        public string? ClientOrderId { get; set; } = null;

        public DateTime? CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? FilledAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiredAtUtc { get; set; } = null;

        public DateTime? CancelledAtUtc { get; set; } = null;

        public DateTime? FailedAtUtc { get; set; } = null;

        public DateTime? ReplacedAtUtc { get; set; } = null;

        public Guid AssetId { get; set; } = Guid.NewGuid(); // Dummy ID

        public string Symbol { get; set; } = string.Empty;

        public AssetClass AssetClass { get; set; } = AssetClass.UsEquity;

        public decimal? Notional => Quantity.HasValue && FilledAveragePrice.HasValue
            ? Quantity.Value * FilledAveragePrice.Value
            : null;

        public decimal? Quantity { get; set; }

        public decimal FilledQuantity { get; set; }

        public long IntegerQuantity => Quantity.HasValue ? (long)Quantity.Value : 0;

        public long IntegerFilledQuantity => (long)FilledQuantity;

        public OrderType OrderType { get; set; } = OrderType.Market;

        public OrderClass OrderClass { get; set; } = OrderClass.Simple;

        public OrderSide OrderSide { get; set; }

        public TimeInForce TimeInForce { get; set; }

        public decimal? LimitPrice { get; set; } = null;

        public decimal? StopPrice { get; set; } = null;

        public decimal? TrailOffsetInDollars { get; set; } = null;

        public decimal? TrailOffsetInPercent { get; set; } = null;

        public decimal? HighWaterMark { get; set; } = null;

        public decimal? AverageFillPrice => FilledAveragePrice;

        public decimal? FilledAveragePrice { get; set; }

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Filled;

        public Guid? ReplacedByOrderId { get; set; } = null;

        public Guid? ReplacesOrderId { get; set; } = null;

        public IReadOnlyList<IOrder> Legs { get; set; } = new List<IOrder>();

        public SimulatedOrder() { }

        public SimulatedOrder(
            Guid id,
            string symbol,
            decimal quantity,
            decimal filledAvgPrice,
            OrderSide orderSide,
            OrderType orderType,
            TimeInForce timeInForce)
        {
            OrderId = id;
            Symbol = symbol;
            Quantity = quantity;
            FilledQuantity = quantity;
            FilledAveragePrice = filledAvgPrice;
            OrderSide = orderSide;
            TimeInForce = timeInForce;
        }
    }
}
