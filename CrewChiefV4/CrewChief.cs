using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CrewChiefV4.RaceRoom;
using CrewChiefV4.Events;
using System.Collections.Generic;
using CrewChiefV4.GameState;
using CrewChiefV4.PCars;
using CrewChiefV4.RaceRoom.RaceRoomData;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;


namespace CrewChiefV4
{
    public class CrewChief : IDisposable
    {
        public static Boolean Debugging = System.Diagnostics.Debugger.IsAttached;

        // speechRecognizer and audioPlayer are shared by many threads.  They should be disposed after root threads stopped, in GlobalResources.Dispose.
        public SpeechRecogniser speechRecogniser;
        public AudioPlayer audioPlayer;

        readonly int timeBetweenProcConnectCheckMillis = 500;
        readonly int timeBetweenProcDisconnectCheckMillis = 2000;
        readonly int maxEventFailuresBeforeDisabling = 10;
        DateTime nextProcessStateCheck = DateTime.MinValue;
        bool isGameProcessRunning = false;

        public static Boolean loadDataFromFile = false;
        public static GameDefinition gameDefinition;

        public static Boolean readOpponentDeltasForEveryLap = false;
        // initial state from properties but can be overridden during a session:
        public static Boolean yellowFlagMessagesEnabled = UserSettings.GetUserSettings().getBoolean("enable_yellow_flag_messages");
        private static Boolean useVerboseResponses = UserSettings.GetUserSettings().getBoolean("use_verbose_responses");
        private Boolean keepQuietEnabled = false;
                
        public static Boolean enableDriverNames = UserSettings.GetUserSettings().getBoolean("enable_driver_names");

        private static int timeInterval = UserSettings.GetUserSettings().getInt("update_interval");

        private static int spotterInterval = UserSettings.GetUserSettings().getInt("spotter_update_interval");

        private Boolean displaySessionLapTimes = UserSettings.GetUserSettings().getBoolean("display_session_lap_times");

        public static Boolean forceSingleClass = UserSettings.GetUserSettings().getBoolean("force_single_class");
        public static int maxUnknownClassesForAC = UserSettings.GetUserSettings().getInt("max_unknown_car_classes_for_assetto");

        private static Dictionary<String, AbstractEvent> eventsList = new Dictionary<String, AbstractEvent>();


        Object lastSpotterState;
        Object currentSpotterState;

        Boolean stateCleared = false;

        public Boolean running = false;

        // This value is set to false when we re-create main run thread, and is set to true
        // once we get past file loading phase (which can be lenghty).
        public Boolean dataFileReadDone = false;
        public Boolean dataFileDumpDone = false;

        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        private Dictionary<String, String> faultingEvents = new Dictionary<String, String>();
        
        private Dictionary<String, int> faultingEventsCount = new Dictionary<String, int>();

        private Boolean sessionHasFailingEvent = false;

        private Spotter spotter;

        private Boolean spotterIsRunning = false;

        private Boolean runSpotterThread = false;

        private Thread spotterThread = null;

        private GameDataReader gameDataReader;

        // hmm....
        public static GameStateData currentGameState = null;

        public GameStateData previousGameState = null;

        private Boolean mapped = false;

        private SessionEndMessages sessionEndMessages;

        public AlarmClock alarmClock;
        // used for the pace notes recorder - need to separate out from the currentGameState so we can
        // set these even when viewing replays
        public static String trackName = "";
        public static int raceroomTrackId = -1;
        public static CarData.CarClassEnum carClass = CarData.CarClassEnum.UNKNOWN_RACE;
        public static Boolean viewingReplay = false;
        public static float distanceRoundTrack = -1;

        public static int playbackIntervalMilliseconds = 0;

        private Object latestRawGameData;

        public CrewChief()
        {
            speechRecogniser = new SpeechRecogniser(this);
            audioPlayer = new AudioPlayer();

            GlobalResources.speechRecogniser = speechRecogniser;
            GlobalResources.audioPlayer = audioPlayer;

            audioPlayer.initialise();
            eventsList.Add("Timings", new Timings(audioPlayer));
            eventsList.Add("Position", new Position(audioPlayer));
            eventsList.Add("LapCounter", new LapCounter(audioPlayer, this));
            eventsList.Add("LapTimes", new LapTimes(audioPlayer));
            eventsList.Add("Penalties", new Penalties(audioPlayer));
            eventsList.Add("PitStops", new PitStops(audioPlayer));
            eventsList.Add("Fuel", new Fuel(audioPlayer));
            eventsList.Add("Battery", new Battery(audioPlayer));
            eventsList.Add("Strategy", new Strategy(audioPlayer));
            eventsList.Add("Opponents", new Opponents(audioPlayer));
            eventsList.Add("RaceTime", new RaceTime(audioPlayer));
            eventsList.Add("TyreMonitor", new TyreMonitor(audioPlayer));
            eventsList.Add("EngineMonitor", new EngineMonitor(audioPlayer));
            eventsList.Add("DamageReporting", new DamageReporting(audioPlayer));
            eventsList.Add("PushNow", new PushNow(audioPlayer));
            eventsList.Add("FlagsMonitor", new FlagsMonitor(audioPlayer));
            eventsList.Add("ConditionsMonitor", new ConditionsMonitor(audioPlayer));
            eventsList.Add("OvertakingAidsMonitor", new OvertakingAidsMonitor(audioPlayer));
            eventsList.Add("FrozenOrderMonitor", new FrozenOrderMonitor(audioPlayer));
            eventsList.Add("IRacingBroadcastMessageEvent", new IRacingBroadcastMessageEvent(audioPlayer));
            eventsList.Add("MulticlassWarnings", new MulticlassWarnings(audioPlayer));            
            sessionEndMessages = new SessionEndMessages(audioPlayer);
            alarmClock = new AlarmClock(audioPlayer);
            DriverNameHelper.readRawNamesToUsableNamesFiles(AudioPlayer.soundFilesPath);
        }

