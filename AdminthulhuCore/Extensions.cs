using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public static class CharExtension {
        public static bool IsTrigger(this char c) {
            return c == Program.commandTrigger [ 0 ] || c == Program.commandTriggerHidden [ 0 ];
        }
    }

    public static class StringExtension {

        public static string Singlify(this string [ ] input, string seperator = "\n", int max = -1) {
            string result = "";
            int index = 0;
            foreach (string str in input) {
                result += str + seperator;
                index++;

                if (max != -1 && max <= index)
                    break;

            }
            return result;
        }
    }
}
