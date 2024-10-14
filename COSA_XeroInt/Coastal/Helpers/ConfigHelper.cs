using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COSA_XeroInt.Coastal.Helpers
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
