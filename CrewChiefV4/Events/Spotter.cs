﻿using CrewChiefV4.Audio;
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

        protected virtual float[] getWorldPositionOfDriverAtPosition(Object currentStateObj, int position)
        {
            return new float[] { 0, 0 };
        }

        public virtual GridSide getGridSide(Object currentStateObj)
        {
            return GridSide.UNKNOWN;
        }

        protected GridSide getGridSideInternal(Object currentStateObj, float playerRotation, float playerXPosition, float playerZPosition, int playerStartingPosition, int numCars)
        {
            Boolean countForwards = playerStartingPosition != 1;

            float[] worldPositionOfOpponent = null;
            int opponentStartingPositionToCheck = countForwards ? playerStartingPosition - 1 : playerStartingPosition + 1;
            int opponentCheckCount = 0;
            // only check 5 opponents, then give up
            while (opponentCheckCount < 5)
            {
                worldPositionOfOpponent = getWorldPositionOfDriverAtPosition(currentStateObj, opponentStartingPositionToCheck);
                if (worldPositionOfOpponent != null)
                {
                    float[] alignedCoordiates = this.internalSpotter.getAlignedXZCoordinates(playerRotation,
                        playerXPosition, playerZPosition, worldPositionOfOpponent[0], worldPositionOfOpponent[1]);
                    if (alignedCoordiates[0] < -2)
                    {
                        return GridSide.RIGHT;
                    }
                    else if (alignedCoordiates[0] > 2)
                    {
                        return GridSide.LEFT;
                    }
                }
                if (countForwards)
                {
                    if (opponentStartingPositionToCheck == 1)
                    {
                        // we're counting forwards and have reached pole, so go back from the player
                        opponentStartingPositionToCheck = playerStartingPosition + 1;
                        countForwards = false;
                    }
                    else
                    {
                        opponentStartingPositionToCheck--;
                    }
                }
                else
                {
                    if (opponentStartingPositionToCheck == numCars - 1)
                    {
                        // we're counting backwards and have reached last, so go forward from the player
                        opponentStartingPositionToCheck = playerStartingPosition - 1;
                        countForwards = true;
                    }
                    else
                    {
                        opponentStartingPositionToCheck++;
                    }
                }
            }
            return GridSide.UNKNOWN;
        }
    }

    public enum GridSide
    {
        UNKNOWN, LEFT, RIGHT
    }
}
