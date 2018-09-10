using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class EngineMonitor : AbstractEvent
    {

        // allow engine status messages during caution periods
        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Checkered, SessionPhase.FullCourseYellow }; }
        }

        private String folderAllClear = "engine_monitor/all_clear";
        private String folderHotWater = "engine_monitor/hot_water";
        private String folderHotOil = "engine_monitor/hot_oil";
        private String folderHotOilAndWater = "engine_monitor/hot_oil_and_water";
        private String folderLowOilPressure = "engine_monitor/low_oil_pressure";
        private String folderLowFuelPressure = "engine_monitor/low_fuel_pressure";
        private String folderStalled = "engine_monitor/stalled";
        EngineStatus lastStatusMessage;

        EngineData engineData;

        float maxSafeOilTemp = 0;
        float maxSafeWaterTemp = 0;

        // record engine data for 60 seconds then report changes
        double statusMonitorWindowLength = 60;

        double gameTimeAtLastStatusCheck;

        DateTime nextOilPressureCheck = DateTime.MinValue;
        DateTime nextFuelPressureCheck = DateTime.MinValue;
        DateTime nextStalledCheck = DateTime.MinValue;

        public EngineMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            lastStatusMessage = EngineStatus.ALL_CLEAR;
            engineData = new EngineData();
            gameTimeAtLastStatusCheck = 0;
            maxSafeOilTemp = 0;
            maxSafeWaterTemp = 0;
            nextOilPressureCheck = DateTime.MinValue;
            nextFuelPressureCheck = DateTime.MinValue;
            nextStalledCheck = DateTime.MinValue;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (engineData == null)
            {
                clearState();
            }
            if (maxSafeWaterTemp == 0) {
                maxSafeWaterTemp = currentGameState.carClass.maxSafeWaterTemp;
            }
            if (maxSafeOilTemp == 0) {
                maxSafeOilTemp = currentGameState.carClass.maxSafeOilTemp;
            }

            // immediately warn about oil pressure / fuel pressure / stalled:
            if (currentGameState.SessionData.SessionRunningTime > 30)
            {
                if (currentGameState.CarDamageData.OverallEngineDamage < DamageLevel.DESTROYED &&
                    !currentGameState.PitData.InPitlane &&
                    currentGameState.EngineData.EngineStalledWarning && 
                    currentGameState.Now > nextStalledCheck &&
                    currentGameState.PositionAndMotionData.CarSpeed < 5)
                {
                    // Play stalled warning straight away
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderStalled, 0, this));
                    // don't re-check stalled warning for a couple of minutes.
                    nextStalledCheck = currentGameState.Now.Add(TimeSpan.FromMinutes(2));
                    // move the oil and fuel pressure checks out a bit to allow it to settle
                    nextOilPressureCheck = currentGameState.Now.Add(TimeSpan.FromSeconds(20));
                    nextFuelPressureCheck = currentGameState.Now.Add(TimeSpan.FromSeconds(20));
                }
                else
                {
                    // don't check oil or fuel pressure if we're stalled
                    if (currentGameState.EngineData.EngineOilPressureWarning && currentGameState.Now > nextOilPressureCheck)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLowOilPressure, 0, this), 10);
                        // don't re-check oil pressure for a couple of minutes
                        nextOilPressureCheck = currentGameState.Now.Add(TimeSpan.FromMinutes(2));
                    }
                    if (currentGameState.EngineData.EngineFuelPressureWarning && currentGameState.Now > nextFuelPressureCheck)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLowFuelPressure, 0, this), 10);
                        // don't re-check fuel pressure for a couple of minutes
                        nextFuelPressureCheck = currentGameState.Now.Add(TimeSpan.FromMinutes(2));
                    }
                }
            }

            if (currentGameState.SessionData.SessionRunningTime > 60 * currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring)
            {
                engineData.addSample(currentGameState.EngineData.EngineOilTemp, currentGameState.EngineData.EngineWaterTemp, 
                    currentGameState.EngineData.EngineOilPressure,currentGameState.EngineData.EngineOilPressureWarning,
                    currentGameState.EngineData.EngineFuelPressureWarning,currentGameState.EngineData.EngineWaterTempWarning,
                    currentGameState.EngineData.EngineStalledWarning);
                // check temperatures every minute:
                if (currentGameState.SessionData.SessionRunningTime > gameTimeAtLastStatusCheck + statusMonitorWindowLength)
                {
                    EngineStatus currentEngineStatus = engineData.getEngineStatusFromAverage(maxSafeWaterTemp, maxSafeOilTemp, currentGameState.EngineData.EngineWaterTemp);
                    if (currentEngineStatus != lastStatusMessage)
                    {
                        if (currentEngineStatus.HasFlag(EngineStatus.ALL_CLEAR))
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playMessage(new QueuedMessage(folderAllClear, 0, this), 5);
                        }
                        else if (currentEngineStatus.HasFlag(EngineStatus.HOT_OIL) && currentEngineStatus.HasFlag(EngineStatus.HOT_WATER))
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playMessage(new QueuedMessage(folderHotOilAndWater, 0, this), 10);
                        }
                        if (currentEngineStatus.HasFlag(EngineStatus.HOT_OIL))
                        {
                            // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                            if (!lastStatusMessage.HasFlag(EngineStatus.HOT_OIL) && !lastStatusMessage.HasFlag(EngineStatus.HOT_WATER))
                            {
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.playMessage(new QueuedMessage(folderHotOil, 0, this), 10);
                            }
                        }
                        if (currentEngineStatus.HasFlag(EngineStatus.HOT_WATER))
                        {
                            // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                            if (!lastStatusMessage.HasFlag(EngineStatus.HOT_OIL) && !lastStatusMessage.HasFlag(EngineStatus.HOT_WATER))
                            {
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.playMessage(new QueuedMessage(folderHotWater, 0, this), 10);
                            }
                        }
                    }
                    gameTimeAtLastStatusCheck = currentGameState.SessionData.SessionRunningTime;
                    engineData = new EngineData();
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            Boolean fromStatusRequest = SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CAR_STATUS) ||
                                        SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS);
            Boolean gotData = false;
            if (engineData != null)
            {
                gotData = true;
                EngineStatus currentEngineStatus = engineData.getEngineStatusFromCurrent(maxSafeWaterTemp, maxSafeOilTemp);
                if (currentEngineStatus.HasFlag(EngineStatus.ALL_CLEAR))
                {
                    lastStatusMessage = currentEngineStatus;
                    if (!fromStatusRequest)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderAllClear, 0, null));
                    }
                }
                else if(currentEngineStatus.HasFlag(EngineStatus.HOT_OIL)  && currentEngineStatus.HasFlag(EngineStatus.HOT_WATER))
                {
                    lastStatusMessage = currentEngineStatus;
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderHotOilAndWater, 0, null));
                }
                else if (currentEngineStatus.HasFlag(EngineStatus.HOT_OIL))
                {
                    // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                    if (!lastStatusMessage.HasFlag(EngineStatus.HOT_OIL) && !lastStatusMessage.HasFlag(EngineStatus.HOT_WATER))
                    {
                        lastStatusMessage = currentEngineStatus;
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderHotOil, 0, null));
                    }
                }
                else if (currentEngineStatus.HasFlag(EngineStatus.HOT_WATER))
                {
                    // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                    if (!lastStatusMessage.HasFlag(EngineStatus.HOT_OIL) && !lastStatusMessage.HasFlag(EngineStatus.HOT_WATER))
                    {
                        lastStatusMessage = currentEngineStatus;
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderHotWater, 0, null));
                    }
                }
            }
            if (!gotData && !fromStatusRequest)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));                
            }
        }

        private class EngineData
        {
            private int samples;
            private float cumulativeOilTemp;
            private float cumulativeWaterTemp;
            private float cumulativeOilPressure;
            private float currentOilTemp;
            private float currentWaterTemp;
            private float currentOilPressure;
            private bool currentOilPressureWarning;
            private bool currentFuelPressureWarning;
            private bool currentWaterTempWarning;
            private bool currentEngineStalled;
            public EngineData()
            {
                this.samples = 0;
                this.cumulativeOilTemp = 0;
                this.cumulativeWaterTemp = 0;
                this.cumulativeOilPressure = 0;
                this.currentOilTemp = 0;
                this.currentWaterTemp = 0;
                this.currentOilPressure = 0;
                this.currentOilPressureWarning = false;
                this.currentFuelPressureWarning = false;
                this.currentWaterTempWarning = false;
                this.currentEngineStalled = false;

            }
            public void addSample(float engineOilTemp, float engineWaterTemp, float engineOilPressure, bool engineOilPressureWarning, bool engineFuelPressureWarning, bool engineWaterTempWarning, bool engineStalled)
            {
                this.samples++;
                this.cumulativeOilTemp += engineOilTemp;
                this.cumulativeWaterTemp += engineWaterTemp;
                this.cumulativeOilPressure += engineOilPressure;
                this.currentOilTemp = engineOilTemp;
                this.currentWaterTemp = engineWaterTemp;
                this.currentOilPressure = engineOilPressure;
                this.currentOilPressureWarning = engineOilPressureWarning;
                this.currentFuelPressureWarning = engineFuelPressureWarning;
                this.currentWaterTempWarning = engineWaterTempWarning;
                this.currentEngineStalled = engineStalled;
            }
            public EngineStatus getEngineStatusFromAverage(float maxSafeWaterTemp, float maxSafeOilTemp, float currentWaterTemp /*only used for logging*/)
            {
                EngineStatus engineStatusFlags = EngineStatus.NONE;
                if (samples > 10 && maxSafeOilTemp > 0 && maxSafeWaterTemp > 0)
                {
                    float averageOilTemp = cumulativeOilTemp / samples;
                    float averageWaterTemp = cumulativeWaterTemp / samples;
                    float averageOilPressure = cumulativeOilPressure / samples;
                    
                    if (averageWaterTemp > maxSafeWaterTemp)
                    {
                        Console.WriteLine(String.Format("Water temp {0} is above safe threshold of {1} for car class", averageWaterTemp, maxSafeWaterTemp));
                        engineStatusFlags = engineStatusFlags | EngineStatus.HOT_WATER;
                    }
                    if (averageOilTemp > maxSafeOilTemp)
                    {
                        Console.WriteLine(String.Format("Oil temp {0} is above safe threshold of {1} for car class", averageOilTemp, maxSafeOilTemp));
                        engineStatusFlags = engineStatusFlags | EngineStatus.HOT_OIL;
                    }
                }
                if (currentOilPressureWarning)
                {
                    engineStatusFlags = engineStatusFlags | EngineStatus.LOW_OIL_PRESSURE;
                }
                if (currentFuelPressureWarning)
                {
                    engineStatusFlags = engineStatusFlags | EngineStatus.LOW_FUEL_PRESSURE;
                }
                if (currentWaterTempWarning)
                {
                    Console.WriteLine(String.Format("Received water temp warning from game, current water temp is {0}", currentWaterTemp));
                    engineStatusFlags = engineStatusFlags | EngineStatus.HOT_WATER;
                }
                if (currentEngineStalled)
                {
                    engineStatusFlags = engineStatusFlags | EngineStatus.ENGINE_STALLED;
                }
                if (engineStatusFlags != EngineStatus.NONE)
                {
                    return engineStatusFlags;
                }
                return EngineStatus.ALL_CLEAR;
                // low oil pressure not implemented
            }
            public EngineStatus getEngineStatusFromCurrent(float maxSafeWaterTemp, float maxSafeOilTemp)
            {
                EngineStatus engineStatusFlags = EngineStatus.NONE;
                if (maxSafeOilTemp > 0 && maxSafeWaterTemp > 0)
                {
                    if (currentWaterTemp > maxSafeWaterTemp)
                    {
                        engineStatusFlags = engineStatusFlags | EngineStatus.HOT_WATER;
                    }
                    if (currentOilTemp > maxSafeOilTemp)
                    {
                        engineStatusFlags = engineStatusFlags | EngineStatus.HOT_OIL;
                    }
                }
                if (currentOilPressureWarning)
                {
                    engineStatusFlags = engineStatusFlags | EngineStatus.LOW_OIL_PRESSURE;
                }
                if (currentFuelPressureWarning)
                {
                    engineStatusFlags = engineStatusFlags | EngineStatus.LOW_FUEL_PRESSURE;
                }
                if (currentWaterTempWarning)
                {
                    engineStatusFlags = engineStatusFlags | EngineStatus.HOT_WATER;
                }
                if (currentEngineStalled)
                {
                    engineStatusFlags = engineStatusFlags | EngineStatus.ENGINE_STALLED;
                }
                if(engineStatusFlags != EngineStatus.NONE)
                {
                    return engineStatusFlags;
                }
                return EngineStatus.ALL_CLEAR;
            }
        }
        [Flags]
        private enum EngineStatus : uint
        {
            NONE = 0x0,
            ALL_CLEAR = 0x1, 
            HOT_OIL = 0x2,
            HOT_WATER = 0x4,
            LOW_OIL_PRESSURE = 0x8,
            LOW_FUEL_PRESSURE = 0x10,
            ENGINE_STALLED = 0x20,
        }
    }
}
