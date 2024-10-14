using Microsoft.Extensions.Configuration;

namespace COSA_GetAuth.Coastal.Helpers
{
    public static class ConfigHelper
    {
        private static IConfiguration _config;

        public static void Initialise(IConfiguration config)
        {
            _config = config;
        }

        public static string Setting(string key)
        {
            string value = _config.GetSection(key).Value;
            return value;
        }
    }
}
