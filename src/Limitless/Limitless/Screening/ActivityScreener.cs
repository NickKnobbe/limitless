namespace Limitless.Screening
{
    internal class ActivityScreener : Screener 
    {

        public ActivityScreener() : base() { }

        public override bool RescreenDue()
        {
            return false;
        }

        public override async Task<List<string>> Rescreen(DateTime currentTime)
        {
            return new List<string>();
        }
    }
}
