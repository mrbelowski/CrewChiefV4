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
                    currentGameState.EngineData.EngineOilPressure);

                if (currentGameState.SessionData.SessionRunningTime > gameTimeAtLastStatusCheck + statusMonitorWindowLength)
                {
                    EngineStatus currentEngineStatus = engineData.getEngineStatusFromAverage(maxSafeWaterTemp, maxSafeOilTemp);
                    if (currentEngineStatus != lastStatusMessage)
                    {
                        switch (currentEngineStatus)
                        {
                            case EngineStatus.ALL_CLEAR:
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.queueClip(new QueuedMessage(folderAllClear, 0, this));
                                break;
                            case EngineStatus.HOT_OIL:
                                // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                                if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                                {
                                    lastStatusMessage = currentEngineStatus;
                                    audioPlayer.queueClip(new QueuedMessage(folderHotOil, 0, this));
                                }
                                break;
                            case EngineStatus.HOT_WATER:
                                // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                                if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                                {
                                    lastStatusMessage = currentEngineStatus;
                                    audioPlayer.queueClip(new QueuedMessage(folderHotWater, 0, this));
                                }
                                break;
                            case EngineStatus.HOT_OIL_AND_WATER:
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.queueClip(new QueuedMessage(folderHotOilAndWater, 0, this));
                                break;
                        }
                    }
                    gameTimeAtLastStatusCheck = currentGameState.SessionData.SessionRunningTime;
                    engineData = new EngineData();
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            Boolean gotData = false;
            if (engineData != null)
            {
                gotData = true;
                EngineStatus currentEngineStatus = engineData.getEngineStatusFromCurrent(maxSafeWaterTemp, maxSafeOilTemp);
                switch (currentEngineStatus)
                {
                    case EngineStatus.ALL_CLEAR:
                        lastStatusMessage = currentEngineStatus;
                        audioPlayer.playClipImmediately(new QueuedMessage(folderAllClear, 0, null), false);
                        break;
                    case EngineStatus.HOT_OIL:
                        // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                        if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playClipImmediately(new QueuedMessage(folderHotOil, 0, null), false);
                        }
                        break;
                    case EngineStatus.HOT_WATER:
                        // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                        if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playClipImmediately(new QueuedMessage(folderHotWater, 0, null), false);
                        }
                        break;
                    case EngineStatus.HOT_OIL_AND_WATER:
                        lastStatusMessage = currentEngineStatus;
                        audioPlayer.playClipImmediately(new QueuedMessage(folderHotOilAndWater, 0, null), false);
                        break;
                }
                audioPlayer.closeChannel();
            }
            if (!gotData)
            {
                audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                audioPlayer.closeChannel();
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
            public EngineData()
            {
                this.samples = 0;
                this.cumulativeOilTemp = 0;
                this.cumulativeWaterTemp = 0;
                this.cumulativeOilPressure = 0;
                this.currentOilTemp = 0;
                this.currentWaterTemp = 0;
                this.currentOilPressure = 0;
            }
            public void addSample(float engineOilTemp, float engineWaterTemp, float engineOilPressure)
            {
                this.samples++;
                this.cumulativeOilTemp += engineOilTemp;
                this.cumulativeWaterTemp += engineWaterTemp;
                this.cumulativeOilPressure += engineOilPressure;
                this.currentOilTemp = engineOilTemp;
                this.currentWaterTemp = engineWaterTemp;
                this.currentOilPressure = engineOilPressure;
            }
            public EngineStatus getEngineStatusFromAverage(float maxSafeWaterTemp, float maxSafeOilTemp)
            {
                // TODO: detect a sudden drop in oil pressure without triggering false positives caused by stalling the engine
                if (samples > 10 && maxSafeOilTemp > 0 && maxSafeWaterTemp > 0)
                {
                    float averageOilTemp = cumulativeOilTemp / samples;
                    float averageWaterTemp = cumulativeWaterTemp / samples;
                    float averageOilPressure = cumulativeOilPressure / samples;
                    if (averageOilTemp > maxSafeOilTemp && averageWaterTemp > maxSafeWaterTemp)
                    {
                        return EngineStatus.HOT_OIL_AND_WATER;
                    }
                    else if (averageWaterTemp > maxSafeWaterTemp)
                    {
                        return EngineStatus.HOT_WATER;
                    }
                    else if (averageOilTemp > maxSafeOilTemp)
                    {
                        return EngineStatus.HOT_OIL;
                    }
                }                
                return EngineStatus.ALL_CLEAR;
                // low oil pressure not implemented
            }
            public EngineStatus getEngineStatusFromCurrent(float maxSafeWaterTemp, float maxSafeOilTemp)
            {
                if (maxSafeOilTemp > 0 && maxSafeWaterTemp > 0)
                {
                    if (currentOilTemp > maxSafeOilTemp && currentWaterTemp > maxSafeWaterTemp)
                    {
                        return EngineStatus.HOT_OIL_AND_WATER;
                    }
                    else if (currentWaterTemp > maxSafeWaterTemp)
                    {
                        return EngineStatus.HOT_WATER;
                    }
                    else if (currentOilTemp > maxSafeOilTemp)
                    {
                        return EngineStatus.HOT_OIL;
                    }
                }                
                return EngineStatus.ALL_CLEAR;
            }
        }

        private enum EngineStatus
        {
            ALL_CLEAR, HOT_OIL, HOT_WATER, HOT_OIL_AND_WATER
        }
    }
}
