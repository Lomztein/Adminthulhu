﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Lomz.ProgramPatcher;

namespace Adminthulhu
{
    public class AutoPatcher : IClockable {

        public static string url = "https://github.com/Lomztein/Adminthulhu/blob/master/Compiled/changelog.txt";

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
            string changelog = await Patcher.DownloadChangelog (url);
            Logging.DebugLog (Logging.LogType.BOT, changelog);
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }
    }
}
