using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {

    class Birthdays : IClockable {

        // This contains birthday dates, should only use month and day value.
        public static List<Date> birthdays;

        public async Task Initialize ( DateTime time ) {
            while (Program.GetServer () == null)
                await Task.Delay (1000);

            IEnumerable<SocketGuildUser> users = Program.GetServer ().Users;
            birthdays = new List<Date> ();

            foreach (SocketGuildUser u in users) {
                // Heres to hoping no user ever has the ID 0.
                Date date = UserSettings.GetSetting<Date> (u.Id, "Birthday", null);
                if (date != null)
                    birthdays.Add (date);
            }
        }

        public Task OnDayPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnHourPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed ( DateTime time ) {
            if (birthdays != null) {
                foreach (Date date in birthdays) {
                    // userID = 0 if the user hasn't set a date.
                    if (date.userID == 0)
                        continue;

                    DateTime dateThisYear = new DateTime (DateTime.Now.Year, date.day.Month, date.day.Day, date.day.Hour, date.day.Minute, 0);
                    if (DateTime.Now > dateThisYear && !date.passedThisYear) {
                        AnnounceBirthday (date);
                        date.passedThisYear = true;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task OnSecondPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        private void AnnounceBirthday (Date date) {
            SocketGuildUser user = Program.GetServer ().GetUser (date.userID);
            SocketGuildChannel main = Program.GetMainChannel (Program.GetServer ());
            // I have no idea if this works, and it's possibly the worst possible way I could have done that.

            int age = 0;
            try {
                age = DateTime.MinValue.Add (DateTime.Now - date.day).Year - DateTime.MinValue.Year;
            } catch (IndexOutOfRangeException) {
                Console.WriteLine (user.Username + " has somehow set their birthday to be before now. wat.");
            }

            string ageSuffix = "'rd";
            switch (age.ToString ().LastOrDefault ()) {
                case '1':
                    ageSuffix = "'st";
                    break;
                case '2':
                    ageSuffix = "'nd";
                    break;
            }

            Program.messageControl.SendMessage (main as SocketTextChannel, "It's **" + Program.GetUserName (user) + "'s** birthday today, wish them congratulations, as you throw them into the depths of hell on their **" + age + ageSuffix + "** birthday!");
            Program.messageControl.SendMessage (user, "This is an official completely not cold and automated birthday greeting, from the loving ~~nazimods~~ admins of **" + Program.serverName + "**: - Happy birthdsay!");
        }

        public static void SetBirthday (ulong userID, DateTime day) {
            // Well that's a lot easier than looping each time, these could prove useful to fully understand.
            Date date = birthdays.Find (x => x.userID == userID);
            if (date == null) {
                birthdays.Add (new Date (userID, day));
            }else {
                date = new Date (userID, day);
            }

            UserSettings.SetSetting (userID, "Birthday", date);
        }

        [Serializable]
        public class Date {

            public ulong userID;
            public DateTime day;
            public bool passedThisYear;

            public Date (ulong id, DateTime _day) {
                userID = id;
                day = _day;

                DateTime dateThisYear = new DateTime (DateTime.Now.Year, day.Month, day.Day, day.Hour, day.Minute, 0);
                if (DateTime.Now > dateThisYear) {
                    passedThisYear = true;
                }
            }

        }
    }
}
