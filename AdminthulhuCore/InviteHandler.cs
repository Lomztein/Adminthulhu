using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Rest;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu
{
    public static class InviteHandler
    {
        public static Dictionary<string, RestInviteMetadata> savedInvites;

        public static void Initialize() {
            UpdateData (null);
        }

        public static async void UpdateData(IReadOnlyCollection<RestInviteMetadata> readOnly) {
            try {
                await Utility.AwaitFullBoot ();
                if (readOnly == null)
                    readOnly = await Utility.GetServer ().GetInvitesAsync ();

                savedInvites = readOnly.ToDictionary (x => x.Code);
            } catch (Exception e) {
                Logging.Log (e);
            }
        }

        public static async Task<RestInviteMetadata> FindInviter() {
            try {
                IReadOnlyCollection<RestInviteMetadata> newInvites = await Utility.GetServer ().GetInvitesAsync ();
                Dictionary<string, RestInviteMetadata> dict = newInvites.ToDictionary (x => x.Code);
                RestInviteMetadata result = null;

                foreach (var key in dict) {
                    if (savedInvites.ContainsKey (key.Key)) {
                        if (savedInvites [ key.Key ].Uses + 1 == key.Value.Uses) {
                            result = key.Value;
                        }
                    } else {
                        if (key.Value.Uses == 1)
                            result = key.Value;
                    }
                }

                UpdateData (newInvites);
                return result;
            } catch (Exception e) {
                Logging.Log (e);
                return null;
            }
        }
    }
}
