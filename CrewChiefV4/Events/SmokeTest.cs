using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.GameState;
using System.IO;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

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
                List<String> usableDriverNames = DriverNameHelper.getUsableDriverNames(rawDriverNames);
                int index = 0;
                foreach (OpponentData driverToTest in driversToTest)
                {
                    if (SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(driverToTest.DriverRawName)))
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
            //audioPlayer.playMessage(new QueuedMessage("sectortest1", LapTimes.getSectorDeltaMessages(LapTimes.SectorReportOption.ALL, 20.5f, 20, 33, 34.1f, 10, 10.1f, true), 0, this));

            /*for (int i = 0; i < 5; i++)
            {
                audioPlayer.playMessage(new QueuedMessage("timingtest" +i, MessageContents(TimeSpanWrapper.FromSeconds(random.Next(100) + ((float)random.Next(99) / 100f), Precision.AUTO_LAPTIMES)), 0, this));
            }*/
            
            /*audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderDrizzleIncreasing, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderRainLightIncreasing, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderRainMidIncreasing, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderRainHeavyIncreasing, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderRainMax, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderRainHeavyDecreasing, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderRainMidDecreasing, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderRainLightDecreasing, 0, this));
            audioPlayer.playMessage(new QueuedMessage(ConditionsMonitor.folderDrizzleDecreasing, 0, this));*/

            audioPlayer.playMessageImmediately(new QueuedMessage(folderTest, 0, this));
            
            /*audioPlayer.playMessage(new QueuedMessage("gap_in_front",
                                        MessageContents(Timings.folderTheGapTo, makeTempDriver("7908jimmy6^&^", new List<string>()), Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)random.NextDouble() * 10)), 0, this));
            */
            DirectoryInfo soundDirectory = new DirectoryInfo(AudioPlayer.soundFilesPath);
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "read_number_tests.txt")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("int" + i, MessageContents(random.Next(3100)), 0, this));
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, MessageContents(TimeSpan.FromSeconds(random.Next(4000) + ((float)random.Next(9) / 10f))), 0, this));
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, MessageContents(TimeSpan.FromSeconds(random.Next(60) + ((float)random.Next(9) / 10f))), 0, this));
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, MessageContents(TimeSpan.FromSeconds(random.NextDouble())), 0, this));
                    }
                    break;
                }
            }            

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
