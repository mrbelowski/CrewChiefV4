﻿using System;
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


namespace CrewChiefV4
{
    public class CrewChief : IDisposable
    {
        readonly int timeBetweenProcConnectCheckMillis = 500;
        readonly int timeBetweenProcDisconnectCheckMillis = 2000;
        DateTime nextProcessStateCheck = DateTime.MinValue;
        bool isGameProcessRunning = false;

        public static Boolean loadDataFromFile = false;

        public SpeechRecogniser speechRecogniser;

        public static GameDefinition gameDefinition;

        public static Boolean readOpponentDeltasForEveryLap = false;
        // initial state from properties but can be overridden during a session:
        public static Boolean yellowFlagMessagesEnabled = UserSettings.GetUserSettings().getBoolean("enable_yellow_flag_messages");
        private Boolean keepQuietEnabled = false;
                
        public static Boolean enableDriverNames = UserSettings.GetUserSettings().getBoolean("enable_driver_names");

        public static TimeSpan _timeInterval = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("update_interval"));

        public static TimeSpan spotterInterval = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_update_interval"));

        private Boolean displaySessionLapTimes = UserSettings.GetUserSettings().getBoolean("display_session_lap_times");
        
        private static Dictionary<String, AbstractEvent> eventsList = new Dictionary<String, AbstractEvent>();

        public AudioPlayer audioPlayer;

        Object lastSpotterState;
        Object currentSpotterState;

        Boolean stateCleared = false;

        public Boolean running = false;

        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        private Dictionary<String, String> faultingEvents = new Dictionary<String, String>();
        
        private Dictionary<String, int> faultingEventsCount = new Dictionary<String, int>();

        private Spotter spotter;

        private Boolean spotterIsRunning = false;

        private Boolean runSpotterThread = false;

        private Boolean disableImmediateMessages = UserSettings.GetUserSettings().getBoolean("disable_immediate_messages");

        private GameStateMapper gameStateMapper;

        private GameDataReader gameDataReader;

        // hmm....
        public static GameStateData currentGameState = null;

        public GameStateData previousGameState = null;

        private Boolean mapped = false;

        private SessionEndMessages sessionEndMessages;

        // used for the pace notes recorder - need to separate out from the currentGameState so we can
        // set these even when viewing replays
        public static String trackName = "";
        public static CarData.CarClassEnum carClass = CarData.CarClassEnum.UNKNOWN_RACE;
        public static Boolean viewingReplay = false;
        public static float distanceRoundTrack = -1;

        public CrewChief()
        {
            speechRecogniser = new SpeechRecogniser(this);
            audioPlayer = new AudioPlayer();
            audioPlayer.initialise();
            eventsList.Add("Timings", new Timings(audioPlayer));
            eventsList.Add("Position", new Position(audioPlayer));
            eventsList.Add("LapCounter", new LapCounter(audioPlayer));
            eventsList.Add("LapTimes", new LapTimes(audioPlayer));
            eventsList.Add("Penalties", new Penalties(audioPlayer));
            eventsList.Add("MandatoryPitStops", new MandatoryPitStops(audioPlayer));
            eventsList.Add("Fuel", new Fuel(audioPlayer));
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
            eventsList.Add("PitStop", new PitStop(audioPlayer));
            sessionEndMessages = new SessionEndMessages(audioPlayer);
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
            if (gameDataReader != null)
            {
                gameDataReader.Dispose();
            }
            audioPlayer.stopMonitor();
            if (speechRecogniser != null)
            {
                speechRecogniser.Dispose();
            }
            if (audioPlayer != null)
            {
                audioPlayer.Dispose();
            }

            gameDataReader = null;
            speechRecogniser = null;
            audioPlayer = null;
        }

        public static AbstractEvent getEvent(String eventName)
        {
            if (eventsList.ContainsKey(eventName))
            {
                return eventsList[eventName];
            }
            else
            {
                return null;
            }
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
            ((Fuel)eventsList["Fuel"]).reportFuelStatus(true);
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
            if (disableImmediateMessages)
            {
                Console.WriteLine("Unable to start spotter - immediate messages are disabled");
            }
            else if (spotter == null)
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

        public void youWot()
        {
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDidntUnderstand, 0, null));
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

        // nasty... these triggers come from the speech recogniser or from button presses, and invoke speech
        // recognition 'respond' methods in the events
        public static void getStatus()
        {
            getEvent("Penalties").respond(SpeechRecogniser.STATUS[0]);
            getEvent("RaceTime").respond(SpeechRecogniser.STATUS[0]);
            getEvent("Position").respond(SpeechRecogniser.STATUS[0]);
            getEvent("MandatoryPitStops").respond(SpeechRecogniser.STATUS[0]);
            getEvent("DamageReporting").respond(SpeechRecogniser.STATUS[0]);
            getEvent("Fuel").respond(SpeechRecogniser.STATUS[0]);
            getEvent("TyreMonitor").respond(SpeechRecogniser.STATUS[0]);
            getEvent("EngineMonitor").respond(SpeechRecogniser.STATUS[0]);
            getEvent("Timings").respond(SpeechRecogniser.STATUS[0]);
        }

