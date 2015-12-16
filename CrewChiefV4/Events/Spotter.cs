using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.Events
{
    interface Spotter
    {
        void clearState();

        void trigger(Object lastState, Object currentState);

        void enableSpotter();

        void disableSpotter();

        void pause();

        void unpause();
    }
}
