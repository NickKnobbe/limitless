using Alpaca.Markets;

namespace Limitless
{
    public interface IMarketBehavior
    {
        Task<IOrder?> GetOrder(IOrder order);
        Task<IOrder?> Buy(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice);
        Task<IOrder?> Sell(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice);
        Task<IOrder?> TakeProfitStopLoss(string symbol, decimal quantity, DateTime timestamp, decimal estimatedPrice, decimal takeProfitLimitPrice, decimal stopLossPrice);
    }
}
