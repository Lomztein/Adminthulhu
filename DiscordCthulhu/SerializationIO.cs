using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DiscordCthulhu {
    class SerializationIO {

        public static async Task<T> LoadObjectFromFile<T> ( string path ) {
            try {
                if (File.Exists (path)) {

                    BinaryFormatter bf = new BinaryFormatter ();
                    FileStream file = File.Open (path, FileMode.Open);

                    T data = (T)bf.Deserialize (file);
                    file.Close ();
                    return data;
                }
                ChatLogger.Log ("Failed to load file at " + path);
                return default (T);
            } catch (Exception e) {
                await ChatLogger.DebugLog ("Error: " + e.Message);
                return default (T);
            }
        }

        public static async Task SaveObjectToFile (string fileName, object obj) {
            try {
                BinaryFormatter bf = new BinaryFormatter ();
                FileStream file = File.Create (fileName);

                bf.Serialize (file, obj);
                file.Close ();
            } catch (Exception e) {
                await ChatLogger.DebugLog ("Error: Failed to save file: " + e.Message);
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
