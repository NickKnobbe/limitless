using Newtonsoft.Json;

namespace Limitless
{
    internal class ConfigurationLoader
    {
        internal async Task<Configuration?> TryLoad(string path)
        {
            string json = string.Empty;
            using (StreamReader r = new StreamReader(path))
            {
                json = r.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<Configuration>(json);
        }
    }
}
