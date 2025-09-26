namespace Limitless
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("The program has begun.");

            try
            {
                var settingsPath = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\..\\..\\..\\..\\launch_settings.json";
                var configLoader = new ConfigurationLoader();
                var configuration = await configLoader.TryLoad(settingsPath);

                if (configuration == null)
                {
                    Console.WriteLine("Configuration was null after a load. Aborting program.");
                    return;
                }

                var controller = new TradeController(configuration);
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