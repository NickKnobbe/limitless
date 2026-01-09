namespace Limitless.Screening
{
    internal abstract class Screener
    {
        public enum ScreenerState
        {
            None = 0,
            Unready = 1,
            Searching = 2,
            Found = 3,
            Resetting = 4,
            Dormant = 5
        }

        public Screener()
        {
            
        }

        public abstract void AssignScreeningStocks(List<string> symbols);

        public abstract bool RescreenDue();

        public abstract Task<List<string>> Screen(DateTime currentTime);
    }
}
