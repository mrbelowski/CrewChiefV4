using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

namespace CrewChiefV4.F1_2018
{
    class F12018Spotter : Spotter
    {
        private float twoPi = (float)(2 * Math.PI);

        public F12018Spotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
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
            CrewChiefV4.F1_2018.F12018UDPreader.F12018StructWrapper currentWrapper = (CrewChiefV4.F1_2018.F12018UDPreader.F12018StructWrapper)currentStateObj;
            UDPPacket currentState = currentWrapper.data;
            Boolean inPits = currentState.m_in_pits > 0;
            Boolean isSpectating = currentState.m_is_spectating != 0;

            if (inPits || isSpectating)
            {
                return;
            }
            CrewChiefV4.F1_2018.F12018UDPreader.F12018StructWrapper previousWrapper = (CrewChiefV4.F1_2018.F12018UDPreader.F12018StructWrapper)lastStateObj;
            UDPPacket lastState = previousWrapper.data;

            DateTime now = new DateTime(currentWrapper.ticksWhenRead);
            float interval = (float)(((double)currentWrapper.ticksWhenRead - (double)previousWrapper.ticksWhenRead) / (double)TimeSpan.TicksPerSecond);

            if (enabled && currentState.m_num_cars > 1)
            {
                int playerIndex = currentState.m_player_car_index;

                CarUDPData playerData = currentState.m_car_data[playerIndex];

                float[] currentPlayerPosition = new float[] { playerData.m_worldPosition[0], playerData.m_worldPosition[2] };

                List<float[]> currentOpponentPositions = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = currentState.m_speed;
                playerVelocityData[1] = currentState.m_xv;
                playerVelocityData[2] = currentState.m_zv;

                for (int i = 0; i < currentState.m_num_cars; i++)
                {
                    if (i == playerIndex)
                    {
                        continue;
                    }
                    CarUDPData opponentData = currentState.m_car_data[i];
                    if (opponentData.m_carPosition > 0 /* just a guess....*/)
                    {
                        float[] currentPositions = new float[] { opponentData.m_worldPosition[0], opponentData.m_worldPosition[2] };
                        currentOpponentPositions.Add(currentPositions);
                    }
                }
                if (currentOpponentPositions.Count() > 0)
                {
                    // rotation data isn't documented, just guessing here
                    float playerRotation = currentState.m_xr;
                    if (playerRotation < 0)
                    {
                        playerRotation = playerRotation * -1;
                    }
                    else
                    {
                        playerRotation = twoPi - playerRotation;
                    }
                    internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);
                }
            }
        }
    }
}
