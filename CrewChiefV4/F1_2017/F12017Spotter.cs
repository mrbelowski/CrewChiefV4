using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

namespace CrewChiefV4.F1_2017
{
    class F12017Spotter : Spotter
    {
        public F12017Spotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            // TODO: car sizes
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, true, 4, 2);
        }

        public override void clearState()
        {
            internalSpotter.clearState();
        }

        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            // todo: maybe a bit of spotting
            return;
        }
    }
}
