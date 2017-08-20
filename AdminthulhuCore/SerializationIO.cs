using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Adminthulhu {
    class SerializationIO {

        public static T LoadObjectFromFile<T>(string path) {
            Logging.Log (Logging.LogType.SYSTEM, "Loading file from path: " + path);
            try {
                if (File.Exists (path)) {
                    using (StreamReader reader = File.OpenText (path)) {
                        string contents = reader.ReadToEnd ();
                        object data = default (T);

                        try { // First attempt to load the file as encrypted, if this fails then load as non-encrypted.
                            string decrypt;
                            try {
                                decrypt = Encryption.OldDecrypt (contents);
                                data = JsonConvert.DeserializeObject<T> (decrypt);
                                return (T)data;
                            } catch {
                                decrypt = Encryption.Decrypt (contents);
                                data = JsonConvert.DeserializeObject<T> (decrypt);
                                return (T)data;
                            }
                        } catch {
                            try {
                                data = JsonConvert.DeserializeObject<T> (contents);
                                return (T)data;
                            } catch {
                                Logging.Log (Logging.LogType.CRITICAL, "File " + path + " was found, but could not be loaded. Continuing now can erase the previous file.");
                            }
                        }
                    }
                } else {
                    Logging.Log (Logging.LogType.WARNING, "File " + path + " not found, this means there is most likely going to be created one at said path.");
                }
            } catch (Exception e) {
                Logging.Log (Logging.LogType.CRITICAL, e.Message);
            }

            return default (T);
        }

        public static void SaveObjectToFile (string fileName, object obj, bool format = false, bool encrypt = true) {
            try {
                using (StreamWriter writer = File.CreateText (fileName)) {
                    string jsonString = JsonConvert.SerializeObject (obj, format ? Formatting.Indented : Formatting.None);
                    string postEncrypt = encrypt ? Encryption.Encrypt (jsonString) : jsonString;
                    writer.Write (postEncrypt);
                }
            } catch (Exception e) {
                Logging.Log (Logging.LogType.EXCEPTION, "Error: Failed to save file: " + e.Message);
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
