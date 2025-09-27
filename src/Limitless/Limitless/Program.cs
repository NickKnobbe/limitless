namespace Limitless
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("The program has begun.");

            try
            {
                var settingsDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\..\\..\\..\\..\\";
                var settingsPath = $"{settingsDirectory}launch_settings.json";
                var secretsPath = $"{settingsDirectory}secrets.json";
                var configLoader = new ConfigurationLoader();
                var configuration = configLoader.TryLoadJSON<Configuration>(settingsPath);
                var secrets = configLoader.TryLoadJSON<Secrets>(secretsPath);

                if (configuration == null || secrets == null)
                {
                    Console.WriteLine("Configuration was null after a load. Aborting program.");
                    return;
                }

                var controller = new TradeController(configuration, secrets);
                await controller.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured and the program will abort: {e.Message}");
            }

            Console.WriteLine("The program has ended.");
        }
    }
}