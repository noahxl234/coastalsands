using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COSA_GetAuth.Services
{
    public static class FileHelper
    {
        public static string? ReadFromFile(string fileName)
        {
            string? result = null;

            using (StreamReader sr = new StreamReader(fileName))
            {
                result = sr.ReadToEnd();
            }

            return result;

        }

        public static void WriteToFile(string fileName, string content)
        {

            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.Write(content);
                sw.Close();
            }

        }

        public static string GetAuthFilename(string type)
        {

            string result = "Auth_" + type;

            return result;

        }


    }

}