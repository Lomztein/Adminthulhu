using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Adminthulhu {

    class Birthdays : IClockable, IConfigurable {

        // This contains birthday dates, should only use month and day value.
        public static List<Date> birthdays;

        public static string onBirthdayAnnouncementMessage = "Congratulations to **{USERNAME}**, as they today celebrate their {AGE} birthday!";
        public static string onBirthdayCongratulationsDM = "Hello, I just wanted to wish you happy birthday today, and I hope you have a great day! :D";

        public async Task Initialize(DateTime time) {

            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);

                while (Utility.GetServer () == null)
                    await Task.Delay (1000);

                IEnumerable<SocketGuildUser> users = Utility.GetServer ().Users;
                birthdays = new List<Date> ();

                foreach (SocketGuildUser u in users) {
                    try {
                        // Heres to hoping no user ever has the ID 0.
                        Date date = UserConfiguration.GetSetting<Date> (u.Id, "Birthday");
                        if (date != null)
                            birthdays.Add (date);
                    } catch (Exception fuck) {
                        Logging.Log (Logging.LogType.EXCEPTION, fuck.Message);
                    }
                }
        }

        public Task OnHourPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed(DateTime time) {
            if (birthdays != null) {
                foreach (Date date in birthdays) {
                    // userID = 0 if the user hasn't set a date.
                    if (date.userID == 0)
                        continue;

                    DateTime dateThisYear = new DateTime (DateTime.Now.Year, date.day.Month, date.day.Day, date.day.Hour, date.day.Minute, date.day.Second);
                    if (DateTime.Now > dateThisYear && date.lastPassedYear != DateTime.Now.Year) {
                        AnnounceBirthday (date);
                        date.lastPassedYear = DateTime.Now.Year;
                        UserConfiguration.SetSetting (date.userID, "Birthday", date);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        private void AnnounceBirthday (Date date) {
            SocketGuildUser user = Utility.GetServer ().GetUser (date.userID);
            SocketGuildChannel main = Utility.GetMainChannel ();
            // I have no idea if this works, and it's possibly the worst possible way I could have done that.

            int age = 0;
            try {
                age = DateTime.MinValue.Add (DateTime.Now - new DateTime (date.day.Year, date.day.Month, date.day.Day)).Year - DateTime.MinValue.Year;
            } catch (IndexOutOfRangeException) {
                Logging.Log (Logging.LogType.EXCEPTION, user.Username + " has somehow set their birthday to be before now. wat.");
            }

            string ageSuffix = "'th";
            switch (age.ToString ().LastOrDefault ()) {
                case '1':
                    ageSuffix = "'st";
                    break;
                case '2':
                    ageSuffix = "'nd";
                    break;
                case '3':
                    ageSuffix = "'rd";
                    break;
            }

            if (age > 10 && age < 14)
                ageSuffix = "'th";

            Program.messageControl.SendMessage (main as SocketTextChannel, onBirthdayAnnouncementMessage.Replace ("{USERNAME}", Utility.GetUserName (user)).Replace ("{AGE}", age + ageSuffix), true);
            Program.messageControl.SendMessage (user, onBirthdayCongratulationsDM);
        }

        public static bool IsUsersBirthday(SocketUser user) {
            if (birthdays == null)
                return false;

            Date date = birthdays.Find (x => x.userID == user.Id);
            if (date == null)
                return false;

            if (date.day.Month == DateTime.Now.Month && date.day.Day == DateTime.Now.Day)
                return true;
            return false;
        }

        public static void SetBirthday (ulong userID, DateTime day) {
            // Well that's a lot easier than looping each time, these could prove useful to fully understand.
            Date oldDate = birthdays.Find (x => x.userID == userID);
            Date newDate;

            newDate = new Date (userID, day);

            birthdays.Remove (oldDate);
            birthdays.Add (newDate);
            UserConfiguration.SetSetting (userID, "Birthday", newDate);
        }

        public void LoadConfiguration() {
            onBirthdayAnnouncementMessage = BotConfiguration.GetSetting ("Birthdays.OnBirthdayAnnouncementMessage", "", onBirthdayAnnouncementMessage);
            onBirthdayCongratulationsDM = BotConfiguration.GetSetting ("Birthdays.OnBirthdayCongratulationsDM", "", onBirthdayCongratulationsDM);
        }

        public class Date {

            public ulong userID;
            public DateTime day;

            public long lastPassedYear;

            public Date (ulong id, DateTime _day) {
                userID = id;
                day = _day;

                DateTime dateThisYear = new DateTime (DateTime.Now.Year, day.Month, day.Day, day.Hour, day.Minute, day.Second);
                if (DateTime.Now > dateThisYear) {
                    lastPassedYear = DateTime.Now.Year;
                }
            }

        }
    }
}
