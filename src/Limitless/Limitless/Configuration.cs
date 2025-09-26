namespace Limitless
{
    internal class Configuration
    {
        public int CustomTickMs { get; set; }
        public int SustainRequiredAfterTurn { get; set; }
        public double StopLossProportionBelow { get; set; }
        public double TakeProfitProportionAbove { get; set; }
        public double TrailingStopProportionBelow { get; set; }
        public double MaximumPricePerBuy { get; set; }
        public double MaximumSharePrice { get; set; }
        public double MaximumBuyDuringRun { get; set; }
        public double MinimumTimeToRevisit { get; set; }
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
