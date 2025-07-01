using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace DevelopersHub.RealtimeNetworking.Server
{
    public static class Data
    {
        public class Player {
            public int gold = 0;
            public int elixir = 0;
            public int gems = 0;
            public List<Building> buildings = new List<Building>();
        }

        public class Building
        {
            public string id = "";
            public int level = 0;
            public long databaseID = 0;
            public int x = 0;
            public int y = 0;
            public int columns = 0;
            public int rows = 0;
        }

        public class ServerBuilding
        {
            public string id = "";
            public int level = 0;
            public long databaseID = 0;
            public int requiredGold = 0;
            public int requiredElixir = 0;
            public int requiredGems = 0; 
            public int columns = 0;
            public int rows = 0;
        }

        public async static Task<string> Serialize<T>(this T target) {
            Task<string> task = Task.Run(() => {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                StringWriter writer = new StringWriter();
                xml.Serialize(writer, target);
                return writer.ToString();
            });
            return await task;
        }

        public async static Task<T> Desrialize<T>(this string target) {
            Task<T> task = Task.Run(() => {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                StringReader reader = new StringReader(target);
                return (T)xml.Deserialize(reader);
            });
            return await task;
        }
    }
}