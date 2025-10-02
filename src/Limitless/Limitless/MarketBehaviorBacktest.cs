using Alpaca.Markets;

namespace Limitless
{
    public class MarketBehaviorBacktest : IMarketBehavior
    {
        private Configuration launchSettings;
        private Dictionary<Guid, IOrder> orderIdToOrder;

        public MarketBehaviorBacktest(Configuration launchSettings)
        {
            this.launchSettings = launchSettings;
            this.orderIdToOrder = new Dictionary<Guid, IOrder>();
        }

        public async Task<IOrder?> Buy(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice)
        {
            if (estimatedPrice <= 0.0M)
            {
                Console.WriteLine($"{timestamp} {symbol} Buy was cancelled. The most recent bid price was <= 0.0.");
                return null;
            }

            if (estimatedPrice > launchSettings.MaximumSharePrice)
            {
                Console.WriteLine($"{timestamp} {symbol} Buy was cancelled. The most recent ask price {estimatedPrice} exceeds the maximum share price tolerance of {launchSettings.MaximumSharePrice}.");
                return null;
            }

            decimal amountAffordable = launchSettings.MaximumPricePerBuy / estimatedPrice;
            quantity = (int)Math.Floor(amountAffordable);

            decimal totalPrice = quantity * estimatedPrice;

            if (quantity < 1)
            {
                Console.WriteLine($"{timestamp} {symbol} Buy was cancelled. Can't buy with a quantity of {quantity}.");
                return null;
            }

            if (totalPrice > launchSettings.MaximumPricePerBuy)
            {
                Console.WriteLine($"{timestamp} {symbol} Buy was cancelled. The most recent ask price {estimatedPrice} times the quantity {quantity} exceeds the maximum buy price tolerance of {launchSettings.MaximumPricePerBuy}.");
                return null;
            }

            var marketOrderRequest = new NewOrderRequest(symbol, OrderQuantity.FromInt64((long)Math.Round(quantity)), OrderSide.Buy, OrderType.Market, TimeInForce.Day);

            var marketOrder = new SimulatedOrder(
                id: Guid.NewGuid(),
                symbol: symbol,
                quantity: quantity,
                filledAvgPrice: estimatedPrice,
                orderSide: OrderSide.Buy,
                orderType: OrderType.Market,
                timeInForce: TimeInForce.Day
            );

            orderIdToOrder[marketOrder.OrderId] = marketOrder;

           // Console.WriteLine($"{timestamp} {symbol} Buy submitted for {quantity} shares at an estimated {quantity} * {estimatedPrice} = {totalPrice}.");

            return marketOrder;
        }

        public async Task<IOrder?> Sell(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice)
        {
            decimal price = quantity * estimatedPrice;

            var marketOrderRequest = new NewOrderRequest(symbol, OrderQuantity.FromInt64((long)Math.Round(quantity)), OrderSide.Sell, OrderType.Market, TimeInForce.Day);

            var marketOrder = new SimulatedOrder(
                id: Guid.NewGuid(),
                symbol: symbol,
                quantity: quantity,
                filledAvgPrice: estimatedPrice,
                orderSide: OrderSide.Sell,
                orderType: OrderType.Market,
                timeInForce: TimeInForce.Day
            );

            orderIdToOrder[marketOrder.OrderId] = marketOrder;

            //Console.WriteLine($"{timestamp} {symbol} Sell submitted for an estimated {quantity} * {estimatedPrice} = {price}.");

            return marketOrder;
        }

        public async Task<IOrder?> GetOrder(IOrder? order)
        {
            if (order == null) { return null; }

            IOrder? storedOrder;
            if (orderIdToOrder.TryGetValue(order.OrderId, out storedOrder))
            {
                if (storedOrder == null) { return null; }

                var storedOrderConverted = storedOrder as SimulatedOrder;

                if (storedOrderConverted == null) { return null; }

                storedOrderConverted.OrderStatus = OrderStatus.Filled;
                return (IOrder?)storedOrderConverted;
            }

            return null;
        }
    }
}
