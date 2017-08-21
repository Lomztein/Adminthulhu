using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Diagnostics;

namespace Adminthulhu
{
    public class AutoPatcher : IClockable {

        public static string url = "https://raw.githubusercontent.com/Lomztein/Adminthulhu/master/Compiled/";

        public Task Initialize(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public async Task OnMinutePassed(DateTime time) {
            using (HttpClient client = new HttpClient ()) {
                string basePath = AppContext.BaseDirectory + "/";

                string localVersion = "";
                try {
                    localVersion = SerializationIO.LoadTextFile (basePath + "version.txt") [ 0 ];
                } catch { }

                string version = await client.GetStringAsync (url + "version.txt");

                if (localVersion != version) {
                    try {
                        string changelog = await client.GetStringAsync (url + "changelog.txt");

                        Process patcher = new Process ();
                        patcher.StartInfo.FileName = AppContext.BaseDirectory + "/patcher/AdminthulhuPatcher.dll";
                        patcher.StartInfo.CreateNoWindow = false;
                        patcher.StartInfo.UseShellExecute = false;

                        patcher.StartInfo.Arguments = url + ", " + basePath;
                        patcher.Start ();

                        Environment.Exit (0);
                    } catch (Exception e) {
                        Logging.Log (Logging.LogType.EXCEPTION, e + " - " + e.StackTrace);
                        throw;
                    }
                }
            }
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }
    }
}
