using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Adminthulhu
{
    public class Permissions : IConfigurable
    {
        public static Dictionary<ulong, PermissionSet> allPermissions = new Dictionary<ulong, PermissionSet> ();
        public static Dictionary<Type, State> defaultPermissions = new Dictionary<Type, State> ();
        public static string filePath = Program.dataPath + "/permissions" + Program.gitHubIgnoreType;

        public class PermissionSet {
            // This class could be replaced with a more complicated dictionary, but I don't really like doing that.
            public Dictionary<Type, State> permissions = new Dictionary<Type, State> ();
        }

        public enum Type {
            Null = -1, VoteForEvents, LockChannels, CreateEvents, CreateCustomCommands, UseAdvancedCommands, Count
        }

        public enum State {
            Null = -1, Inherit, Disallowed, Allowed, Count
        }

        public static void Initialize() {
            LoadData ();
            Permissions permissions = new Permissions ();
            permissions.LoadConfiguration ();
            BotConfiguration.AddConfigurable (permissions);
        }

        public static void SaveData() {
            SerializationIO.SaveObjectToFile (filePath, allPermissions, true, false);
        }

        public static void LoadData() {
            allPermissions = SerializationIO.LoadObjectFromFile<Dictionary<ulong, PermissionSet>> (filePath);
            if (allPermissions == null) {
                allPermissions = new Dictionary<ulong, PermissionSet> ();
                SaveData ();
            }
        }

        public static void SetPermissions (ulong owner, Type type, State state) {
            if (!allPermissions.ContainsKey (owner))
                allPermissions.Add (owner, new PermissionSet ());
            if (allPermissions [ owner ].permissions.ContainsKey (type)) {
                allPermissions [ owner ].permissions [ type ] = state;
            } else {
                allPermissions [ owner ].permissions.Add (type, state);
            }
            SaveData ();
        }

        public static PermissionSet GetPermission(ulong owner) {
            if (allPermissions.ContainsKey (owner))
                return allPermissions [ owner ];
            return null;
        }

        public static bool HasPermission(SocketGuildUser user, Type type) {
            State finalState = defaultPermissions [ type ];
            // Get and reverse given user roles, should now follow Discord hirachy.
            SocketRole everyoneRole = Utility.GetServer ().EveryoneRole;
            List<ulong> toCheck = user.Roles.Where (x => x != everyoneRole).Select (x => x.Id).ToList ();
            toCheck.Reverse ();
            toCheck.Add (user.Id); // Add the user himself for potiential overrides.

            foreach (ulong id in toCheck) {
                PermissionSet permissionSet = GetPermission (id);
                if (permissionSet != null) { // If permission set or specific permission isn't declared, then ignore. Similar to if it was Inherit.
                    if (permissionSet.permissions.ContainsKey (type)) {
                        if (permissionSet.permissions [ type ] != State.Inherit)
                            finalState = permissionSet.permissions [ type ];
                        // Good lord thats a lot of nesting, might want to reformat a bit.
                    }
                }
            }

            return finalState == State.Allowed || finalState == State.Inherit;
        }

        public void LoadConfiguration() {
            defaultPermissions = new Dictionary<Type, State> ();
            for (int i = 0; i < (int)Type.Count; i++) {
                defaultPermissions.Add ((Type)i, BotConfiguration.GetSetting ("Permissions." + ((Type)i).ToString () + "Default", this, State.Inherit));
            }
        }
    }

    public class PermissionCommands : CommandSet {

        public PermissionCommands() {
            command = "permissions";
            shortHelp = "Extended bot permissions commands.";
            catagory = Category.Admin;
            isAdminOnly = true;

            commandsInSet = new Command [ ] {

            };
        }

        public override void Initialize() {
            base.Initialize ();
            List<SetBase> newCommands = new List<SetBase> ();
            for (int i = 0; i < (int)Permissions.Type.Count; i++) {
                SetBase setBase = new SetBase ();
                setBase.type = (Permissions.Type)i;

                setBase.command = setBase.type.ToString ().ToLower ();
                setBase.shortHelp = $"Change permission {setBase.type}.";
                setBase.AddOverload (typeof (bool), $"Set permission {setBase.type} for the given owner to the given state.");
                newCommands.Add (setBase);
            }
            AddProceduralCommands (newCommands.ToArray ());
        }

        public class SetBase : Command {

            public Permissions.Type type;

            public Task<Result> Execute(SocketUserMessage e, ulong owner, string state) {
                SoftStringComparer softStringComparer = new SoftStringComparer ();
                Permissions.State s = Permissions.State.Null;
                for (int i = 0; i < (int)Permissions.State.Count; i++) {
                    if (softStringComparer.Equals (((Permissions.State)i).ToString (), state)) {
                        s = (Permissions.State)i;
                    }
                }

                if (s == Permissions.State.Null) {
                    return TaskResult (false, "Failed to set permission - Could not parse state.");
                }

                Permissions.SetPermissions (owner, type, s);
                return TaskResult (true, $"Succesfully set permission {type} for {owner} to {s}.");
            }
        }

    }
}
