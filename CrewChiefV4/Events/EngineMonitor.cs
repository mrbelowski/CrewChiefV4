﻿using System;
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
        
        EngineStatus lastStatusMessage;

        EngineData engineData;

        float maxSafeOilTemp = 0;
        float maxSafeWaterTemp = 0;

        // record engine data for 60 seconds then report changes
        double statusMonitorWindowLength = 60;

        double gameTimeAtLastStatusCheck;

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
            if (currentGameState.SessionData.SessionRunningTime > 60 * currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring)
            {
                engineData.addSample(currentGameState.EngineData.EngineOilTemp, currentGameState.EngineData.EngineWaterTemp, 
                    currentGameState.EngineData.EngineOilPressure,currentGameState.EngineData.EngineOilPressureWarning,
                    currentGameState.EngineData.EngineFuelPressureWarning,currentGameState.EngineData.EngineWaterTempWarning);

                if (currentGameState.SessionData.SessionRunningTime > gameTimeAtLastStatusCheck + statusMonitorWindowLength)
                {
                    EngineStatus currentEngineStatus = engineData.getEngineStatusFromAverage(maxSafeWaterTemp, maxSafeOilTemp);
                    if (currentEngineStatus != lastStatusMessage)
                    {
                        if (currentEngineStatus.HasFlag(EngineStatus.ALL_CLEAR))
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playMessage(new QueuedMessage(folderAllClear, 0, this));
                        }
                        else if (currentEngineStatus.HasFlag(EngineStatus.HOT_OIL) && currentEngineStatus.HasFlag(EngineStatus.HOT_WATER))
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playMessage(new QueuedMessage(folderHotOilAndWater, 0, this));
                        }
                        else if (currentEngineStatus.HasFlag(EngineStatus.HOT_OIL))
                        {
                            // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                            if (!lastStatusMessage.HasFlag(EngineStatus.HOT_OIL) && !lastStatusMessage.HasFlag(EngineStatus.HOT_WATER))
                            {
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.playMessage(new QueuedMessage(folderHotOil, 0, this));
                            }
                        }
                        else if (currentEngineStatus.HasFlag(EngineStatus.HOT_WATER))
                        {
                            // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                            if (!lastStatusMessage.HasFlag(EngineStatus.HOT_OIL) && !lastStatusMessage.HasFlag(EngineStatus.HOT_WATER))
                            {
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.playMessage(new QueuedMessage(folderHotWater, 0, this));
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
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this));                
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
            }
            public void addSample(float engineOilTemp, float engineWaterTemp, float engineOilPressure, bool engineOilPressureWarning, bool engineFuelPressureWarning, bool engineWaterTempWarning)
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
            }
            public EngineStatus getEngineStatusFromAverage(float maxSafeWaterTemp, float maxSafeOilTemp)
            {
                // TODO: detect a sudden drop in oil pressure without triggering false positives caused by stalling the engine
                EngineStatus engineStatusFlags = EngineStatus.NONE;
                if (samples > 10 && maxSafeOilTemp > 0 && maxSafeWaterTemp > 0)
                {
                    float averageOilTemp = cumulativeOilTemp / samples;
                    float averageWaterTemp = cumulativeWaterTemp / samples;
                    float averageOilPressure = cumulativeOilPressure / samples;
                    
                    if (averageWaterTemp > maxSafeWaterTemp)
                    {
                        engineStatusFlags = engineStatusFlags | EngineStatus.HOT_WATER;
                    }
                    if (averageOilTemp > maxSafeOilTemp)
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
                if(engineStatusFlags != EngineStatus.NONE)
                {
                    return engineStatusFlags;
                }
                return EngineStatus.ALL_CLEAR;
            }
        }
        [Flags]
        private enum EngineStatus
        {
            NONE = 0x0,
            ALL_CLEAR = 0x1, 
            HOT_OIL = 0x2,
            HOT_WATER = 0x4,
            LOW_OIL_PRESSURE = 0x8,
            LOW_FUEL_PRESSURE = 0x10,
        }
    }
}
