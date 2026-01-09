namespace Limitless.Screening
{
    internal class ActivityScreener : Screener 
    {

        public ActivityScreener() : base() { }

        public override void AssignScreeningStocks(List<string> symbols)
        {
            throw new NotImplementedException();
        }

        public override bool RescreenDue()
        {
            return false;
        }

        public override async Task<List<string>> Screen(DateTime currentTime)
        {
            return new List<string>();
        }
    }
}