        public static void getSessionStatus()
       {
            getEvent("Penalties").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("RaceTime").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("Position").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("MandatoryPitStops").respond(SpeechRecogniser.SESSION_STATUS[0]);
            getEvent("Timings").respond(SpeechRecogniser.SESSION_STATUS[0]);
        }

        public static void getCarStatus()
        {
            getEvent("DamageReporting").respond(SpeechRecogniser.CAR_STATUS[0]);
            getEvent("Fuel").respond(SpeechRecogniser.CAR_STATUS[0]);
            getEvent("TyreMonitor").respond(SpeechRecogniser.CAR_STATUS[0]);
            getEvent("EngineMonitor").respond(SpeechRecogniser.CAR_STATUS[0]);
        }

        public static void getDamageReport()
        {
            getEvent("DamageReporting").respond(SpeechRecogniser.DAMAGE_REPORT[0]);
        }
        //


        public void reportCurrentTime()
        {
            DateTime now = DateTime.Now;
            int hour = now.Hour;
            if (hour == 0)
            {
                hour = 24;
            }
            if (hour > 12) {
                hour = hour - 12;
            }
            audioPlayer.playMessageImmediately(new QueuedMessage("current_time", 
                AbstractEvent.MessageContents(hour, now.Minute), 0, null));
        }

        private void startSpotterThread()
        {
            if (spotter != null)
            {
                lastSpotterState = null;
                currentSpotterState = null;
                spotterIsRunning = true;
                ThreadStart work = spotterWork;
                Thread thread = new Thread(work);
                runSpotterThread = true;
                thread.Start();
            }
        }

