using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DiscordCthulhu {
    class SerializationIO {

        public static T LoadObjectFromFile<T> ( string path ) {
            if (File.Exists (path)) {

                BinaryFormatter bf = new BinaryFormatter ();
                FileStream file = File.Open (path, FileMode.Open);

                T data = (T)bf.Deserialize (file);
                file.Close ();
                return data;
            }
            ChatLogger.Log ("Failed to load file at " + path);
            return default (T);
        }

        public static void SaveObjectToFile (string fileName, object obj) {
            BinaryFormatter bf = new BinaryFormatter ();
            FileStream file = File.Create (fileName);

            bf.Serialize (file, obj);
            file.Close ();
        }

        public static string[] LoadTextFile (string path) {
            ChatLogger.Log (path);
            StreamReader reader = File.OpenText (path);

            List<string> con = new List<string> ();
            int maxTries = short.MaxValue;

            while (true && maxTries > 0) {
                Console.WriteLine ("LoadTextFile");
                maxTries--;
                string loc = reader.ReadLine ();
                if (loc == null) {
                    break;
                } else {
                    con.Add (loc);
                }
            }

            return con.ToArray ();
        }
    }
}
