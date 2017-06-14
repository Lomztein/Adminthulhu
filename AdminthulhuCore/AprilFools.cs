using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Adminthulhu
{
    public class AprilFools : IClockable {

        public DateTime day = new DateTime (2000, 4, 1);
        public ulong ignoreUserID = 0;

        public async Task Initialize(DateTime time) {
            await Task.Delay (10000);
            Program.discordClient.MessageReceived += DiscordClient_MessageReceived; // Eh whatever
            OnHourPassed (DateTime.Now);
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public async Task OnHourPassed(DateTime time) {
            await Task.Delay (10000);
            SocketRole role = Utility.GetServer ().GetRole (273017450390487041); // Yay for hardcoding!
            IEnumerable<SocketGuildUser> activeUsers = Utility.GetServer ().Users.Where (x => x.Roles.Contains (role));
            Random random = new Random ();

            ignoreUserID = activeUsers.ElementAt (random.Next (0, activeUsers.Count ())).Id;
        }

        public Task OnMinutePassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        private async Task DiscordClient_MessageReceived(SocketMessage arg) {
            if (DateTime.Now.Month == day.Month && DateTime.Now.Day == day.Day && arg.Author.Id != Program.discordClient.CurrentUser.Id) {
                await arg.DeleteAsync ();
                Program.messageControl.SendMessage (arg.Channel as SocketTextChannel, arg.Content, true);
            }
        }
    }
}
