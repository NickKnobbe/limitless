using Alpaca.Markets;

namespace Limitless
{
    internal class MarketBehaviorAlpaca : IMarketBehavior
    {
        private readonly Configuration launchSettings;
        private readonly IAlpacaTradingClient tradingClient;

        internal MarketBehaviorAlpaca(Configuration launchSettings, IAlpacaTradingClient tradingClient)
        {
            this.launchSettings = launchSettings;
            this.tradingClient = tradingClient;
        }

        public async Task<IOrder?> Buy(string symbol, int quantity, DateTime timestamp, decimal estimatedPrice)
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
                Console.WriteLine($"{timestamp} {symbol} Buy was cancelled. The total price exceeds the maximum buy price tolerance.");
                return null;
            }

            var marketOrderRequest = new NewOrderRequest(symbol, OrderQuantity.FromInt64(quantity), OrderSide.Buy, OrderType.Market, TimeInForce.Day);

            var marketOrder = await tradingClient.PostOrderAsync(marketOrderRequest);

            Console.WriteLine($"{timestamp} {symbol} Buy submitted for {quantity} shares at an estimated {totalPrice}.");

            return marketOrder;
        }

        public async Task<IOrder?> Sell(string symbol, int quantity, DateTime timestamp, decimal estimatedPrice)
        {
            var marketOrderRequest = new NewOrderRequest(symbol, OrderQuantity.FromInt64(quantity), OrderSide.Sell, OrderType.Market, TimeInForce.Day);

            decimal price = quantity * estimatedPrice;

            var marketOrder = await tradingClient.PostOrderAsync(marketOrderRequest);

            Console.WriteLine($"{timestamp} {symbol} Sell submitted for an estimated {price}.");

            return marketOrder;
        }

        public async Task<IOrder?> GetOrder(IOrder order)
        {
            var updatedOrder = await tradingClient.GetOrderAsync(order.OrderId);
            return updatedOrder;
        }

        internal async Task<object> GetPosition(string symbol)
        {
            var position = await tradingClient.GetPositionAsync(symbol);
            return position;
        }
    }

}
