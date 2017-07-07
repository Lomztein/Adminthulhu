using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Adminthulhu {
    class SerializationIO {

        public static T LoadObjectFromFile<T> ( string path ) {
            try {
                if (File.Exists (path)) {
                    
                    StreamReader reader = File.OpenText (path);
                    JsonTextReader jReader = new JsonTextReader (reader);

                    JsonSerializer serializer = new JsonSerializer ();
                    object data = serializer.Deserialize<T> (jReader);

                    reader.Dispose ();
                    jReader.Close ();

                    return (T)data;
                }
                Console.WriteLine ("Failed to load file at " + path);
                return default (T);
            } catch (Exception e) {
                Console.WriteLine ("Error: " + e.Message);
                return default (T);
            }
        }

        public static void SaveObjectToFile (string fileName, object obj) {
            try {
                StreamWriter writer = File.CreateText (fileName);
                
                JsonSerializer serializer = new JsonSerializer ();
                serializer.Serialize (writer, obj);

                writer.Dispose ();
            } catch (Exception e) {
                Console.WriteLine ("Error: Failed to save file: " + e.Message);
            }
        }

        public static string[] LoadTextFile (string path) {
            StreamReader reader = File.OpenText (path);

            List<string> con = new List<string> ();
            int maxTries = short.MaxValue;

            while (true && maxTries > 0) {
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

        public static void SaveTextFile (string path, params string[] text) {
            // tfw the method I was going to write already exists. Still going to put it in SerializationIO to centralize file saving and loading.
            File.AppendAllLines (path, text);
        }
    }
}
