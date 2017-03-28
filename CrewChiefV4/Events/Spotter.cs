using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.Events
{
    interface Spotter
    {
        void clearState();

        void trigger(Object lastState, Object currentState, GameStateData currentGameState);

        void enableSpotter();

        void disableSpotter();

        void pause();

        void unpause();
    }
}
