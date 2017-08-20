using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    // This encryption isn't in any way designed to be unbreakable, however it is designed to make sure the bot follows Discord API's ToS. Consider this an act of /r/maliciouscompliance.
    public class Encryption : IConfigurable {

        public static byte encryptionKey = 128;
        public static int encryptionSeed = 0;

        public static void Initialize () {
            Encryption enc = new Encryption ();
            enc.LoadConfiguration ();
            BotConfiguration.AddConfigurable (enc);

            while (!DoEncryptionCheck ()) {
                Console.WriteLine ("!WARNING! - ENCRYPTION FAILED TO SYNCRONIZE INPUT AND RESULT, THIS WILL HINDER FILES FROM LOADING CORRECTLY. CONTACT DEVELOPER IMMIDIATELY IF THIS OCCURS IN RELEASE VERSION. WRITE \"continue\" TO IGNORE.");
                if (Console.ReadLine () == "continue")
                    break;
            }
        }

        public static bool DoEncryptionCheck() {
            Logging.Log (Logging.LogType.BOT, "Running encryption check..");
            string testString = "the quick brown fox jumps over the lazy dog. THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG. 0123456789 ,+-*/!\"#¤%&/()=?`^*>\\<@£$€{[]}|";
            string encryption = Encrypt (testString);
            Logging.Log (Logging.LogType.BOT, "Encryption input: " + testString);
            string decryption = Decrypt (encryption);
            Logging.Log (Logging.LogType.BOT, "Decryption ouput: " + decryption);
            Logging.Log (Logging.LogType.BOT, "Yes that is spelled wrong, but it lines up the outputs.");

            return decryption == testString;
        }

        public void LoadConfiguration() {
            Random random = new Random ();
            encryptionKey = BotConfiguration.GetSetting ("Misc.EncryptionKey", "", (byte)random.Next (1, 255));
            encryptionSeed = BotConfiguration.GetSetting ("Misc.EncryptionSeed", "", random.Next (int.MinValue, int.MaxValue));
        }

        public static string Encrypt(string input) {
            string result = "";
            Random random = new Random (encryptionSeed);
            for (int i = 0; i < input.Length; i++)
                result += (char)(input [ i ] + encryptionKey + (byte)random.Next (0, 255));
            return result;
        }

        public static string Decrypt(string input) {
            string result = "";
            Random random = new Random (encryptionSeed);
            for (int i = 0; i < input.Length; i++)
                result += (char)(input [ i ] - (encryptionKey + (byte)random.Next (0, 255)));
            return result;
        }

        public static string OldDecrypt(string input) {
            string result = "";
            for (int i = 0; i < input.Length; i++)
                result += (char)((ulong)input [ i ] - encryptionKey);
            return result;
        }
    }
}
