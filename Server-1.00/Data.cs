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
        }

        public static string Serialize<T>(this T target) {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            StringWriter writer = new StringWriter();
            xml.Serialize(writer, target);
            return writer.ToString();
        }

        public static T Desrialize<T>(this string target) {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(target);
            return (T)xml.Deserialize(reader);
        }
    }
}