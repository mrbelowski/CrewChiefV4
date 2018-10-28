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
                        audioPlayer.playMessage(new QueuedMessage("gap_in_front" + index, 0,
                                        messageFragments: MessageContents(Timings.folderTheGapTo, driverToTest, Timings.folderAheadIsIncreasing,
                                        TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)),
                                        alternateMessageFragments: MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds((float)Utilities.random.NextDouble() * 10)),
                                        abstractEvent: this));
                        audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);

                        audioPlayer.playMessage(new QueuedMessage("leader_pitting" + index, 0,
                                        messageFragments: MessageContents(Opponents.folderTheLeader, driverToTest, Opponents.folderIsPitting), 
                                        abstractEvent: this));
                        audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);

                        audioPlayer.playMessage(new QueuedMessage("new_fastest_lap" + index, 0,
                                        messageFragments: MessageContents(Opponents.folderNewFastestLapFor, driverToTest, TimeSpan.FromSeconds(Utilities.random.NextDouble() * 100)),
                                        abstractEvent: this));
                        audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);

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
            if (AudioPlayer.folderChiefRadioCheck != null)
            {
                audioPlayer.playSpotterMessage(new QueuedMessage(SMOKE_TEST, 0, messageFragments: MessageContents(AudioPlayer.folderChiefRadioCheck)), false);
            }
            if (NoisyCartesianCoordinateSpotter.folderSpotterRadioCheck != null
                && !String.Equals(UserSettings.GetUserSettings().getString("spotter_name"), UserSettings.GetUserSettings().getString("chief_name"), StringComparison.InvariantCultureIgnoreCase))  // Don't play this if spotter and chief are the same person.
            {
                Thread.Sleep(800);
                audioPlayer.playSpotterMessage(new QueuedMessage(SMOKE_TEST_SPOTTER, 0,
                    messageFragments: MessageContents(NoisyCartesianCoordinateSpotter.folderSpotterRadioCheck)), false);
            }

            PlaybackModerator.SetTracing(true /*enabled*/);

            DirectoryInfo soundDirectory = new DirectoryInfo(AudioPlayer.soundFilesPath);
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "read_number_tests.txt")
                {
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("int" + i, 0, messageFragments: MessageContents(Utilities.random.Next(3100)), abstractEvent: this));
                        audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, 0,
                            messageFragments: MessageContents(TimeSpan.FromSeconds(Utilities.random.Next(4000) + ((float)Utilities.random.Next(9) / 10f))), abstractEvent: this));
                        audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, 0,
                            messageFragments: MessageContents(TimeSpan.FromSeconds(Utilities.random.Next(60) + ((float)Utilities.random.Next(9) / 10f))), abstractEvent: this));
                        audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        audioPlayer.playMessage(new QueuedMessage("time" + i, 0,
                            messageFragments: MessageContents(TimeSpan.FromSeconds(Utilities.random.NextDouble())), abstractEvent: this));
                        audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);
                    }
                    break;
                }
            }
            testDriverNames();
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
                    audioPlayer.playMessageImmediately(new QueuedMessage(messageName, 0, messageFragments: fragments, abstractEvent: this, 
                        type: SoundType.IMPORTANT_MESSAGE, priority: 0));
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
            audioPlayer.playMessageImmediately(new QueuedMessage(messageName, 0, messageFragments: fragments, abstractEvent: this,
                type: SoundType.IMPORTANT_MESSAGE, priority: 0));
            return true;
        }

        private void beepOutInTest()
        {
            PlaybackModerator.SetTracing(true /*enabled*/);

            QueuedMessage inTheMiddleMessage = new QueuedMessage("spotter/in_the_middle", 2);
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);

            Thread.Sleep(5000);

            audioPlayer.playMessage(new QueuedMessage("position/bad_start", 0, abstractEvent: this));
            audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);

            Thread.Sleep(5000);
            inTheMiddleMessage = new QueuedMessage("spotter/in_the_middle", 2);
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);

            return; 
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
            audioPlayer.playMessage(new QueuedMessage("check", 0, messageFragments: fragments, abstractEvent: this));
            audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);

            fragments = new List<MessageFragment>();
            fragments.Add(MessageFragment.Text(Strategy.folderClearTrackOnPitExit));
            fragments.Add(MessageFragment.Text(Strategy.folderWeShouldEmergeInPosition));
            fragments.Add(MessageFragment.Integer(12));
            fragments.Add(MessageFragment.Text(Strategy.folderBetween));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("bakus", rawDriverNames)));
            fragments.Add(MessageFragment.Text(Strategy.folderAnd));
            fragments.Add(MessageFragment.Opponent(makeTempDriver("fillingham", rawDriverNames)));
            audioPlayer.playMessage(new QueuedMessage("check", 0, messageFragments: fragments, abstractEvent: this));
            audioPlayer.wakeMonitorThreadForRegularMessages(DateTime.UtcNow);

            Thread.Sleep(2500);
            audioPlayer.playMessageImmediately(new QueuedMessage(NoisyCartesianCoordinateSpotter.folderEnableSpotter, 0));

            QueuedMessage inTheMiddleMessage = new QueuedMessage("spotter/in_the_middle", 0);
            //inTheMiddleMessage.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 2000;
            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);
        }
    }
}
