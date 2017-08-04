using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Adminthulhu {
    class SerializationIO {

        public static T LoadObjectFromFile<T> ( string path, bool critical = false ) {
            try {
                if (File.Exists (path)) {
                    using (StreamReader reader = File.OpenText (path)) {
                        using (JsonTextReader jReader = new JsonTextReader (reader)) {
                            JsonSerializer serializer = new JsonSerializer ();
                            object data = serializer.Deserialize<T> (jReader);

                            jReader.Close ();
                            return (T)data;
                        }
                    }
                }
                ChatLogger.Log ("Failed to load file at " + path);
                return default (T);
            } catch (Exception e) {
                ChatLogger.Log ("Error: " + e.Message);
                if (critical)
                    throw e;
                else
                    return default (T);
            }
        }

        public static void SaveObjectToFile (string fileName, object obj, bool format = false) {
            try {
                using (StreamWriter writer = File.CreateText (fileName)) {
                    string jsonString = JsonConvert.SerializeObject (obj, format ? Formatting.Indented : Formatting.None);
                    writer.Write (jsonString);
                }
            } catch (Exception e) {
                ChatLogger.Log ("Error: Failed to save file: " + e.Message);
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
