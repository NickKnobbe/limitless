namespace Limitless
{
    public class Configuration
    {
        public int CustomTickMs { get; set; }
        public int SustainRequiredAfterTurn { get; set; }
        public decimal StopLossProportionBelow { get; set; }
        public decimal TakeProfitProportionAbove { get; set; }
        public decimal TrailingStopProportionBelow { get; set; }
        public decimal MaximumPricePerBuy { get; set; }
        public decimal MaximumSharePrice { get; set; }
        public decimal MaximumBuyDuringRun { get; set; }
        public decimal MinimumTimeToRevisit { get; set; }
        public int MaximumSecondsToRun { get; set; }
        public int TraderCooldownTickDuration { get; set; }
        public bool SimulateLiveMarket { get; set; }
        public string ApiBaseUrl { get; set; } = string.Empty;
        public TimeOnly RegularMarketHoursStart { get; set; }
        public TimeOnly RegularMarketHoursEnd { get; set; }
        public TimeOnly DailyActiveTimeStart { get; set; }
        public TimeOnly DailyActiveTimeEnd { get; set; }
        public DateTime BacktestTimeStart { get; set; }
        public DateTime BacktestTimeEnd { get; set; }
        public DateTime PriceAggregatorTimeStart { get; set; }
        public DateTime PriceAggregatorTimeEnd { get; set; }
        public int PriceAggregatorHistoryDays { get; set; }
    }
}