        public void setGameDefinition(GameDefinition gameDefinition)
        {
            spotter = null;
            mapped = false;
            if (gameDefinition == null)
            {
                Console.WriteLine("No game definition selected");
            }
            else
            {
                Console.WriteLine("Using game definition " + gameDefinition.friendlyName);
                UserSettings.GetUserSettings().setProperty("last_game_definition", gameDefinition.gameEnum.ToString());
                UserSettings.GetUserSettings().saveUserSettings();
                CrewChief.gameDefinition = gameDefinition;
                //I think we shuld add it here 
                if (gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || 
                    gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT ||
                    gameDefinition.gameEnum == GameEnum.RF1 ||
                    gameDefinition.gameEnum == GameEnum.RF2_64BIT)
                {
                    PluginInstaller pluginInstaller = new PluginInstaller();
                    pluginInstaller.InstallOrUpdatePlugins(gameDefinition);
                }
            }
        }

        public void Dispose()
        {
        }

        public static AbstractEvent getEvent(String eventName)
        {
            AbstractEvent abstractEvent = null;
            if (eventsList.TryGetValue(eventName, out abstractEvent))
            {
                return abstractEvent;
            }

            return null;
        }

        public void toggleKeepQuietMode()
        {
            if (keepQuietEnabled) 
            {
                disableKeepQuietMode();
            }
            else
            {
                enableKeepQuietMode();
            }
        }

        public void toggleDelayMessagesInHardParts()
        {
            if (AudioPlayer.delayMessagesInHardParts)
            {
                disableDelayMessagesInHardParts();
            }
            else
            {
                enableDelayMessagesInHardParts();
            }
        }

