using Alpaca.Markets;

namespace Limitless.Screening
{
    internal class ScreeningDataGatherer
    {
        private readonly Configuration _launchSettings;
        private IAlpacaDataClient dataClient;

        public ScreeningDataGatherer(Configuration launchSettings, IAlpacaDataClient client)
        {
            _launchSettings = launchSettings;
            dataClient = client;
        }

        public async Task<IReadOnlyList<IActiveStock>> GetMostActive(DateTime atDateTime, int numberOfStocks)
        {
            return await dataClient.ListMostActiveStocksByVolumeAsync(numberOfStocks);
        }
    }
}
