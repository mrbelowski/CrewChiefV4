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
        public static String SMOKE_TEST = "smoke_test_chief";
        public static String SMOKE_TEST_SPOTTER = "smoke_test_spotter";

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

        public override Boolean isApplicableForCurrentSessionAndPhase(SessionType sessionType, SessionPhase sessionPhase)
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
                                        TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)), 0, this));
                        audioPlayer.playMessage(new QueuedMessage("leader_pitting" + index,
                            MessageContents(Opponents.folderTheLeader, driverToTest, Opponents.folderIsPitting), 0, this));
                        audioPlayer.playMessage(new QueuedMessage("new_fastest_lap" + index, MessageContents(Opponents.folderNewFastestLapFor, driverToTest,
                                            TimeSpan.FromSeconds(Utilities.random.NextDouble() * 100)), 0, this));
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
                audioPlayer.playMessage(new QueuedMessage("timingtest" +i, MessageContents(TimeSpanWrapper.FromSeconds(Utilities.random.Next(100) + ((float)random.Next(99) / 100f), Precision.AUTO_LAPTIMES)), 0, this));
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

            if (AudioPlayer.folderChiefRadioCheck != null)
            {
                audioPlayer.playSpotterMessage(new QueuedMessage(SMOKE_TEST, MessageContents(AudioPlayer.folderChiefRadioCheck), 0, null), false);
            }
            if (NoisyCartesianCoordinateSpotter.folderSpotterRadioCheck != null
                && !String.Equals(UserSettings.GetUserSettings().getString("spotter_name"), UserSettings.GetUserSettings().getString("chief_name"), StringComparison.InvariantCultureIgnoreCase))  // Don't play this if spotter and chief are the same person.
            {
                Thread.Sleep(800);
                audioPlayer.playSpotterMessage(new QueuedMessage(SMOKE_TEST_SPOTTER, MessageContents(NoisyCartesianCoordinateSpotter.folderSpotterRadioCheck), 0, null), false);
            }

            PlaybackModerator.SetTracing(true /*enabled*/);
            //this.BeepOutInTest();

            // pit exit strategy debug stuff to see how it sounds
            
            /*List<String> rawDriverNames = new List<string>();
            List<MessageFragment> fragments = new List<MessageFragment>();
            fragments.Add(MessageFragment.Text(Strategy.folderClearTrackOnPitExit));
            fragments.Add(MessageFragment.Text(Strategy.folderWeShouldEmergeInPosition));
            fragments.Add(MessageFragment.Integer(12));
            fragments.Add(MessageFragment.Text(Strategy.folderBetween));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("bakus", rawDriverNames)));
            fragments.Add(MessageFragment.Text(Strategy.folderAnd));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("fillingham", rawDriverNames)));
            audioPlayer.playMessage(new QueuedMessage("check", fragments, 0, this));
            Thread.Sleep(2000);
            audioPlayer.playMessageImmediately(new QueuedMessage(NoisyCartesianCoordinateSpotter.folderEnableSpotter, 0, null));
            */
            /*
            List<String> rawDriverNames = new List<string>();
            audioPlayer.playMessage(new QueuedMessage("opponent_exiting_behind", MessageContents(makeTempDriver("bakus", rawDriverNames),
                                        Strategy.folderIsPittingFromPosition, 12, Strategy.folderHeWillComeOutJustBehind), 0, this));
            */

            DirectoryInfo soundDirectory = new DirectoryInfo(AudioPlayer.soundFilesPath);
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "read_number_tests.txt")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("int" + i, MessageContents(Utilities.random.Next(3100)), 0, this));
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, MessageContents(TimeSpan.FromSeconds(Utilities.random.Next(4000) + ((float)Utilities.random.Next(9) / 10f))), 0, this));
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, MessageContents(TimeSpan.FromSeconds(Utilities.random.Next(60) + ((float)Utilities.random.Next(9) / 10f))), 0, this));
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, MessageContents(TimeSpan.FromSeconds(Utilities.random.NextDouble())), 0, this));
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
                TimeSpan.FromSeconds(60 + (Utilities.random.NextDouble() * 60))), 0, this));

            audioPlayer.playMessage(new QueuedMessage("yesBoxAfter", MessageContents(MandatoryPitStops.folderMandatoryPitStopsYesStopAfter,
                TimeSpan.FromMinutes(10)), 0, null));
            audioPlayer.playMessage(new QueuedMessage("laps_on_current_tyres", MessageContents(TyreMonitor.folderLapsOnCurrentTyresIntro,
                5, TyreMonitor.folderLapsOnCurrentTyresOutro), 0, this));*/

        }
        public bool soundTestPlay(String[] foldersOrStuff, int messageNumber = 1)
        {
            List<String> rawDriverNames = new List<string>();
            List<MessageFragment> fragments = new List<MessageFragment>();
            if (foldersOrStuff.Length > 0 && File.Exists(foldersOrStuff[0]))
            {
                foldersOrStuff = File.ReadAllLines(foldersOrStuff[0]);
            }
            int iter = 0;
            String messageName = "";
            foreach(String stuffToPlay in foldersOrStuff)            
            {
                messageName = "sound_test_message_" + messageNumber.ToString();
                iter++;
                int num;
                bool isNumeric = int.TryParse(stuffToPlay, out num);
                if(stuffToPlay.StartsWith("#") || stuffToPlay.Length < 1)
                { 
                    continue;
                }
                if (stuffToPlay.StartsWith("&"))
                {                    
                    String [] nextNessage = new String [foldersOrStuff.Length - iter];
                    Array.Copy(foldersOrStuff, iter, nextNessage, 0, foldersOrStuff.Length - iter);
                    audioPlayer.playMessageImmediately(new QueuedMessage(messageName, fragments, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                    messageNumber++;
                    return soundTestPlay(nextNessage, messageNumber);
                }
                
                if (isNumeric)
                {
                    fragments.Add(MessageFragment.Integer(num));
                }
                else if (stuffToPlay.ToLower().StartsWith("pause"))
                {                    
                    String pauseLength = stuffToPlay.Substring(6);
                    isNumeric = int.TryParse(pauseLength, out num);
                    if(isNumeric)
                    {
                        fragments.Add(MessageFragment.Text(Pause(num)));
                    }                    
                }
                else if (stuffToPlay.ToLower().StartsWith("name"))
                {
                    String nameToMake = stuffToPlay.Substring(5);
                    fragments.Add(MessageFragment.Opponent(makeTempDriver(nameToMake, rawDriverNames)));
                }
                else if (stuffToPlay.ToLower().StartsWith("time"))
                {
                    float time = 0;
                    String timeToMake = "";
                    Precision precision = Precision.AUTO_LAPTIMES;
                    timeToMake = stuffToPlay.Substring(9);
                    isNumeric = float.TryParse(timeToMake, out time);
                    if (isNumeric)
                    {
                        if (stuffToPlay.ToLower().Contains("lap"))
                        {
                            precision = Precision.AUTO_LAPTIMES;
                        }
                        else if (stuffToPlay.ToLower().Contains("gap"))
                        {
                            precision = Precision.AUTO_GAPS;
                        }
                        else if (stuffToPlay.ToLower().Contains("hun"))
                        {
                            precision = Precision.HUNDREDTHS;
                        }
                        else if (stuffToPlay.ToLower().Contains("sec"))
                        {
                            precision = Precision.SECONDS;
                        }
                        else if (stuffToPlay.ToLower().Contains("ten"))
                        {
                            precision = Precision.TENTHS;
                        }
                        fragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(time, precision)));       
                    }                  
                }
                else
                {
                    fragments.Add(MessageFragment.Text(stuffToPlay));
                }

            }
            audioPlayer.playMessageImmediately(new QueuedMessage(messageName, fragments, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
            return true;
        }

        private void beepOutInTest()
        {
            PlaybackModerator.SetTracing(true /*enabled*/);

            QueuedMessage inTheMiddleMessage = new QueuedMessage("spotter/in_the_middle", 0, null);
            inTheMiddleMessage.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 2000;
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);

            Thread.Sleep(5000);

            /*audioPlayer.playMessage(new QueuedMessage("gap_in_front",
                                        MessageContents(Timings.folderTheGapTo, makeTempDriver("7908jimmy6^&^", new List<string>()), Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)random.NextDouble() * 10)), 0, this));*/

            audioPlayer.playMessage(new QueuedMessage("position/bad_start", 0, this));

            Thread.Sleep(5000);
            inTheMiddleMessage = new QueuedMessage("spotter/in_the_middle", 0, null);
            inTheMiddleMessage.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 2000;
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);

            return; 
            inTheMiddleMessage = new QueuedMessage("spotter/car_right", 0, null);
            inTheMiddleMessage.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 2000;
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);

            audioPlayer.playMessage(new QueuedMessage("gap_in_front2",
                                        MessageContents(Timings.folderTheGapTo, makeTempDriver("7908jimmy6^&^", new List<string>()), Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)), 0, this));

            

            audioPlayer.playMessage(new QueuedMessage("gap_in_front4",
                                        MessageContents(Timings.folderTheGapTo, makeTempDriver("7908jimmy6^&^", new List<string>()), Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)), 0, this));
            Thread.Sleep(8000);
            inTheMiddleMessage = new QueuedMessage("spotter/car_right", 0, null);
            inTheMiddleMessage.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 20000;
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);

            Thread.Sleep(2000);
            audioPlayer.playMessage(new QueuedMessage("gap_in_front3",
                                        MessageContents(Timings.folderTheGapTo, makeTempDriver("7908jimmy6^&^", new List<string>()), Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)), 0, this));


            Thread.Sleep(4000);
            inTheMiddleMessage = new QueuedMessage("spotter/car_right", 0, null);
            inTheMiddleMessage.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 20000;
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);

            Thread.Sleep(5000);
            audioPlayer.playMessage(new QueuedMessage("gap_in_front4",
                                        MessageContents(Timings.folderTheGapTo, makeTempDriver("7908jimmy6^&^", new List<string>()), Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)),
                                        MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)), 0, this));
        }

        private void messageInterruptTest()
        {
            PlaybackModerator.SetTracing(true /*enabled*/);

            List<String> rawDriverNames = new List<string>();
            List<MessageFragment> fragments = new List<MessageFragment>();
            fragments.Add(MessageFragment.Text(Strategy.folderClearTrackOnPitExit));
            fragments.Add(MessageFragment.Text(Strategy.folderWeShouldEmergeInPosition));
            fragments.Add(MessageFragment.Integer(12));
            fragments.Add(MessageFragment.Text(Strategy.folderBetween));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("bakus", rawDriverNames)));
            fragments.Add(MessageFragment.Text(Strategy.folderAnd));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("fillingham", rawDriverNames)));
            audioPlayer.playMessage(new QueuedMessage("check", fragments, 0, this));
            fragments = new List<MessageFragment>();
            fragments.Add(MessageFragment.Text(Strategy.folderClearTrackOnPitExit));
            fragments.Add(MessageFragment.Text(Strategy.folderWeShouldEmergeInPosition));
            fragments.Add(MessageFragment.Integer(12));
            fragments.Add(MessageFragment.Text(Strategy.folderBetween));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("bakus", rawDriverNames)));
            fragments.Add(MessageFragment.Text(Strategy.folderAnd));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("fillingham", rawDriverNames)));
            audioPlayer.playMessage(new QueuedMessage("check", fragments, 0, this));
            Thread.Sleep(2500);
            audioPlayer.playMessageImmediately(new QueuedMessage(NoisyCartesianCoordinateSpotter.folderEnableSpotter, 0, null));

            QueuedMessage inTheMiddleMessage = new QueuedMessage("spotter/in_the_middle", 0, null);
            //inTheMiddleMessage.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 2000;
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);
        }
    }
}
