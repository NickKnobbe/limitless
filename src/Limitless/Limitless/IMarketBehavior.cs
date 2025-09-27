using Alpaca.Markets;

namespace Limitless
{
    public interface IMarketBehavior
    {
        Task<IOrder?> Buy(string symbol, int quantity, DateTime timestamp, decimal estimatedPrice);
        Task<IOrder?> Sell(string symbol, int quantity, DateTime timestamp, decimal estimatedPrice);
        Task<IOrder?> GetOrder(IOrder order);
    }
}
