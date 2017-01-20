using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCthulhu {
    public interface IClockable {

        void Initialize ( DateTime time );

        void OnSecondPassed ( DateTime time );

        void OnMinutePassed ( DateTime time );

        void OnHourPassed ( DateTime time );

        void OnDayPassed ( DateTime time );
    }
}
