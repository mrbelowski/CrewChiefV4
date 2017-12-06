using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.Events
{
    public abstract class Spotter
    {
        protected NoisyCartesianCoordinateSpotter internalSpotter;

        protected Boolean paused = false;

        protected Boolean enabled;

        protected Boolean initialEnabledState;

        protected AudioPlayer audioPlayer;

        public abstract void clearState();

        public abstract void trigger(Object lastState, Object currentState, GameStateData currentGameState);

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playMessageImmediately(new QueuedMessage(NoisyCartesianCoordinateSpotter.folderEnableSpotter, 0, null));
        }
        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playMessageImmediately(new QueuedMessage(NoisyCartesianCoordinateSpotter.folderDisableSpotter, 0, null));
        }

        public void pause()
        {
            this.paused = true;
        }

        public void unpause()
        {
            this.paused = false;
        }

        public virtual GridSide getGridSide()
        {
            return GridSide.UNKNOWN;
        }
    }

    public enum GridSide
    {
        UNKNOWN, LEFT, RIGHT
    }
}