        public void enableDelayMessagesInHardParts()
        {
            if (!AudioPlayer.delayMessagesInHardParts)
            {
                AudioPlayer.delayMessagesInHardParts = true;
            }
            // switch the gap points to use the adjusted ones
            if (currentGameState != null && currentGameState.SessionData.TrackDefinition != null && currentGameState.hardPartsOnTrackData.hardPartsMapped)
            {
                currentGameState.SessionData.TrackDefinition.adjustGapPoints(currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap);
            }
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowledgeEnableDelayInHardParts, 0, null));
        }

        public void disableDelayMessagesInHardParts()
        {
            if (AudioPlayer.delayMessagesInHardParts)
            {
                AudioPlayer.delayMessagesInHardParts = false;
            }
            // switch the gap points back to use the regular ones
            if (currentGameState != null && currentGameState.SessionData.TrackDefinition != null && currentGameState.hardPartsOnTrackData.hardPartsMapped)
            {
                currentGameState.SessionData.TrackDefinition.setGapPoints();
            }
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowledgeDisableDelayInHardParts, 0, null));
        }

        public void toggleReadOpponentDeltasMode()
        {
            if (readOpponentDeltasForEveryLap)
            {
                disableDeltasMode();
            }
            else
            {
                enableDeltasMode();
            }
        }

        public void enableDeltasMode()
        {
            if (!readOpponentDeltasForEveryLap)
            {
                readOpponentDeltasForEveryLap = true;
            }
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDeltasEnabled, 0, null));
        }

        public void disableDeltasMode()
        {
            if (readOpponentDeltasForEveryLap)
            {
                readOpponentDeltasForEveryLap = false;
            }
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDeltasDisabled, 0, null));
        }

        public void toggleEnableYellowFlagsMode()
        {
            if (yellowFlagMessagesEnabled)
            {
                disableYellowFlagMessages();
            }
            else
            {
                enableYellowFlagMessages();
            }
        }

        public void enableYellowFlagMessages()
        {
            yellowFlagMessagesEnabled = true;
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderYellowEnabled, 0, null));
        }

        public void disableYellowFlagMessages()
        {
            yellowFlagMessagesEnabled = false;
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderYellowDisabled, 0, null));
        }

        public void toggleManualFormationLapMode()
        {
            if (GameStateData.useManualFormationLap)
            {
                disableManualFormationLapMode();
            }
            else
            {
                enableManualFormationLapMode();
            }
        }

        public void enableManualFormationLapMode()
        {
            // Prevent accidential trigger during the race.  Luckily, there's a handy hack available :)
            if (currentGameState != null && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.CompletedLaps >=1)
            {
                Console.WriteLine("Rejecting manual formation lap request due to race already in progress");
                return;
            }
            if (!GameStateData.useManualFormationLap)
            {
                GameStateData.useManualFormationLap = true;
                GameStateData.onManualFormationLap = true;
            }
            Console.WriteLine("Manual formation lap mode is ACTIVE");
            audioPlayer.playMessageImmediately(new QueuedMessage(LapCounter.folderManualFormationLapModeEnabled, 0, null));
        }

        public void disableManualFormationLapMode()
        {
            if (GameStateData.useManualFormationLap)
            {
                GameStateData.useManualFormationLap = false;
                GameStateData.onManualFormationLap = false;
            }
            Console.WriteLine("Manual formation lap mode is DISABLED");
            audioPlayer.playMessageImmediately(new QueuedMessage(LapCounter.folderManualFormationLapModeDisabled, 0, null));
        }

        public void reportFuelStatus()
        {
            if (GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
            {
                ((Battery)eventsList["Battery"]).reportBatteryStatus(true);
                if (useVerboseResponses)
                {
                    ((Battery)eventsList["Battery"]).reportExtendedBatteryStatus(true, false);
                }
            }
            else
            {
                ((Fuel)eventsList["Fuel"]).reportFuelStatus(true, (CrewChief.currentGameState != null && CrewChief.currentGameState.SessionData.SessionType == SessionType.Race));
            }
        }

        public void toggleSpotterMode()
        {
            if (GlobalBehaviourSettings.spotterEnabled)
            {
                disableSpotter();
            }
            else
            {
                enableSpotter();
            }
        }

        public void enableKeepQuietMode()
        {
            keepQuietEnabled = true;
            audioPlayer.enableKeepQuietMode();
        }

        public void playCornerNamesForCurrentLap()
        {
            if (currentGameState != null)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                currentGameState.readLandmarksForThisLap = true;
            }
        }

        public void disableKeepQuietMode()
        {
            keepQuietEnabled = false;
            audioPlayer.disableKeepQuietMode();
        }

        public void enableSpotter()
        {
            if (spotter == null)
            {
                Console.WriteLine("No spotter configured for this game");
            }
            else
            {
                GlobalBehaviourSettings.spotterEnabled = true;
                spotter.enableSpotter();
            }
        }

        public void disableSpotter()
        {
            if (spotter != null)
            {
                GlobalBehaviourSettings.spotterEnabled = false;
                spotter.disableSpotter();
            }
        }

        public void respondToRadioCheck()
        {
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderRadioCheckResponse, 0, null));
        }

        public void youWot(Boolean detectedSomeSpeech)
        {
            if (!running)
            {
                return;
            }
            if (detectedSomeSpeech)
            {
                Console.WriteLine("Detected speech input but nothing was recognised");
            }
            else
            {
                Console.WriteLine("No speech input was detected");
            }
            
            if (DamageReporting.waitingForDriverIsOKResponse)
            {
                ((DamageReporting)CrewChief.getEvent("DamageReporting")).cancelWaitingForDriverIsOK(
                    detectedSomeSpeech ? DamageReporting.DriverOKResponseType.NOT_UNDERSTOOD : DamageReporting.DriverOKResponseType.NO_SPEECH);
            }
            else
            {
                // TODO: separate responses for no input detected, and input not understood?
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDidntUnderstand, 0, null));
            }
        }

        public void togglePaceNotesPlayback()
        {
            if (DriverTrainingService.isPlayingPaceNotes)
            {
                DriverTrainingService.stopPlayingPaceNotes();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
            }
            else
            {
                if (CrewChief.currentGameState != null && CrewChief.currentGameState.SessionData.TrackDefinition != null)
                {
                    if (!DriverTrainingService.isPlayingPaceNotes)
                    {
                        if (DriverTrainingService.loadPaceNotes(CrewChief.gameDefinition.gameEnum,
                                CrewChief.currentGameState.SessionData.TrackDefinition.name, CrewChief.currentGameState.carClass.carClassEnum))
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No track or car has been loaded - start an on-track session before loading a pace notes");
                }
            }
        }
        
        public void togglePaceNotesRecording()
        {
            if (DriverTrainingService.isRecordingPaceNotes)
            {
                DriverTrainingService.completeRecordingPaceNotes();
            }
            else
            {
                if (CrewChief.trackName == null || CrewChief.trackName.Equals(""))
                {
                    Console.WriteLine("No track has been loaded - start an on-track session before recording pace notes");
                    return;
                }
                if (CrewChief.carClass == CarData.CarClassEnum.UNKNOWN_RACE || CrewChief.carClass == CarData.CarClassEnum.USER_CREATED)
                {
                    Console.WriteLine("No car class has been set - this pace notes session will not be class specific");
                }
                DriverTrainingService.startRecordingPaceNotes(CrewChief.gameDefinition.gameEnum,
                    CrewChief.trackName, CrewChief.carClass);                
            }
        }
        public void toggleTrackLandmarkRecording()
        {
            if(TrackLandMarksRecorder.isRecordingTrackLandmarks)
            {
                TrackLandMarksRecorder.completeRecordingTrackLandmarks();
            }
            else
            {
                if (CrewChief.trackName == null || CrewChief.trackName.Equals(""))
                {
                    Console.WriteLine("No track has been loaded - start an on-track session before recording landmarks");
                    return;
                }
                else
                {
                    TrackLandMarksRecorder.startRecordingTrackLandmarks(CrewChief.gameDefinition.gameEnum,
                    CrewChief.trackName, CrewChief.raceroomTrackId);
                }
                
            }
        }
        public void toggleAddTrackLandmark()
        {
            if (TrackLandMarksRecorder.isRecordingTrackLandmarks)
            {
                TrackLandMarksRecorder.addLandmark(CrewChief.distanceRoundTrack);
            }
        }
        // nasty... these triggers come from the speech recogniser or from button presses, and invoke speech
        // recognition 'respond' methods in the events
        public static void getStatus()
        {
            getEvent("Penalties").respond(SpeechRecogniser.STATUS[0]);
            getEvent("RaceTime").respond(SpeechRecogniser.STATUS[0]);
            getEvent("Position").respond(SpeechRecogniser.STATUS[0]);
            getEvent("PitStops").respond(SpeechRecogniser.STATUS[0]);
            getEvent("DamageReporting").respond(SpeechRecogniser.STATUS[0]);
            if (GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
            {
                getEvent("Battery").respond(SpeechRecogniser.CAR_STATUS[0]);
            }
            else
            {
                getEvent("Fuel").respond(SpeechRecogniser.CAR_STATUS[0]);
            }
            getEvent("TyreMonitor").respond(SpeechRecogniser.STATUS[0]);
            getEvent("EngineMonitor").respond(SpeechRecogniser.STATUS[0]);
            getEvent("Timings").respond(SpeechRecogniser.STATUS[0]);
        }

        public static void getSessionStatus()
       {
            getEvent("Penalties").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("RaceTime").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("Position").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("PitStops").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("Timings").respond(SpeechRecogniser.SESSION_STATUS[0]);
        }

        public static void getCarStatus()
        {
            getEvent("DamageReporting").respond(SpeechRecogniser.CAR_STATUS[0]);
            if (GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
            {
                getEvent("Battery").respond(SpeechRecogniser.CAR_STATUS[0]);
            }
            else
            {
                getEvent("Fuel").respond(SpeechRecogniser.CAR_STATUS[0]);
            }
            getEvent("TyreMonitor").respond(SpeechRecogniser.CAR_STATUS[0]);
            getEvent("EngineMonitor").respond(SpeechRecogniser.CAR_STATUS[0]);
        }

        public static void getDamageReport()
        {
            getEvent("DamageReporting").respond(SpeechRecogniser.DAMAGE_REPORT[0]);
        }
        
        public void reportCurrentTime()
        {
            DateTime now = DateTime.Now;
            int hour = now.Hour;
            int minute = now.Minute;
            Boolean isPastMidDay = false;
            if (hour >= 12)
            {
                isPastMidDay = true;
            }
            if (AudioPlayer.soundPackLanguage == "it")
            {
                audioPlayer.playMessageImmediately(new QueuedMessage("current_time",
                    AbstractEvent.MessageContents(hour, NumberReaderIt2.folderAnd, now.Minute), 0, null));
            }
            else
            {
                if (hour == 0)
                {
                    isPastMidDay = false;
                    hour = 24;
                }
                if (hour > 12)
                {
                    hour = hour - 12;
                }
                if (minute < 10)
                {
                    if (minute == 0)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("current_time",
                           AbstractEvent.MessageContents(hour, isPastMidDay ? AlarmClock.folderPM : AlarmClock.folderAM), 0, null));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("current_time",
                            AbstractEvent.MessageContents(hour, NumberReader.folderOh, now.Minute, isPastMidDay ? AlarmClock.folderPM : AlarmClock.folderAM), 0, null));
                    }
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("current_time",
                        AbstractEvent.MessageContents(hour, now.Minute, isPastMidDay ? AlarmClock.folderPM : AlarmClock.folderAM), 0, null));
                }
            }
        }

        private void startSpotterThread()
        {
            if (spotter != null)
            {
                if (spotterThread != null)
                {
                    // This is the corner case when spotter was disabled during runtime.
                    stopSpotterThread();
                    spotterThread = null;
                }
                Debug.Assert(spotterThread == null);
                lastSpotterState = null;
                currentSpotterState = null;
                spotterIsRunning = true;
                ThreadStart work = spotterWork;

                // Thread owned and managed by CrewChief.Run thread.
                spotterThread = new Thread(work);

                runSpotterThread = true;
                spotterThread.Start();
            }
        }

        private void stopSpotterThread()
        {
            if (spotter != null && spotterThread != null)
            {
                runSpotterThread = false;

                if (spotterThread.IsAlive)
                {
                    Console.WriteLine("Waiting for spotter thread to stop...");
                    if (!spotterThread.Join(5000))
                    {
                        Console.WriteLine("Warning: Timed out waiting for spotter thread to stop to stop");
                    }
                    Console.WriteLine("Spotter thread stopped");
                }

                spotterThread = null;
            }
        }

        private void spotterWork()
        {
            Console.WriteLine("Invoking spotter every " + spotterInterval);
            try
            {
                while (runSpotterThread)
                {
                    if (spotter != null && gameDataReader.hasNewSpotterData())
                    {
                        currentSpotterState = gameDataReader.ReadGameData(true);
                        if (lastSpotterState != null && currentSpotterState != null)
                        {
                            try
                            {
                                spotter.trigger(lastSpotterState, currentSpotterState, currentGameState);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Spotter failed: " + e.StackTrace);
                            }
                        }
                        lastSpotterState = currentSpotterState;
                    }
                    Thread.Sleep(spotterInterval);
                }
            }
            catch (Exception)  // Exceptions can happen on Stop and DisconnectFromProcess.
            {
                Console.WriteLine("Spotter thread terminated.");
            }
            spotterIsRunning = false;
        }

        public Tuple<GridSide, Dictionary<int, GridSide>> getGridSide()
        {
            return this.spotter.getGridSide(this.latestRawGameData);
        }

        public Boolean Run(String filenameToRun, Boolean dumpToFile)
        {
            try
            {
                PlaybackModerator.SetCrewChief(this);

                loadDataFromFile = false;
                audioPlayer.mute = false;
                if (filenameToRun != null)
                {
                    loadDataFromFile = true;
                    GlobalBehaviourSettings.spotterEnabled = gameDefinition.gameEnum == GameEnum.F1_2018;
                    dumpToFile = false;
                }
                else
                {
                    dataFileReadDone = true;  // Don't block UI as we won't be loading from the file.
                }
                SpeechRecogniser.waitingForSpeech = false;
                SpeechRecogniser.gotRecognitionResult = false;
                SpeechRecogniser.keepRecognisingInHoldMode = false;
                GameStateMapper gameStateMapper = GameStateReaderFactory.getInstance().getGameStateMapper(gameDefinition);
                gameStateMapper.setSpeechRecogniser(speechRecogniser);
                gameDataReader = GameStateReaderFactory.getInstance().getGameStateReader(gameDefinition);
                gameDataReader.ResetGameDataFromFile();

                gameDataReader.dumpToFile = dumpToFile;
                if (gameDefinition.spotterName != null)
                {
                    spotter = (Spotter)Activator.CreateInstance(Type.GetType(gameDefinition.spotterName),
                        audioPlayer, GlobalBehaviourSettings.spotterEnabled);
                }
                else
                {
                    Console.WriteLine("No spotter defined for game " + gameDefinition.friendlyName);
                    spotter = null;
                }
                running = true;
                if (!audioPlayer.initialised)
                {
                    Console.WriteLine("Failed to initialise audio player");
                    return false;
                }
                // mute the audio player for anything < 10ms
                audioPlayer.mute = loadDataFromFile && CrewChief.playbackIntervalMilliseconds < 10;
                audioPlayer.startMonitor();
                Boolean attemptedToRunGame = false;

                Console.WriteLine("Polling for shared data every " + timeInterval + "ms");
                Boolean sessionFinished = false;
                while (running)
                {
                    DateTime now = DateTime.UtcNow;
                    //GameStateData.CurrentTime = now;
                   
                    alarmClock.trigger(null, null);

                    if (!loadDataFromFile)
                    {
                        // Turns our checking for running process by name is an expensive system call.  So don't do that on every tick.
                        if (now > nextProcessStateCheck)
                        {
                            nextProcessStateCheck = now.Add(
                                TimeSpan.FromMilliseconds(isGameProcessRunning ? timeBetweenProcDisconnectCheckMillis : timeBetweenProcConnectCheckMillis));
                            isGameProcessRunning = Utilities.IsGameRunning(gameDefinition.processName, gameDefinition.alternativeProcessNames);
                        }

                        if (mapped
                            && !isGameProcessRunning
                            && gameDefinition.HasAnyProcessNameAssociated())
                        {
                            gameDataReader.DisconnectFromProcess();
                            mapped = false;
                        }

                        if (!gameDefinition.HasAnyProcessNameAssociated()  // Network data case.
                            || isGameProcessRunning)
                        {
                            if (!mapped)
                            {
                                mapped = gameDataReader.Initialise();

                                // Instead of stressing process to death on failed mapping,
                                // give a it a break.
                                if (!mapped)
                                    Thread.Sleep(1000);
                            }
                        }
                        else if (UserSettings.GetUserSettings().getBoolean(gameDefinition.gameStartEnabledProperty) && !attemptedToRunGame)
                        {
                            Utilities.runGame(UserSettings.GetUserSettings().getString(gameDefinition.gameStartCommandProperty),
                                UserSettings.GetUserSettings().getString(gameDefinition.gameStartCommandOptionsProperty));
                            attemptedToRunGame = true;
                        }
                    }

                    if (loadDataFromFile || mapped)
                    {
                        stateCleared = false;

                        if (loadDataFromFile)
                        {
                            try
                            {
                                latestRawGameData = gameDataReader.ReadGameDataFromFile(filenameToRun, 3000);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error reading game data: " + e.StackTrace);
                            }
                            finally
                            {
                                dataFileReadDone = true;
                            }
                            if (latestRawGameData == null)
                            {
                                MainWindow.autoScrollConsole = true;
                                Console.WriteLine("Reached the end of the data file, sleeping to clear queued messages");
                                Utilities.InterruptedSleep(5000 /*totalWaitMillis*/, 500 /*waitWindowMillis*/, () => running /*keepWaitingPredicate*/);
                                try
                                {
                                    audioPlayer.purgeQueues();
                                }
                                catch (Exception)
                                {
                                    // ignore
                                }
                                running = false;
                                continue;
                            }
                        }
                        else
                        {
                            try
                            {
                                latestRawGameData = gameDataReader.ReadGameData(false);
                            }
                            catch (GameDataReadException e)
                            {
                                Console.WriteLine("Error reading game data " + e.cause.StackTrace);
                                continue;
                            }
                        }
                        // another Thread may have stopped the app - check here before processing the game data
                        if (!running)
                        {
                            continue;
                        }
                        gameStateMapper.versionCheck(latestRawGameData);

                        GameStateData nextGameState = null;
                        try
                        {
                            nextGameState = gameStateMapper.mapToGameStateData(latestRawGameData, currentGameState);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error mapping game data: " + e.Message + ", " + e.StackTrace);
                        }
                        // if we're paused or viewing another car, the mapper will just return the previous game state so we don't lose all the
                        // persistent state information. If this is the case, don't process any stuff
                        if (nextGameState != null && (nextGameState.SessionData.AbruptSessionEndDetected || nextGameState != currentGameState))
                        {
                            previousGameState = currentGameState;
                            currentGameState = nextGameState;
                            if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                gameStateMapper.populateDerivedRaceSessionData(currentGameState);
                            }
                            else
                            {
                                gameStateMapper.populateDerivedNonRaceSessionData(currentGameState);
                            }
                            if (!sessionFinished && currentGameState.SessionData.SessionPhase == SessionPhase.Finished
                                && previousGameState != null)
                            {
                                string positionMsg;
                                if (currentGameState.SessionData.IsDisqualified)
                                {
                                    positionMsg = "Disqualified";
                                }
                                else if (currentGameState.SessionData.IsDNF)
                                {
                                    positionMsg = "DNF";
                                }
                                else
                                {
                                    positionMsg = currentGameState.SessionData.ClassPosition.ToString();
                                }
                                Console.WriteLine("Session finished, position = " + positionMsg);
                                audioPlayer.purgeQueues();
                                if (displaySessionLapTimes)
                                {
                                    if (currentGameState.SessionData.formattedPlayerLapTimes.Count > 0)
                                    {
                                        Console.WriteLine("Session lap times:");
                                        Console.WriteLine(String.Join(";    ", currentGameState.SessionData.formattedPlayerLapTimes));
                                    }
                                    else
                                    {
                                        Console.WriteLine("No valid lap times were set.");
                                    }
                                }

                                if (CrewChief.gameDefinition.gameEnum != GameEnum.IRACING)
                                {
                                    sessionEndMessages.trigger(previousGameState.SessionData.SessionRunningTime, previousGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase,
                                        previousGameState.SessionData.SessionStartClassPosition, previousGameState.SessionData.ClassPosition,
                                        previousGameState.SessionData.NumCarsInPlayerClassAtStartOfSession, previousGameState.SessionData.CompletedLaps,
                                        currentGameState.SessionData.IsDisqualified, currentGameState.SessionData.IsDNF, currentGameState.Now);
                                }
                                else
                                {
                                    // In iRacing, use currentGameState.SessionData.ClassPosition.  I don't completely understand what is going on, but sometimes position is very wrong right
                                    // before finishing line.
                                    sessionEndMessages.trigger(previousGameState.SessionData.SessionRunningTime, previousGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase,
                                        previousGameState.SessionData.SessionStartClassPosition, currentGameState.SessionData.ClassPosition,
                                        previousGameState.SessionData.NumCarsInPlayerClassAtStartOfSession, previousGameState.SessionData.CompletedLaps,
                                        currentGameState.SessionData.IsDisqualified, currentGameState.SessionData.IsDNF, currentGameState.Now);
                                }
                                audioPlayer.wakeMonitorThreadForRegularMessages(currentGameState.Now);
                                sessionFinished = true;
                                audioPlayer.disablePearlsOfWisdom = false;

                                if (loadDataFromFile)
                                {
                                    Utilities.InterruptedSleep(2000 /*totalWaitMillis*/, 500 /*waitWindowMillis*/, () => running /*keepWaitingPredicate*/);
                                }
                            }
                            float prevTime = previousGameState == null ? 0 : previousGameState.SessionData.SessionRunningTime;
                            if (currentGameState.SessionData.IsNewSession)
                            {
                                Console.WriteLine("New session");
                                PlaybackModerator.ClearVerbosityData();
                                PlaybackModerator.lastBlockedMessageId = -1;
                                audioPlayer.disablePearlsOfWisdom = false;
                                displayNewSessionInfo(currentGameState);
                                sessionFinished = false;
                                if (!stateCleared)
                                {
                                    Console.WriteLine("Clearing game state...");
                                    audioPlayer.purgeQueues();

                                    foreach (KeyValuePair<String, AbstractEvent> entry in eventsList)
                                    {
                                        entry.Value.clearState();
                                    }
                                    if (spotter != null)
                                    {
                                        spotter.clearState();
                                    }
                                    faultingEvents.Clear();
                                    faultingEventsCount.Clear();
                                    sessionHasFailingEvent = false;
                                    stateCleared = true;
                                    PCarsGameStateMapper.FIRST_VIEWED_PARTICIPANT_NAME = null;
                                    PCarsGameStateMapper.WARNED_ABOUT_MISSING_STEAM_ID = false;
                                    PCarsGameStateMapper.FIRST_VIEWED_PARTICIPANT_INDEX = -1;
                                }
                                if (enableDriverNames)
                                {
                                    List<String> rawDriverNames = currentGameState.getRawDriverNames();
                                    if (currentGameState.SessionData.DriverRawName != null && currentGameState.SessionData.DriverRawName.Length > 0 &&
                                        !rawDriverNames.Contains(currentGameState.SessionData.DriverRawName))
                                    {
                                        rawDriverNames.Add(currentGameState.SessionData.DriverRawName);
                                    }
                                    if (rawDriverNames.Count > 0)
                                    {
                                        List<String> usableDriverNames = DriverNameHelper.getUsableDriverNames(rawDriverNames);
                                        if (speechRecogniser != null && speechRecogniser.initialised)
                                        {
                                            speechRecogniser.addOpponentSpeechRecognition(usableDriverNames, enableDriverNames);
                                        }
                                        // now load all the sound files for this set of driver names
                                        SoundCache.loadDriverNameSounds(usableDriverNames);
                                    }
                                }
                                audioPlayer.wakeMonitorThreadForRegularMessages(currentGameState.Now);
                            }
                            else if (previousGameState != null &&
                                        (gameDefinition.gameEnum == GameEnum.F1_2018 ||
                                        (((gameDefinition.gameEnum == GameEnum.PCARS2 && currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) ||
                                            currentGameState.SessionData.SessionRunningTime > previousGameState.SessionData.SessionRunningTime) ||
                                        (previousGameState.SessionData.SessionPhase != currentGameState.SessionData.SessionPhase)) ||
                                        ((gameDefinition.gameEnum == GameEnum.PCARS_32BIT || gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                                                gameDefinition.gameEnum == GameEnum.PCARS2 || gameDefinition.gameEnum == GameEnum.PCARS_NETWORK ||
                                                gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK) &&
                                            currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTotalRunTime == -1)))
                            {
                                if (!sessionFinished)
                                {
                                    if (spotter != null)
                                    {
                                        if (currentGameState.FlagData.isFullCourseYellow || DamageReporting.waitingForDriverIsOKResponse)
                                        {
                                            spotter.pause();
                                        }
                                        else
                                        {
                                            spotter.unpause();
                                        }
                                    }
                                    if (currentGameState.SessionData.IsNewLap)
                                    {
                                        currentGameState.display();
                                    }
                                    stateCleared = false;
                                }
                                // update the auto-verbosity
                                PlaybackModerator.UpdateAutoVerbosity(currentGameState);

                                // Allow events to be processed after session finish.  Event should use applicableSessionPhases/applicableSessionTypes to opt in/out.
                                // for now, don't trigger any events for F1 2018 as there's no game mapping
                                if (gameDefinition.gameEnum != GameEnum.F1_2018)
                                {
                                    foreach (KeyValuePair<String, AbstractEvent> entry in eventsList)
                                    {
                                        if (entry.Value.isApplicableForCurrentSessionAndPhase(currentGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase))
                                        {
                                            // special case - if we've crashed heavily and are waiting for a response from the driver, don't trigger other events
                                            if (entry.Key.Equals("DamageReporting") || !DamageReporting.waitingForDriverIsOKResponse)
                                            {
                                                triggerEvent(entry.Key, entry.Value, previousGameState, currentGameState);
                                            }
                                        }
                                    }
                                    audioPlayer.wakeMonitorThreadForRegularMessages(currentGameState.Now);
                                }
                                if (!sessionFinished)
                                {
                                    if (DriverTrainingService.isPlayingPaceNotes)
                                    {
                                        DriverTrainingService.checkDistanceAndPlayIfNeeded(currentGameState.Now, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                                            currentGameState.PositionAndMotionData.DistanceRoundTrack, audioPlayer);
                                    }
                                    if (spotter != null && GlobalBehaviourSettings.spotterEnabled && !spotterIsRunning &&
                                        (gameDefinition.gameEnum == GameEnum.F1_2018 || !loadDataFromFile))
                                    {
                                        Console.WriteLine("********** starting spotter***********");
                                        spotter.clearState();
                                        startSpotterThread();
                                    }
                                    else if (spotterIsRunning && !GlobalBehaviourSettings.spotterEnabled)
                                    {
                                        runSpotterThread = false;
                                    }
                                }
                            }
                            else if (spotter != null)
                            {
                                spotter.pause();
                            }
                        }
                    }
                    if (filenameToRun != null)
                    {
                        // mute the audio player for anything < 10ms
                        audioPlayer.mute = CrewChief.playbackIntervalMilliseconds < 10;
                        if (CrewChief.playbackIntervalMilliseconds > 0)
                        {
                            Thread.Sleep(CrewChief.playbackIntervalMilliseconds);
                        }
                    }
                    else
                    {
                        Thread.Sleep(timeInterval);
                    }
                }
                foreach (KeyValuePair<String, AbstractEvent> entry in eventsList)
                {
                    entry.Value.clearState();
                }
                if (spotter != null)
                {
                    spotter.clearState();
                }
                stateCleared = true;
                currentGameState = null;
                previousGameState = null;
                sessionFinished = false;
                faultingEvents.Clear();
                faultingEventsCount.Clear();
                PlaybackModerator.ClearVerbosityData();
                PlaybackModerator.lastBlockedMessageId = -1;
                if (audioPlayer != null)
                {
                    audioPlayer.disablePearlsOfWisdom = false;
                }
                sessionHasFailingEvent = false;
                if (gameDataReader != null)
                {
                    if (gameDataReader.dumpToFile)
                    {
                        try
                        {
                            gameDataReader.DumpRawGameData();
                        }
                        finally
                        {
                            dataFileDumpDone = true;
                        }
                    }
                    dataFileDumpDone = true;
                    try
                    {
                        gameDataReader.stop();
                        gameDataReader.DisconnectFromProcess();
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
                if (SoundCache.dumpListOfUnvocalizedNames)
                {
                    DriverNameHelper.dumpUnvocalizedNames();
                }
                mapped = false;
            }
            finally
            {
                // Thread cleanup.

                if (speechRecogniser != null)
                {
                    speechRecogniser.stop();
                }

                // Wait on child threads and release owned resources here.
                Console.WriteLine("Stopping queue monitor");
                if (audioPlayer != null)
                {
                    audioPlayer.stopMonitor();
                    audioPlayer.disablePearlsOfWisdom = false;
                }
                SoundCache.saveVarietyData();

                stopSpotterThread();

                // Release thread resources:
                if (gameDataReader != null)
                {
                    gameDataReader.Dispose();
                    gameDataReader = null;
                }
            }

            return true;
        }

        private void triggerEvent(String eventName, AbstractEvent abstractEvent, GameStateData previousGameState, GameStateData currentGameState)
        {
            try
            {
                int failureCount;
                if (!sessionHasFailingEvent || !faultingEventsCount.TryGetValue(eventName, out failureCount) || failureCount < maxEventFailuresBeforeDisabling)
                {
                    abstractEvent.trigger(previousGameState, currentGameState);
                }
            }
            catch (Exception e)
            {
                int failureCount = 0;
                if (faultingEventsCount.TryGetValue(eventName, out failureCount))
                {
                    faultingEventsCount[eventName] = ++failureCount;
                    if (failureCount >= maxEventFailuresBeforeDisabling)
                    {
                        sessionHasFailingEvent = true;
                        Console.WriteLine("Event " + eventName +
                            " has failed " + maxEventFailuresBeforeDisabling + " times in this session and will be disabled");
                    }
                }
                if (!faultingEvents.ContainsKey(eventName))
                {
                    Console.WriteLine("Event " + eventName + " threw exception " + e.Message + " stack " + e.StackTrace);
                    Console.WriteLine("This is the first time this event has failed in this session");
                    faultingEvents.Add(eventName, e.Message);
                    faultingEventsCount.Add(eventName, 1);
                }
                else if (faultingEvents[eventName] != e.Message)
                {
                    Console.WriteLine("Event " + eventName + " threw a different exception: " + e.Message);
                    faultingEvents[eventName] = e.Message;
                }
            }
        }

        public void stop()
        {
            running = false;
            runSpotterThread = false;
            if (audioPlayer != null)
            {
                audioPlayer.monitorRunning = false;
            }
        }

        private void displayNewSessionInfo(GameStateData currentGameState)
        {
            Console.WriteLine("New session details...");
            Console.WriteLine("SessionType: " + currentGameState.SessionData.SessionType);
            Console.WriteLine("EventIndex: " + currentGameState.SessionData.EventIndex);
            Console.WriteLine("SessionIteration: " + currentGameState.SessionData.SessionIteration);
            String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
            Console.WriteLine("TrackName: \"" + trackName + "\"");
        }

        public static Boolean isPCars()
        {
            return CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT ||
                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK ||
                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2 ||
                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK;
        }

        // This has to be called before starting man Chief thread (runApp).
        public void onRestart()
        {
            dataFileReadDone = false;
            dataFileDumpDone = false;
        }
    }
}
