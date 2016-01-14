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
                        audioPlayer.queueClip(new QueuedMessage("gap_in_front" + index,
                                        MessageContents(Timings.folderTheGapTo, driverToTest, Timings.folderAheadIsIncreasing,
                                        TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)), 0, this));
                        audioPlayer.queueClip(new QueuedMessage("leader_pitting" + index,
                            MessageContents(Opponents.folderTheLeader, driverToTest, Opponents.folderIsPitting), 0, this));
                        audioPlayer.queueClip(new QueuedMessage("new_fastest_lap" + index, MessageContents(Opponents.folderNewFastestLapFor, driverToTest,
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
            audioPlayer.queueClip(new QueuedMessage(folderTest, 0, this));
            testDriverNames();

            /*
            audioPlayer.queueClip(new QueuedMessage("conditionsAirAndTrackIncreasing1", MessageContents
                               (ConditionsMonitor.folderAirAndTrackTempIncreasing, 
                               ConditionsMonitor.folderAirTempIsNow, QueuedMessage.folderNameNumbersStub + 26,
                               ConditionsMonitor.folderTrackTempIsNow, QueuedMessage.folderNameNumbersStub + 32, ConditionsMonitor.folderCelsius), 0, this));
            audioPlayer.queueClip(new QueuedMessage("Fuel/estimate", MessageContents(
                                        Fuel.folderWeEstimate, QueuedMessage.folderNameNumbersStub + 12, Fuel.folderMinutesRemaining), 0, this));
            audioPlayer.queueClip(new QueuedMessage("laptime", MessageContents(LapTimes.folderLapTimeIntro, 
                TimeSpan.FromSeconds(60 + (random.NextDouble() * 60))), 0, this));

            audioPlayer.queueClip(new QueuedMessage("yesBoxAfter", MessageContents(MandatoryPitStops.folderMandatoryPitStopsYesStopAfter,
                QueuedMessage.folderNameNumbersStub + 10, MandatoryPitStops.folderMandatoryPitStopsMinutes), 0, null));
            audioPlayer.queueClip(new QueuedMessage("laps_on_current_tyres", MessageContents(TyreMonitor.folderLapsOnCurrentTyresIntro,
                QueuedMessage.folderNameNumbersStub + 5, TyreMonitor.folderLapsOnCurrentTyresOutro), 0, this));
            */
        }
    }
}