        private void spotterWork()
        {
            int threadSleepTime = ((int) spotterInterval.Milliseconds / 10) + 1;
            DateTime nextRunTime = DateTime.Now;
            Console.WriteLine("Invoking spotter every " + spotterInterval.Milliseconds + "ms, pausing " + threadSleepTime + "ms between invocations");

            try
            {
                while (runSpotterThread)
                {
                    DateTime now = DateTime.Now;
                    if (now > nextRunTime && spotter != null && gameDataReader.hasNewSpotterData())
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
                        nextRunTime = DateTime.Now.Add(spotterInterval);
                    }
                    Thread.Sleep(threadSleepTime);
                }
            }
            catch (Exception)  // Exceptions can happen on Stop and DisconnectFromProcess.
            {
                Console.WriteLine("Spotter thread terminated.");
            }
            spotterIsRunning = false;
        }

        public Boolean Run(String filenameToRun, int interval, Boolean dumpToFile)
        {
            loadDataFromFile = false;
            audioPlayer.mute = false;
            if (filenameToRun != null && System.Diagnostics.Debugger.IsAttached)
            {
                loadDataFromFile = true;
                GlobalBehaviourSettings.spotterEnabled = false;
                if (interval > 0)
                {
                    _timeInterval = TimeSpan.FromMilliseconds(interval);
                    audioPlayer.mute = false;
                }
                else
                {
                    _timeInterval = TimeSpan.Zero;
                    audioPlayer.mute = true;
                }
                dumpToFile = false;
            }
            SpeechRecogniser.waitingForSpeech = false;
            SpeechRecogniser.gotRecognitionResult = false;
            SpeechRecogniser.keepRecognisingInHoldMode = false;
            gameStateMapper = GameStateReaderFactory.getInstance().getGameStateMapper(gameDefinition);
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
            DateTime nextRunTime = DateTime.Now;
            if (!audioPlayer.initialised)
            {
                Console.WriteLine("Failed to initialise audio player");
                return false;
            }
            audioPlayer.startMonitor();
            Boolean attemptedToRunGame = false;

            Console.WriteLine("Polling for shared data every " + _timeInterval.Milliseconds + "ms");
            Boolean sessionFinished = false;

            while (running)
            {
                DateTime now = DateTime.Now;
                //GameStateData.CurrentTime = now;
                if (now > nextRunTime)
                {
                    // ensure the updates don't get synchronised with the spotter / UDP receiver
                    int updateTweak = Utilities.random.Next(10) - 5;
                    nextRunTime = DateTime.Now.Add(_timeInterval);
                    nextRunTime.Add(TimeSpan.FromMilliseconds(updateTweak));
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
                        Object rawGameData;
                        if (loadDataFromFile)
                        {
                            rawGameData = gameDataReader.ReadGameDataFromFile(filenameToRun);
                            if (rawGameData == null)
                            {
                                Console.WriteLine("Reached the end of the data file, sleeping to clear queued messages");
                                Thread.Sleep(5000);
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
                            rawGameData = gameDataReader.ReadGameData(false);
                        }
                        gameStateMapper.versionCheck(rawGameData);

                        GameStateData nextGameState = null;
                        try
                        {
                            nextGameState = gameStateMapper.mapToGameStateData(rawGameData, currentGameState);
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
                                    positionMsg = currentGameState.SessionData.Position.ToString();
                                }
                                Console.WriteLine("Session finished, position = " + positionMsg);
                                audioPlayer.purgeQueues();
                                if (displaySessionLapTimes)
                                {
                                    Console.WriteLine("Session lap times:");
                                    Console.WriteLine(String.Join(";", currentGameState.SessionData.formattedPlayerLapTimes));
                                }
                                sessionEndMessages.trigger(previousGameState.SessionData.SessionRunningTime, previousGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase,
                                    previousGameState.SessionData.SessionStartPosition, previousGameState.SessionData.Position, previousGameState.SessionData.NumCarsAtStartOfSession, previousGameState.SessionData.CompletedLaps,
                                    previousGameState.SessionData.IsDisqualified, previousGameState.SessionData.IsDNF);

                                sessionFinished = true;
                                audioPlayer.disablePearlsOfWisdom = false;
                                if (loadDataFromFile)
                                {
                                    Thread.Sleep(2000);
                                }
                            }
                            float prevTime = previousGameState == null ? 0 : previousGameState.SessionData.SessionRunningTime;
                            if (currentGameState.SessionData.IsNewSession)
                            {
                                Console.WriteLine("New session");
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
                            }
                            // TODO: for AC free practice sessions, the SessionRunningTime is set to 1 hour in the mapper and stays there so this block never triggers
                            else if (!sessionFinished && previousGameState != null &&
                                        (currentGameState.SessionData.SessionRunningTime > previousGameState.SessionData.SessionRunningTime ||
                                        (previousGameState.SessionData.SessionPhase != currentGameState.SessionData.SessionPhase)) ||
                                        ((gameDefinition.gameEnum == GameEnum.PCARS_32BIT || gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                                                gameDefinition.gameEnum == GameEnum.PCARS2 || gameDefinition.gameEnum == GameEnum.PCARS_NETWORK ||
                                                gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK) &&
                                            currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTotalRunTime == -1))
                            {
                                if (spotter != null)
                                {
                                    if (currentGameState.FlagData.isFullCourseYellow)
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
                                foreach (KeyValuePair<String, AbstractEvent> entry in eventsList)
                                {
                                    if (entry.Value.isApplicableForCurrentSessionAndPhase(currentGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase))
                                    {
                                        triggerEvent(entry.Key, entry.Value, previousGameState, currentGameState);
                                    }
                                }
                                if (DriverTrainingService.isPlayingPaceNotes)
                                {
                                    DriverTrainingService.checkDistanceAndPlayIfNeeded(currentGameState.Now, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                                        currentGameState.PositionAndMotionData.DistanceRoundTrack, audioPlayer);
                                }
                                if (spotter != null && GlobalBehaviourSettings.spotterEnabled && !spotterIsRunning && !loadDataFromFile)
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
                            else if (spotter != null)
                            {
                                spotter.pause();
                            }
                        }
                    }
                }
                else
                {
                    // ensure the updates don't get synchronised with the spotter / UDP receiver
                    int threadSleepTime = 5 + Utilities.random.Next(10);
                    Thread.Sleep(threadSleepTime);
                    continue;
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
            Console.WriteLine("Stopping queue monitor");
            if (audioPlayer != null)
            {
                audioPlayer.stopMonitor();
                audioPlayer.disablePearlsOfWisdom = false;
            }
            if (gameDataReader != null)
            {
                if (gameDataReader.dumpToFile)
                {
                    gameDataReader.DumpRawGameData();
                }
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
            mapped = false;

            return true;
        }

        private void triggerEvent(String eventName, AbstractEvent abstractEvent, GameStateData previousGameState, GameStateData currentGameState)
        {
            try
            {
                abstractEvent.trigger(previousGameState, currentGameState);
            }
            catch (Exception e)
            {
                if (faultingEventsCount.ContainsKey(eventName))
                {
                    faultingEventsCount[eventName]++;
                    if (faultingEventsCount[eventName] > 5)
                    {
                        Console.WriteLine("Event " + eventName +
                            " has failed > 5 times in this session");
                    }
                }
                if (!faultingEvents.ContainsKey(eventName))
                {
                    Console.WriteLine("Event " + eventName + " threw exception " + e.Message);
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
        }

        private void displayNewSessionInfo(GameStateData currentGameState)
        {
            Console.WriteLine("New session details...");
            Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
            Console.WriteLine("EventIndex " + currentGameState.SessionData.EventIndex);
            Console.WriteLine("SessionIteration " + currentGameState.SessionData.SessionIteration);
            String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
            Console.WriteLine("TrackName " + trackName);
        }
    }
}
