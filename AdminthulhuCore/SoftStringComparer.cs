using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    class SoftStringComparer : IEqualityComparer<string> {
        public bool Equals(string x, string y) {
            if (x == null || y == null)
                return false;

            if (x.Length > y.Length) {
                return x.Substring (0, y.Length) == y;
            } else if (x.Length < y.Length) {
                return y.Substring (0, x.Length) == x;
            } else {
                return x == y;
            }
        }

        public int GetHashCode(string obj) {
            throw new NotImplementedException ();
        }
    }
}
