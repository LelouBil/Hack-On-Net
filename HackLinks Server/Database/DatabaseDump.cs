using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Database
{
    static class DatabaseDump
    {
        private static List<string> commands = new List<string>
        {
            
        };

        public static string LoadCommands() {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "HackLinks_Server.Ressources.SqlSchema.sql";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
        }

        public static void addBinaries() {

        }

        public static List<string> Commands => commands;
    }
}
