using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCthulhu {
    public interface IClockable {

        Task Initialize ( DateTime time );

        Task OnSecondPassed ( DateTime time );

        Task OnMinutePassed ( DateTime time );

        Task OnHourPassed ( DateTime time );

        Task OnDayPassed ( DateTime time );
    }
}
