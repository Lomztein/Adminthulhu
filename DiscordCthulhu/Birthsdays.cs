using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {

    class Birthsdays : IClockable {

        // This contains birthsday dates, should only use month and day value.
        public static List<Date> birthsdays;

        public async void Initialize ( DateTime time ) {
            while (Program.GetServer () == null)
                await Task.Delay (1000);

            IEnumerable<User> users = Program.GetServer ().Users;
            birthsdays = new List<Date> ();

            foreach (User u in users) {
                // Heres to hoping no user ever has the ID 0.
                Date date = UserSettings.GetSetting<Date> (u.Id, "Birthsday", null);
                if (date != null)
                    birthsdays.Add (date);
            }
        }

        public void OnDayPassed ( DateTime time ) {
        }

        public void OnHourPassed ( DateTime time ) {
        }

        public void OnMinutePassed ( DateTime time ) {
            foreach (Date date in birthsdays) {
                // userID = 0 if the user hasn't set a date.
                if (date.userID == 0)
                    continue;

                DateTime dateThisYear = new DateTime (DateTime.Now.Year, date.day.Month, date.day.Day, date.day.Hour, date.day.Minute, 0);
                if (DateTime.Now > dateThisYear && !date.passedThisYear) {
                    AnnounceBirthsday (date);
                    date.passedThisYear = true;
                }
            }
        }

        public void OnSecondPassed ( DateTime time ) {
        }

        private void AnnounceBirthsday (Date date) {
            User user = Program.GetServer ().GetUser (date.userID);
            Channel main = Program.GetMainChannel (Program.GetServer ());
            // I have no idea if this works, and it's possibly the worst possible way I could have done that.

            int age = 0;
            try {
                age = DateTime.MinValue.Add (DateTime.Now - date.day).Year - DateTime.MinValue.Year;
            } catch (IndexOutOfRangeException e) {
                Console.WriteLine (user.Name + " has somehow set their birthsday to be before now. wat.");
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

            Program.messageControl.SendMessage (main, "It's **" + Program.GetUserName (user) + "'s** birthsday today, wish them congratulations, as you throw them into the depths of hell on their **" + age + ageSuffix + "** birthsday!");
            Program.messageControl.SendMessage (user, "This is an official completely not cold and automated birthsday greeting, from the loving ~~nazimods~~ admins of **" + Program.serverName + "**: - Happy birthdsay!");
        }

        public static void SetBirthsday (ulong userID, DateTime day) {
            // Well that's a lot easier than looping each time, these could prove useful to fully understand.
            Date date = birthsdays.Find (x => x.userID == userID);
            if (date == null) {
                birthsdays.Add (new Date (userID, day));
            }else {
                date = new Date (userID, day);
            }

            UserSettings.SetSetting (userID, "Birthsday", date);
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
