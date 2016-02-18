using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.GameState;
using System.IO;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class SmokeTest : AbstractEvent
    {
        private String folderTest = "radio_check/test";

        private Random random = new Random();

        public SmokeTest(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            return true;
        }

        private OpponentData makeTempDriver(String driverName, List<String> rawDriverNames)
        {
            OpponentData opponent = new OpponentData();
            opponent.DriverRawName = driverName;
            rawDriverNames.Add(driverName);
            return opponent;
        }
        
        private void testDriverNames()
        {
            List<OpponentData> driversToTest = new List<OpponentData>();
            List<String> rawDriverNames = new List<string>();
            DirectoryInfo soundDirectory = new DirectoryInfo(AudioPlayer.soundFilesPath);
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "names_test.txt")
                {
                    String[] lines = File.ReadAllLines(Path.Combine(AudioPlayer.soundFilesPath, fileInSoundDirectory.Name));
                    foreach (String line in lines)
                    {
                        if (line.Trim().Length > 0)
                        {
                            driversToTest.Add(makeTempDriver(line.Trim(), rawDriverNames));
                        }
                    }
                    break;
                }
            }
            if (rawDriverNames.Count > 0)
            {
                Console.WriteLine("Playing test sounds for drivers " + String.Join(", ", rawDriverNames));
                List<String> usableDriverNames = DriverNameHelper.getUsableDriverNames(rawDriverNames, AudioPlayer.soundFilesPath);
                int index = 0;
                foreach (OpponentData driverToTest in driversToTest)
                {
                    if (SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableNameForRawName(driverToTest.DriverRawName)))
                    {
                        audioPlayer.playMessage(new QueuedMessage("gap_in_front" + index,
                                        MessageContents(Timings.folderTheGapTo, driverToTest, Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)random.NextDouble() * 10)), 0, this));
                        audioPlayer.playMessage(new QueuedMessage("leader_pitting" + index,
                            MessageContents(Opponents.folderTheLeader, driverToTest, Opponents.folderIsPitting), 0, this));
                        audioPlayer.playMessage(new QueuedMessage("new_fastest_lap" + index, MessageContents(Opponents.folderNewFastestLapFor, driverToTest,
                                            TimeSpan.FromSeconds(random.NextDouble() * 100)), 0, this));
                        index++;
                    }
                    else
                    {
                        Console.WriteLine("No sound file for driver " + driverToTest.DriverRawName + " - unable to play test sounds");
                    }
                }
            }
        }
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            audioPlayer.playMessage(new QueuedMessage("sectortest1", LapTimes.getSectorDeltaMessages(LapTimes.SectorReportOption.ALL, 20.1f, 20, 33, 32.1f, 10, 10.1f, true), 0, this));

            audioPlayer.playMessage(new QueuedMessage(folderTest, 0, this));
            /*audioPlayer.playMessage(new QueuedMessage("int1", MessageContents(143), 0, this));
            audioPlayer.playMessage(new QueuedMessage("int2", MessageContents(1), 0, this));
            audioPlayer.playMessage(new QueuedMessage("int3", MessageContents(1000), 0, this));
            audioPlayer.playMessage(new QueuedMessage("int4", MessageContents(2300), 0, this));
            audioPlayer.playMessage(new QueuedMessage("int5", MessageContents(401), 0, this));

            audioPlayer.playMessage(new QueuedMessage("time1", MessageContents(TimeSpan.FromMinutes(32.1)), 0, this));
            audioPlayer.playMessage(new QueuedMessage("time2", MessageContents(TimeSpan.FromSeconds(47.12)), 0, this));
            audioPlayer.playMessage(new QueuedMessage("time3", MessageContents(TimeSpan.FromSeconds(101)), 0, this));
            audioPlayer.playMessage(new QueuedMessage("time4", MessageContents(TimeSpan.FromSeconds(0.4)), 0, this));
            audioPlayer.playMessage(new QueuedMessage("time5", MessageContents(TimeSpan.FromMinutes(61)), 0, this));
            audioPlayer.playMessage(new QueuedMessage("time6", MessageContents(TimeSpan.FromSeconds(1.1)), 0, this));*/
            /*audioPlayer.playMessage(new QueuedMessage("gap test", MessageContents(LapTimes.folderGapIntro, TimeSpan.FromSeconds(3.1),
                LapTimes.folderGapOutroOffPace), 0, this));*/
            testDriverNames();
            
            /*
            audioPlayer.playMessage(new QueuedMessage(LapCounter.folderGetReady, 0, this));
            audioPlayer.playMessage(new QueuedMessage(MandatoryPitStops.folderMandatoryPitStopsPitThisLap, 0, this));
            audioPlayer.playMessage(new QueuedMessage(MandatoryPitStops.folderMandatoryPitStopsFitPrimesThisLap, 0, this));
            audioPlayer.playMessage(new QueuedMessage(Position.folderBeingOvertaken, 0, this));
            audioPlayer.playMessage(new QueuedMessage(Position.folderOvertaking, 0, this));
            audioPlayer.playMessage(new QueuedMessage(SessionEndMessages.folderFinishedRace, 0, this));
            audioPlayer.playMessage(new QueuedMessage(SessionEndMessages.folderFinishedRaceLast, 0, this));
            audioPlayer.playMessage(new QueuedMessage(SessionEndMessages.folderPodiumFinish, 0, this));
            
            audioPlayer.playMessage(new QueuedMessage(LapCounter.folderGetReady, 0, this));
            audioPlayer.playMessage(new QueuedMessage("rain1", MessageContents(
                                        ConditionsMonitor.folderSeeingSomeRain), 0, this));
            audioPlayer.playMessage(new QueuedMessage("rain2", MessageContents(
                                        ConditionsMonitor.folderStoppedRaining), 0, this));
            audioPlayer.playMessage(new QueuedMessage("pearl1", MessageContents(
                                                    PearlsOfWisdom.folderKeepItUp), 0, this));
            audioPlayer.playMessage(new QueuedMessage("pearl2", MessageContents(
                                                                PearlsOfWisdom.folderMustDoBetter), 0, this));
            audioPlayer.playMessage(new QueuedMessage("getReady", MessageContents(
                                                    LapCounter.folderGetReady), 0, this));

            audioPlayer.playMessage(new QueuedMessage("conditionsAirAndTrackIncreasing1", MessageContents
                               (ConditionsMonitor.folderAirAndTrackTempIncreasing, 
                               ConditionsMonitor.folderAirTempIsNow, 26, Pause(2000),
                               ConditionsMonitor.folderTrackTempIsNow, 32, ConditionsMonitor.folderCelsius), 0, this));
            audioPlayer.playMessage(new QueuedMessage("Fuel/estimate", MessageContents(
                                        Fuel.folderWeEstimate, 12, Fuel.folderMinutesRemaining), 0, this));
            audioPlayer.playMessage(new QueuedMessage("laptime", MessageContents(LapTimes.folderLapTimeIntro, 
                TimeSpan.FromSeconds(60 + (random.NextDouble() * 60))), 0, this));

            audioPlayer.playMessage(new QueuedMessage("yesBoxAfter", MessageContents(MandatoryPitStops.folderMandatoryPitStopsYesStopAfter,
                TimeSpan.FromMinutes(10)), 0, null));
            audioPlayer.playMessage(new QueuedMessage("laps_on_current_tyres", MessageContents(TyreMonitor.folderLapsOnCurrentTyresIntro,
                5, TyreMonitor.folderLapsOnCurrentTyresOutro), 0, this));*/

        }
    }
}
