using Microsoft.Extensions.Configuration;

namespace COSA_GetAuth.Coastal.Helpers
{
    public static class LogHelper
    {

        public static void LogSummary(string message)
        {
            Log(message, "summary");
            Console.WriteLine(message);
        }

        public static void LogDetail(string message)
        {
            Log(message, "detail");
        }

        public static void Log(string message, string logType = "detail")
        {
            //get filename
            bool isSummary = logType.ToLower() == "summary";
            string baseFolder = GetLogFolder();
            string dateFolder = DateTime.Today.ToString("yyyyMM");
            string filename = string.Format("{0}{1}{2}", isSummary ? "LogSummary_" : "LogDetail_", DateTime.Today.ToString("yyyyMM"), ".txt");
            string logFilename = Path.Combine(baseFolder, dateFolder, filename);

            //compose line
            string result = string.Format("{0} {1}", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), message);

            try
            {
                //write to log
                //Console.WriteLine(result);
                using (StreamWriter w = File.AppendText(logFilename))
                {
                    w.WriteLine(result);
                }

            }
            catch (Exception e)
            {
                string msg = e.Message;
            }

        }

        private static string GetLogFolder()
        {
            //get path from app.config
            //string baseFolder = _config["LogBaseFolder"];
            string baseFolder = ConfigHelper.Setting("LogBaseFolder");

            string dateFolder = DateTime.Today.ToString("yyyyMM");
            string fullFolder = Path.Combine(baseFolder, dateFolder);
            Directory.CreateDirectory(fullFolder);
            return baseFolder;
        }


    }

}
