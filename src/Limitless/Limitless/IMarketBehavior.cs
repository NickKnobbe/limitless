using Alpaca.Markets;

namespace Limitless
{
    public interface IMarketBehavior
    {
        Task<IOrder?> Buy(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice);
        Task<IOrder?> Sell(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice);
        Task<IOrder?> GetOrder(IOrder order);
    }
}
