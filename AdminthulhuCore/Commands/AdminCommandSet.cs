using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class AdminCommandSet : CommandSet
    {
        public AdminCommandSet() {
            command = "admin";
            shortHelp = "Commands for adminstrative ~~abuse~~ stuff.";
            catagory = Category.Utility;
            isAdminOnly = true;

            commandsInSet = new Command [ ] {
                new StrikeCommandSet (), new CAddEventGame (), new CRemoveEventGame (), new CHighlightEventGame (),
                new CAcceptYoungling (), new CReloadConfiguration (), new CSetYoungling (), new CCreatePoll (), new CCheckPatch (),
                new CSetSetting (), new CDisplayFile (), new PermissionCommands (), new CAddHeader (),
            };
        }
    }
}
