using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class MulticlassWarnings : AbstractEvent
    {
        private Boolean enableMulticlassWarnings = UserSettings.GetUserSettings().getBoolean("enable_multiclass_messages");

        private String folderFasterCarsFightingBehindIncludingClassLeader = "multiclass/faster_cars_fighting_behind_inc_class_leader";
        private String folderFasterCarsBehindIncludingClassLeader = "multiclass/faster_cars_behind_inc_class_leader";
        private String folderFasterCarsBehindFighting = "multiclass/faster_cars_behind_fighting";
        private String folderFasterCarsBehind = "multiclass/faster_cars_behind";
        private String folderFasterCarBehindRacingPlayerForPositionIsClassLeader = "multiclass/faster_car_behind_racing_player_is_class_leader"; // this one's a mouth-full :(
        private String folderFasterCarBehindIsClassLeader = "multiclass/faster_car_behind_is_class_leader";
        private String folderFasterCarBehindRacingPlayerForPosition = "multiclass/faster_car_behind_racing_player";
        private String folderFasterCarBehind = "multiclass/faster_car_behind";

        private String folderSlowerCarsFightingAheadIncludingClassLeader = "multiclass/slower_cars_fighting_ahead_inc_class_leader";
        private String folderSlowerCarsAheadIncludingClassLeader = "multiclass/slower_cars_ahead_inc_class_leader";
        private String folderSlowerCarsAheadFighting = "multiclass/slower_cars_ahead_fighting";
        private String folderSlowerCarsAhead = "multiclass/slower_cars_ahead";
        private String folderSlowerCarAheadRacingPlayerForPositionIsClassLeader = "multiclass/slower_car_ahead_racing_player_is_class_leader";
        private String folderSlowerCarAheadClassLeader = "multiclass/slower_car_ahead_is_class_leader";
        private String folderSlowerCarAheadRacingPlayerForPosition = "multiclass/slower_car_ahead_racing_player";
        private String folderSlowerCarAhead = "multiclass/slower_car_ahead";

        private static String folderFasterClass = "multiclass/it_is_a_faster_class";
        private static String folderSlowerClass = "multiclass/it_is_a_slower_class";
        private static String folderSameClassAsUs = "multiclass/same_class_as_us";
        private static String folderLmp1 = "multiclass/lmp1";
        private static String folderLmp2 = "multiclass/lmp2";
        private static String folderLmp3 = "multiclass/lmp3";
        private static String folderGT1 = "multiclass/gt1";
        private static String folderGT2 = "multiclass/gt2";
        private static String folderGTE = "multiclass/gte";
        private static String folderGTC = "multiclass/gtc";
        private static String folderGTLM = "multiclass/gtlm";
        private static String folderGT3 = "multiclass/gt3";
        private static String folderGT4 = "multiclass/gt4";
        private static String folderGT5 = "multiclass/gt5";
        private static String folderGT500 = "multiclass/gt500";
        private static String folderGT300 = "multiclass/gt300";
        private static String folderDTM = "multiclass/dtm";
        private static String folderGroupA = "multiclass/groupa";
        private static String folderGroupB = "multiclass/groupb";
        private static String folderGroupC = "multiclass/groupc";
        private static String folderGroup4 = "multiclass/group4";
        private static String folderGroup5 = "multiclass/group5";
        private static String folderGroup6 = "multiclass/group6";
        private static String folderGTO = "multiclass/gto";
        private static String folderTC1 = "multiclass/tc1";
        private static String folderTC2 = "multiclass/tc2";
        private static String folderCarreraCup = "multiclass/carrera_cup";

        private static Dictionary<CarData.CarClassEnum, string> carClassEnumToSound = new Dictionary<CarData.CarClassEnum, string>();

        private DateTime nextCheckForOtherCarClasses = DateTime.MinValue;
        private TimeSpan timeBetweenOtherClassChecks = TimeSpan.FromSeconds(4);
        private TimeSpan timeToWaitForOtherClassWarningToSettle = TimeSpan.FromSeconds(6);
        private OtherCarClassWarningData previousCheckOtherClassWarningData;
        private HashSet<string> driverNamesForSlowerClassLastWarnedAbout = new HashSet<string>();
        private HashSet<string> driverNamesForFasterClassLastWarnedAbout = new HashSet<string>();

        private DateTime timeOfLastSingleCarFasterClassWarning = DateTime.MinValue;
        private DateTime timeOfLastMultipleCarFasterClassWarning = DateTime.MinValue;
        private DateTime timeOfLastSingleCarSlowerClassWarning = DateTime.MinValue;
        private DateTime timeOfLastMultipleCarSlowerClassWarning = DateTime.MinValue;

        // distance ahead where we consider slower cars. As we'll be behind the opponent, the separation value is negative
        private float slowerCarWarningZoneStart = -100;
        private float slowerCarWarningZoneEnd = -400;

        // distance behind where we consider faster cars
        private float fasterCarWarningZoneStart = 400;
        private float fasterCarWarningZoneEnd = 100;

        Dictionary<CarData.CarClass, float> bestTimesByClass = new Dictionary<CarData.CarClass, float>();

        enum ClassSpeedComparison {SLOWER, FASTER, UNKNOWN};

        private GameStateData currentGameState;

        static MulticlassWarnings()
        {
            carClassEnumToSound.Add(CarData.CarClassEnum.LMP1, folderLmp1);
            carClassEnumToSound.Add(CarData.CarClassEnum.LMP2, folderLmp2);
            carClassEnumToSound.Add(CarData.CarClassEnum.LMP3, folderLmp3);
            carClassEnumToSound.Add(CarData.CarClassEnum.GT1, folderGT1);
            carClassEnumToSound.Add(CarData.CarClassEnum.GT2, folderGT2);
            carClassEnumToSound.Add(CarData.CarClassEnum.GT3, folderGT3);
            carClassEnumToSound.Add(CarData.CarClassEnum.GT4, folderGT4);
            carClassEnumToSound.Add(CarData.CarClassEnum.GT5, folderGT5);
            carClassEnumToSound.Add(CarData.CarClassEnum.GTO, folderGTO);
            carClassEnumToSound.Add(CarData.CarClassEnum.GTC, folderGTC);
            carClassEnumToSound.Add(CarData.CarClassEnum.GTE, folderGTE);
            carClassEnumToSound.Add(CarData.CarClassEnum.GTLM, folderGTLM);
            //carClassEnumToSound.Add(CarData.CarClassEnum.GT300, folderGT300);
            carClassEnumToSound.Add(CarData.CarClassEnum.GT500, folderGT500);
            carClassEnumToSound.Add(CarData.CarClassEnum.DTM, folderDTM);
            carClassEnumToSound.Add(CarData.CarClassEnum.GROUPA, folderGroupA);
            //carClassEnumToSound.Add(CarData.CarClassEnum.GROUPB, folderGroupB);
            carClassEnumToSound.Add(CarData.CarClassEnum.GROUPC, folderGroupC);
            carClassEnumToSound.Add(CarData.CarClassEnum.GROUP4, folderGroup4);
            carClassEnumToSound.Add(CarData.CarClassEnum.GROUP5, folderGroup5);
            carClassEnumToSound.Add(CarData.CarClassEnum.GROUP6, folderGroup6);
            carClassEnumToSound.Add(CarData.CarClassEnum.TC1, folderTC1);
            carClassEnumToSound.Add(CarData.CarClassEnum.TC2, folderTC2);
            carClassEnumToSound.Add(CarData.CarClassEnum.CARRERA_CUP, folderCarreraCup);
        }

        public MulticlassWarnings(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;            
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Race }; }
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green }; }
        }

        public override void clearState()
        {
            nextCheckForOtherCarClasses = DateTime.MinValue;
            previousCheckOtherClassWarningData = null;
            driverNamesForSlowerClassLastWarnedAbout.Clear();
            driverNamesForFasterClassLastWarnedAbout.Clear();
            timeOfLastSingleCarFasterClassWarning = DateTime.MinValue;
            timeOfLastMultipleCarFasterClassWarning = DateTime.MinValue;
            timeOfLastSingleCarSlowerClassWarning = DateTime.MinValue;
            timeOfLastMultipleCarSlowerClassWarning = DateTime.MinValue;
            bestTimesByClass.Clear();
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            this.currentGameState = currentGameState;
            if (!enableMulticlassWarnings ||
                GameStateData.onManualFormationLap ||GameStateData.NumberOfClasses == 1 || GameStateData.forceSingleClass(currentGameState) ||
                currentGameState.SessionData.TrackDefinition == null || currentGameState.SessionData.CompletedLaps < 3 ||
                currentGameState.SessionData.PlayerLapTimeSessionBest <= 0 || currentGameState.PitData.InPitlane ||
                currentGameState.PositionAndMotionData.CarSpeed < 5 || currentGameState.PositionAndMotionData.DistanceRoundTrack <= 0)
            {
                return;
            }
            // update the best lap for each class - do this every sector once we've completed 2 laps
            if (currentGameState.SessionData.CompletedLaps > 1 && currentGameState.SessionData.IsNewSector)
            {
                getBestTimesByClass(currentGameState);
            }
            if (currentGameState.Now > nextCheckForOtherCarClasses)
            {
                OtherCarClassWarningData otherClassWarningData = getOtherCarClassWarningData(currentGameState);
                if (otherClassWarningData != null && (otherClassWarningData.numFasterCars > 0 || otherClassWarningData.numSlowerCars > 0))
                {
                    if (previousCheckOtherClassWarningData == null)
                    {
                        previousCheckOtherClassWarningData = otherClassWarningData;
                        // wait a while and check again before announcing
                        nextCheckForOtherCarClasses = currentGameState.Now.Add(timeToWaitForOtherClassWarningToSettle);
                    }
                    else
                    {
                        // TODO: THIS LOGIC FOR CHECKING THE STATE HAS CHANGED BEFORE ANNOUNCING IS BOLLOCKS
                        // check if this data is consistent with the previous data and that we've not warned about all these cars
                        if (otherClassWarningData.canAnnounce(previousCheckOtherClassWarningData, driverNamesForFasterClassLastWarnedAbout, driverNamesForSlowerClassLastWarnedAbout,
                            currentGameState.Now, timeOfLastSingleCarFasterClassWarning, timeOfLastMultipleCarFasterClassWarning, 
                            timeOfLastSingleCarSlowerClassWarning, timeOfLastMultipleCarSlowerClassWarning))
                        {
                            previousCheckOtherClassWarningData = null;
                            // do the announcing - need to decide which to prefer - read multiple?
                            Console.WriteLine(otherClassWarningData.ToString());
                            if (otherClassWarningData.numFasterCars > 1)
                            {
                                timeOfLastMultipleCarFasterClassWarning = currentGameState.Now;
                                this.driverNamesForFasterClassLastWarnedAbout = new HashSet<string>(otherClassWarningData.fasterCarDriverNames);
                                if (otherClassWarningData.fasterCarsIncludeClassLeader)
                                {
                                    if (otherClassWarningData.fasterCarsRacingForPosition)
                                    {
                                        Console.WriteLine("FASTER CARS BEHIND INCLUDING THE CLASS LEADER, THEY'RE RACING FOR POSITION");
                                        audioPlayer.playMessage(new QueuedMessage(folderFasterCarsFightingBehindIncludingClassLeader, 0, this));
                                    }
                                    else
                                    {
                                        Console.WriteLine("FASTER CARS BEHIND INCLUDING THE CLASS LEADER");
                                        audioPlayer.playMessage(new QueuedMessage(folderFasterCarsBehindIncludingClassLeader, 0, this));
                                    }
                                }
                                else if (otherClassWarningData.fasterCarsRacingForPosition)
                                {
                                    Console.WriteLine("FASTER CARS BEHIND, THEY'RE RACING EACH OTHER FOR POSITION");
                                    audioPlayer.playMessage(new QueuedMessage(folderFasterCarsBehindFighting, 0, this));
                                }
                                else
                                {
                                    Console.WriteLine("FASTER CARS BEHIND");
                                    audioPlayer.playMessage(new QueuedMessage(folderFasterCarsBehind, 0, this));
                                }
                                // don't bother with 'no blue flag' warning here - this only really makes sense if all the 
                                // cars in the group are racing the player for position. Do we need to fix this in the OtherCarClassWarningData data?
                            }
                            else if (otherClassWarningData.numFasterCars == 1)
                            {
                                timeOfLastSingleCarFasterClassWarning = currentGameState.Now;
                                this.driverNamesForFasterClassLastWarnedAbout = new HashSet<string>(otherClassWarningData.fasterCarDriverNames);
                                if (otherClassWarningData.fasterCarsIncludeClassLeader)
                                {
                                    if (otherClassWarningData.fasterCarIsRacingPlayerForPosition)
                                    {
                                        Console.WriteLine("LEADER FROM FASTER CLASS RACING PLAYER FOR POSITION BEHIND - NO BLUE FLAG");
                                        audioPlayer.playMessage(new QueuedMessage(folderFasterCarBehindRacingPlayerForPositionIsClassLeader, 0, this));
                                    }
                                    else
                                    {
                                        Console.WriteLine("LEADER FROM FASTER CLASS BEHIND");
                                        audioPlayer.playMessage(new QueuedMessage(folderFasterCarBehindIsClassLeader, 0, this));
                                    }
                                }
                                else if (otherClassWarningData.fasterCarIsRacingPlayerForPosition)
                                {
                                    Console.WriteLine("FASTER CAR RACING PLAYER FOR POSITION BEHIND - NO BLUE FLAG");
                                    audioPlayer.playMessage(new QueuedMessage(folderFasterCarBehindRacingPlayerForPosition, 0, this));
                                }
                                else
                                {
                                    Console.WriteLine("FASTER CAR BEHIND");
                                    audioPlayer.playMessage(new QueuedMessage(folderFasterCarBehind, 0, this));
                                }
                            }
                            if (otherClassWarningData.numSlowerCars > 1)
                            {
                                timeOfLastMultipleCarSlowerClassWarning = currentGameState.Now;
                                this.driverNamesForSlowerClassLastWarnedAbout = new HashSet<string>(otherClassWarningData.slowerCarDriverNames);
                                if (otherClassWarningData.slowerCarsIncludeClassLeader)
                                {
                                    if (otherClassWarningData.slowerCarsRacingForPosition)
                                    {
                                        Console.WriteLine("SLOWER CARS AHEAD INCLUDING THE CLASS LEADER, THEY'RE RACING FOR POSITION");
                                        audioPlayer.playMessage(new QueuedMessage(folderSlowerCarsFightingAheadIncludingClassLeader, 0, this));
                                    }
                                    else
                                    {
                                        Console.WriteLine("SLOWER CARS AHEAD INCLUDING THE CLASS LEADER");
                                        audioPlayer.playMessage(new QueuedMessage(folderSlowerCarsAheadIncludingClassLeader, 0, this));
                                    }
                                }
                                else if (otherClassWarningData.slowerCarsRacingForPosition)
                                {
                                    Console.WriteLine("SLOWER CARS AHEAD, THEY'RE RACING EACH OTHER FOR POSITION");
                                    audioPlayer.playMessage(new QueuedMessage(folderSlowerCarsAheadFighting, 0, this));
                                }
                                else
                                {
                                    Console.WriteLine("SLOWER CARS AHEAD");
                                    audioPlayer.playMessage(new QueuedMessage(folderSlowerCarsAhead, 0, this));
                                }
                                // don't bother with 'no blue flag' warning here - this only really makes sense if all the 
                                // cars in the group are racing the player for position. Do we need to fix this in the OtherCarClassWarningData data?
                            }
                            else if (otherClassWarningData.numSlowerCars == 1)
                            {
                                timeOfLastSingleCarSlowerClassWarning = currentGameState.Now;
                                this.driverNamesForSlowerClassLastWarnedAbout = new HashSet<string>(otherClassWarningData.slowerCarDriverNames);
                                if (otherClassWarningData.slowerCarsIncludeClassLeader)
                                {
                                    if (otherClassWarningData.slowerCarIsRacingPlayerForPosition)
                                    {
                                        Console.WriteLine("LEADER FROM SLOWER CLASS RACING PLAYER FOR POSITION AHEAD - NO BLUE FLAG");
                                        audioPlayer.playMessage(new QueuedMessage(folderSlowerCarAheadRacingPlayerForPositionIsClassLeader, 0, this));
                                    }
                                    else
                                    {
                                        Console.WriteLine("LEADER FROM SLOWER CLASS AHEAD");
                                        audioPlayer.playMessage(new QueuedMessage(folderSlowerCarAheadClassLeader, 0, this));
                                    }
                                }
                                else if (otherClassWarningData.slowerCarIsRacingPlayerForPosition)
                                {
                                    Console.WriteLine("SLOWER CAR RACING PLAYER FOR POSITION AHEAD - NO BLUE FLAG");
                                    audioPlayer.playMessage(new QueuedMessage(folderSlowerCarAheadRacingPlayerForPosition, 0, this));
                                }
                                else
                                {
                                    Console.WriteLine("SLOWER CAR AHEAD");
                                    audioPlayer.playMessage(new QueuedMessage(folderSlowerCarAhead, 0, this));
                                }
                            }
                        }
                        // now wait a while before checking again - how long should we wait here? What if the 'canAnnounce' check fails?
                        nextCheckForOtherCarClasses = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                    }
                }
                else
                {
                    nextCheckForOtherCarClasses = currentGameState.Now.Add(timeBetweenOtherClassChecks);
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            Boolean gotResponse = false;
            if (SpeechRecogniser.WHAT_CLASS_IS_CAR_AHEAD.Contains(voiceMessage))
            {
                String opponentKey = currentGameState.getOpponentKeyInFrontOnTrack();
                if (opponentKey != null)
                {
                    gotResponse = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage(getClassSound(currentGameState.OpponentData[opponentKey]), 0, this));
                }
            }
            else if (SpeechRecogniser.WHAT_CLASS_IS_CAR_BEHIND.Contains(voiceMessage))
            {                
                String opponentKey = currentGameState.getOpponentKeyBehindOnTrack();
                if (opponentKey != null)
                {
                    gotResponse = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage(getClassSound(currentGameState.OpponentData[opponentKey]), 0, this));
                }
            }
            else if (SpeechRecogniser.IS_CAR_AHEAD_MY_CLASS.Contains(voiceMessage))
            {
                String opponentKey = currentGameState.getOpponentKeyInFrontOnTrack();
                if (opponentKey != null)
                {
                    gotResponse = true;
                    CarData.CarClass opponentClass = currentGameState.OpponentData[opponentKey].CarClass;
                    if (CarData.IsCarClassEqual(currentGameState.carClass, opponentClass))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderYes, 0, this));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNo, 0, this));
                    }
                }
            }
            else if (SpeechRecogniser.IS_CAR_BEHIND_MY_CLASS.Contains(voiceMessage))
            {
                String opponentKey = currentGameState.getOpponentKeyBehindOnTrack();
                if (opponentKey != null)
                {
                    gotResponse = true;
                    CarData.CarClass opponentClass = currentGameState.OpponentData[opponentKey].CarClass;
                    if (CarData.IsCarClassEqual(currentGameState.carClass, opponentClass))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderYes, 0, this));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNo, 0, this));
                    }
                }
            }
            if (!gotResponse)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this));
            }
        }

        private String getClassSound(OpponentData opponentToCheck)
        {
            CarData.CarClass opponentClass = opponentToCheck.CarClass;
            if (CarData.IsCarClassEqual(opponentClass, currentGameState.carClass))
            {
                return folderSameClassAsUs;
            }
            String classSound;
            if (carClassEnumToSound.TryGetValue(opponentClass.carClassEnum, out classSound) && SoundCache.availableSounds.Contains(classSound))
            {
                return classSound;
            }
            else
            {
                ClassSpeedComparison opponentComparisonToPlayer = getClassSpeedComparisonToPlayer(opponentClass);
                if (opponentComparisonToPlayer == ClassSpeedComparison.FASTER)
                {
                    return folderFasterClass;
                }
                else if (opponentComparisonToPlayer == ClassSpeedComparison.SLOWER)
                {
                    return folderSlowerClass;
                }
            }
            return AudioPlayer.folderNoData;
        }

        private class OtherCarClassWarningData
        {
            public int numFasterCars;
            public int numSlowerCars;
            public Boolean fasterCarsIncludeClassLeader;
            public Boolean slowerCarsIncludeClassLeader;
            public Boolean fasterCarsRacingForPosition;
            public Boolean slowerCarsRacingForPosition;
            public Boolean fasterCarIsRacingPlayerForPosition = false;
            public Boolean slowerCarIsRacingPlayerForPosition = false;
            public HashSet<string> fasterCarDriverNames;
            public HashSet<string> slowerCarDriverNames;

            public OtherCarClassWarningData(int numFasterCars, int numSlowerCars, Boolean fasterCarsIncludeClassLeader, Boolean slowerCarsIncludeClassLeader,
                Boolean fasterCarsRacingForPosition, Boolean slowerCarsRacingForPosition, Boolean fasterCarIsRacingPlayerForPosition, Boolean slowerCarIsRacingPlayerForPosition,
                HashSet<string> fasterCarDriverNames, HashSet<string> slowerCarDriverNames)
            {
                this.numFasterCars = numFasterCars;
                this.numSlowerCars = numSlowerCars;
                this.fasterCarsIncludeClassLeader = fasterCarsIncludeClassLeader;
                this.slowerCarsIncludeClassLeader = slowerCarsIncludeClassLeader;
                this.fasterCarsRacingForPosition = fasterCarsRacingForPosition;
                this.slowerCarsRacingForPosition = slowerCarsRacingForPosition;
                this.fasterCarIsRacingPlayerForPosition = fasterCarIsRacingPlayerForPosition;
                this.slowerCarIsRacingPlayerForPosition = slowerCarIsRacingPlayerForPosition;
                this.fasterCarDriverNames = fasterCarDriverNames;
                this.slowerCarDriverNames = slowerCarDriverNames;
            }

            // checks if the current warning data is consistent with the previous warning data and a bunch of other stuff
            // The goal here is to ensure we play significant warnings without spamming the player with messages
            public Boolean canAnnounce(OtherCarClassWarningData previousOtherCarClassWarningData, HashSet<string> fasterDriversAtLastAnnouncement,
                HashSet<string> slowerDriversAtLastAnnouncement, DateTime now,
                DateTime timeOfLastSingleCarFasterClassWarning, DateTime timeOfLastMultipleCarFasterClassWarning, 
                DateTime timeOfLastSingleCarSlowerClassWarning, DateTime timeOfLastMultipleCarSlowerClassWarning)
            {
                if ((this.numFasterCars > 0 && previousOtherCarClassWarningData.numFasterCars > 0) ||
                    (this.numSlowerCars > 0 && previousOtherCarClassWarningData.numSlowerCars > 0))
                {
                    Boolean hasNewFasterDriverToWarnAbout = fasterCarDriverNames.Except(fasterDriversAtLastAnnouncement).Any();
                    Boolean hasNewSlowerDriverToWarnAbout = slowerCarDriverNames.Except(slowerDriversAtLastAnnouncement).Any();

                    if (hasNewFasterDriverToWarnAbout || hasNewSlowerDriverToWarnAbout)
                    {
                        // try to estimate how important this warning might be
                        if (numSlowerCars == 1 &&
                            (slowerCarsIncludeClassLeader || slowerCarIsRacingPlayerForPosition ||
                                now > timeOfLastSingleCarSlowerClassWarning + TimeSpan.FromMinutes(2)))
                        {
                            return true;
                        }
                        else if (numSlowerCars > 1 &&
                            (slowerCarsIncludeClassLeader || slowerCarsRacingForPosition ||
                                now > timeOfLastMultipleCarSlowerClassWarning + TimeSpan.FromMinutes(2)))
                        {
                            return true;
                        }
                        else if (numFasterCars == 1 &&
                            (fasterCarsIncludeClassLeader || fasterCarIsRacingPlayerForPosition ||
                                now > timeOfLastSingleCarFasterClassWarning + TimeSpan.FromMinutes(2)))
                        {
                            return true;
                        }
                        else if (numFasterCars > 1 &&
                            (fasterCarsIncludeClassLeader || fasterCarsRacingForPosition ||
                                now > timeOfLastMultipleCarFasterClassWarning + TimeSpan.FromMinutes(2)))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return "num faster cars closing = " + numFasterCars + " faster cars battling for position = " +
                    fasterCarsRacingForPosition + " faster cars include class leader = " + fasterCarsIncludeClassLeader +
                    " num slower cars closing = " + numSlowerCars + " slower cars battling for position = " +
                    slowerCarsRacingForPosition + " slower cars include class leader = " + slowerCarsIncludeClassLeader +
                    " faster car is racing player for position = " + fasterCarIsRacingPlayerForPosition +
                    " slower car is racing player for position = " + slowerCarIsRacingPlayerForPosition + 
                    " fasterCarDriverNames = " + String.Join(", ", fasterCarDriverNames) +
                    " slowerCarDriverNames = " + String.Join(", ", slowerCarDriverNames);
            }
        }

        private void getBestTimesByClass(GameStateData currentGameState)
        {
            // this is used by the main thread and the speech recogniser thread, both of which may update it, so lock before using
            lock (this)
            {
                CarData.CarClass playerCarClass = currentGameState.carClass;
                float playerBestTime = currentGameState.SessionData.PlayerLapTimeSessionBest;
                this.bestTimesByClass.Clear();
                if (playerBestTime > 0)
                {
                    bestTimesByClass.Add(playerCarClass, playerBestTime);
                }
                foreach (OpponentData opponentData in currentGameState.OpponentData.Values)
                {
                    CarData.CarClass opponentCarClass = opponentData.CarClass;
                    float bestLap = opponentData.CurrentBestLapTime;
                    if (bestLap > 0)
                    {
                        Boolean foundInDictionary = false;
                        foreach (CarData.CarClass carClass in bestTimesByClass.Keys)
                        {
                            if (CarData.IsCarClassEqual(carClass, opponentCarClass))
                            {
                                foundInDictionary = true;
                                float existingBestTime = bestTimesByClass[carClass];
                                if (bestLap < existingBestTime)
                                {
                                    bestTimesByClass[carClass] = bestLap;
                                }
                                break;
                            }
                        }
                        if (!foundInDictionary)
                        {
                            bestTimesByClass.Add(opponentCarClass, bestLap);
                        }
                    }
                }
            }
        }

        // returns ClassSpeedComparison.FASTER if the opponent class is faster than the player's class
        private ClassSpeedComparison getClassSpeedComparisonToPlayer(CarData.CarClass opponentClass)
        {
            // work out if this opponent is faster or slower
            float playerClassBestLap = 0;
            float opponentClassBestLap = 0;
            Boolean gotOpponentClassBestLap = false;
            Boolean gotPlayerClassBestLap = false;
            // the getBestTimesByClass dictionary may be updated by another Thread at any point, so lock on this before using it
            lock (this)
            {
                if (bestTimesByClass.Count == 0)
                {
                    getBestTimesByClass(currentGameState);
                }
                foreach (CarData.CarClass carClass in bestTimesByClass.Keys)
                {
                    if (CarData.IsCarClassEqual(carClass, opponentClass))
                    {
                        opponentClassBestLap = bestTimesByClass[carClass];
                        gotOpponentClassBestLap = true;
                    }
                    else if (CarData.IsCarClassEqual(carClass, currentGameState.carClass))
                    {
                        playerClassBestLap = bestTimesByClass[carClass];
                        gotPlayerClassBestLap = true;
                    }
                    if (gotOpponentClassBestLap && gotPlayerClassBestLap)
                    {
                        break;
                    }
                }
                if (playerClassBestLap > 0 && opponentClassBestLap > 0)
                {
                    return playerClassBestLap > opponentClassBestLap ? ClassSpeedComparison.FASTER : ClassSpeedComparison.SLOWER;
                }
            }
            return ClassSpeedComparison.UNKNOWN;
        }

        // to be called every couple of seconds, not every tick
        private OtherCarClassWarningData getOtherCarClassWarningData(GameStateData currentGameState)
        {
            float playerDistanceRoundTrack = currentGameState.PositionAndMotionData.DistanceRoundTrack;
            float playerBestLap = currentGameState.SessionData.PlayerLapTimeSessionBest;
            float trackLength = currentGameState.SessionData.TrackDefinition.trackLength;
            float halfTrackLength = trackLength / 2f;

            int numFasterCars = 0;
            int numSlowerCars = 0;
            Dictionary<int, List<float>> fasterCarLapCountAndSeparations = new Dictionary<int, List<float>>();
            Dictionary<int, List<float>> slowerCarLapCountAndSeparations = new Dictionary<int, List<float>>();
            Boolean fasterCarsIncludeClassLeader = false;
            Boolean slowerCarsIncludeClassLeader = false;
            Boolean fasterCarsRacingForPosition = false;
            Boolean slowerCarsRacingForPosition = false;
            // these are edge cases - a faster car is immediately behind us on track and is immediately behind us overall,
            // or a slower car is immediately ahead of us on track and is immediately ahead of us overall
            Boolean fasterCarIsRacingPlayerForPosition = false;
            Boolean slowerCarIsRacingPlayerForPosition = false;
            HashSet<string> fasterCarDriverNames = new HashSet<string>();
            HashSet<string> slowerCarDriverNames = new HashSet<string>();

            foreach (OpponentData opponentData in currentGameState.OpponentData.Values)
            {
                if (CarData.IsCarClassEqual(opponentData.CarClass, currentGameState.carClass) ||
                    opponentData.CurrentBestLapTime <= 0 || opponentData.InPits || opponentData.DistanceRoundTrack <= 0 ||
                    opponentData.Speed < 5 || !opponentData.IsActive)
                {
                    continue;
                }
                ClassSpeedComparison classSpeedComparisonToPlayer = getClassSpeedComparisonToPlayer(opponentData.CarClass);
                if (classSpeedComparisonToPlayer == ClassSpeedComparison.UNKNOWN)
                {
                    continue;
                }
                // separation is +ve when the player is in front, -ve when he's behind
                float separation = playerDistanceRoundTrack - opponentData.DistanceRoundTrack;
                if (separation > halfTrackLength)
                {
                    separation = trackLength - separation;
                }
                else if (separation < -1 * halfTrackLength)
                {
                    separation = trackLength + separation;
                }
                if (classSpeedComparisonToPlayer == ClassSpeedComparison.FASTER && separation > fasterCarWarningZoneEnd && separation < fasterCarWarningZoneStart)
                {
                    // player is ahead of a faster class car
                    numFasterCars++;
                    fasterCarDriverNames.Add(opponentData.DriverRawName);
                    if (opponentData.ClassPosition == 1)
                    {
                        fasterCarsIncludeClassLeader = true;
                    }
                    if (opponentData.OverallPosition == currentGameState.SessionData.OverallPosition + 1)
                    {
                        fasterCarIsRacingPlayerForPosition = true;
                    }
                    // get the separations of other cars we've already processed which are on the same lap as this opponent:
                    if (!fasterCarsRacingForPosition)
                    {
                        List<float> separationsForOtherCarsOnSameLap;
                        if (fasterCarLapCountAndSeparations.TryGetValue(opponentData.CompletedLaps, out separationsForOtherCarsOnSameLap))
                        {
                            // if any of them are close to each other, set the boolean flag
                            foreach (float otherSeparation in separationsForOtherCarsOnSameLap)
                            {
                                if (Math.Abs(otherSeparation - separation) < 50)
                                {
                                    fasterCarsRacingForPosition = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            separationsForOtherCarsOnSameLap = new List<float>();
                            separationsForOtherCarsOnSameLap.Add(separation);
                            fasterCarLapCountAndSeparations.Add(opponentData.CompletedLaps, separationsForOtherCarsOnSameLap);
                        }
                    }
                }
                // this separation check looks odd because the separation value is negative (player is behind) so the < and > appear reversed
                else if (classSpeedComparisonToPlayer == ClassSpeedComparison.SLOWER && separation < slowerCarWarningZoneStart && separation > slowerCarWarningZoneEnd)
                {
                    // player is behind a slower class car
                    numSlowerCars++;
                    slowerCarDriverNames.Add(opponentData.DriverRawName);
                    if (opponentData.ClassPosition == 1)
                    {
                        slowerCarsIncludeClassLeader = true;
                    }
                    if (opponentData.OverallPosition == currentGameState.SessionData.OverallPosition - 1)
                    {
                        slowerCarIsRacingPlayerForPosition = true;
                    }
                    // get the separations of other cars we've already processed which are on the same lap as this opponent:
                    if (!slowerCarsRacingForPosition)
                    {
                        List<float> separationsForOtherCarsOnSameLap;
                        if (slowerCarLapCountAndSeparations.TryGetValue(opponentData.CompletedLaps, out separationsForOtherCarsOnSameLap))
                        {
                            // if any of them are close to each other, set the boolean flag
                            foreach (float otherSeparation in separationsForOtherCarsOnSameLap)
                            {
                                if (Math.Abs(otherSeparation - separation) < 50)
                                {
                                    slowerCarsRacingForPosition = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            separationsForOtherCarsOnSameLap = new List<float>();
                            separationsForOtherCarsOnSameLap.Add(separation);
                            slowerCarLapCountAndSeparations.Add(opponentData.CompletedLaps, separationsForOtherCarsOnSameLap);
                        }
                    }
                }
            }
            return new OtherCarClassWarningData(numFasterCars, numSlowerCars, fasterCarsIncludeClassLeader, slowerCarsIncludeClassLeader,
                fasterCarsRacingForPosition, slowerCarsRacingForPosition, fasterCarIsRacingPlayerForPosition, slowerCarIsRacingPlayerForPosition,
                fasterCarDriverNames, slowerCarDriverNames);
        }
    }
}
