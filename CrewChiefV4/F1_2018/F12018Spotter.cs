using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using F1UdpNet;

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
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, true, UserSettings.GetUserSettings().getFloat("f1_2018_spotter_car_length"), 2);
        }

        public override void clearState()
        {
            internalSpotter.clearState();
        }

        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            F12018StructWrapper currentWrapper = (F12018StructWrapper)currentStateObj;
            if (currentWrapper.packetLapData.m_lapData == null || currentWrapper.packetMotionData.m_carMotionData == null || currentWrapper.packetCarTelemetryData.m_carTelemetryData == null)
            {
                return;
            }
            Boolean inPits = currentWrapper.packetLapData.m_lapData[currentWrapper.packetLapData.m_header.m_playerCarIndex].m_pitStatus != 0;
            Boolean isSpectating = currentWrapper.packetSessionData.m_isSpectating != 0;

            if (inPits || isSpectating)
            {
                return;
            }

            DateTime now = new DateTime(currentWrapper.ticksWhenRead);

            if (enabled && currentWrapper.packetMotionData.m_carMotionData.Length > 1)
            {
                int playerIndex = currentWrapper.packetMotionData.m_header.m_playerCarIndex;

                CarMotionData playerData = currentWrapper.packetMotionData.m_carMotionData[playerIndex];

                float[] currentPlayerPosition = new float[] { playerData.m_worldPositionX, playerData.m_worldPositionZ };

                List<float[]> currentOpponentPositions = new List<float[]>();
                List<float[]> currentOpponentVelocityData = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = (float)currentWrapper.packetCarTelemetryData.m_carTelemetryData[currentWrapper.packetCarTelemetryData.m_header.m_playerCarIndex].m_speed * 0.277778f;
                playerVelocityData[1] = playerData.m_worldVelocityX;
                playerVelocityData[2] = playerData.m_worldVelocityZ;

                for (int i = 0; i < currentWrapper.packetMotionData.m_carMotionData.Length; i++)
                {
                    if (i == playerIndex)
                    {
                        continue;
                    }
                    CarMotionData opponentData = currentWrapper.packetMotionData.m_carMotionData[i];
                    float[] currentPositions = new float[] { opponentData.m_worldPositionX, opponentData.m_worldPositionZ };
                    currentOpponentPositions.Add(currentPositions);
                    currentOpponentVelocityData.Add(new float[] { opponentData.m_worldVelocityX, opponentData.m_worldVelocityZ });
                }
                if (currentOpponentPositions.Count() > 0)
                {
                    float playerRotation = playerData.m_yaw;
                    if (playerRotation < 0)
                    {
                        playerRotation = playerRotation * -1;
                    }
                    else
                    {
                        playerRotation = twoPi - playerRotation;
                    }
                    internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions, currentOpponentVelocityData);
                }
            }
        }
    }
}
