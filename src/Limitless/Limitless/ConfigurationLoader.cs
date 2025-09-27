using Newtonsoft.Json;

namespace Limitless
{
    internal class ConfigurationLoader
    {
        internal T? TryLoadJSON<T>(string path)
            where T : class
        {
            try
            {
                string json = string.Empty;
                using (StreamReader r = new StreamReader(path))
                {
                    json = r.ReadToEnd();
                }

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
