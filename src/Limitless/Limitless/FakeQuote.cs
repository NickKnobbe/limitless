using Alpaca.Markets;

namespace Limitless
{
    /// <summary>
    /// Used to mimic an Alpaca quote.
    /// </summary>
    internal class FakeQuote : IQuote
    {
        public string Symbol { get; set; } = string.Empty;

        public DateTime TimestampUtc { get; set; }

        public string BidExchange { get; set; } = string.Empty;

        public string AskExchange { get; set; } = string.Empty;

        public decimal BidPrice { get; set; }

        public decimal AskPrice { get; set; }

        public decimal BidSize { get; set; }

        public decimal AskSize { get; set; }

        public string Tape { get; set; } = string.Empty;

        public IReadOnlyList<string> Conditions => new List<string>();

        public static FakeQuote FromBar(IBar bar)
        {
            return new FakeQuote()
            {
                Symbol = bar.Symbol,
                TimestampUtc = bar.TimeUtc,
                BidPrice = bar.Close,
                AskPrice = bar.Close
            };
        }
    }
}
