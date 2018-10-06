using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.GameState
{
    public abstract class GameStateMapper
    {
        protected SpeechRecogniser speechRecogniser;

        // in race sessions, delay position changes to allow things to settle. This is game-dependent
        private Dictionary<string, PendingRacePositionChange> PendingRacePositionChanges = new Dictionary<string, PendingRacePositionChange>();
        private TimeSpan PositionChangeLag = TimeSpan.FromMilliseconds(1000);
        class PendingRacePositionChange
        {
            public int newPosition;
            public DateTime positionSettledTime;
            public PendingRacePositionChange(int newPosition, DateTime positionSettledTime)
            {
                this.newPosition = newPosition;
                this.positionSettledTime = positionSettledTime;
            }
        }

        /** May return null if the game state raw data is considered invalid */
        public abstract GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState);

        public abstract void versionCheck(Object memoryMappedFileStruct);

        private DateTime nextOpponentBehindPitMessageDue = DateTime.MinValue;
        private DateTime nextOpponentAheadPitMessageDue = DateTime.MinValue;
        private DateTime nextLeaderPitMessageDue = DateTime.MinValue;

        public virtual void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        // This method populates data derived from the mapped data, that's common to all games.
        // This is specific to race sessions. At the time of writing there's nothing needed for qual and
        // practice, but these may be added later
        public virtual void populateDerivedRaceSessionData(GameStateData currentGameState)
        {
            Boolean singleClass = GameStateData.NumberOfClasses == 1 || CrewChief.forceSingleClass;
            // always set the session start class position and lap start class position:
            if (currentGameState.SessionData.JustGoneGreen || currentGameState.SessionData.IsNewSession)
            {
                // NOTE: on new session, ClassPosition in rF2 is not correct.  It is updated with a bit of a delay.
                // Since this code triggers on JustGoneGreen as well, this is corrected at that point, but I am not yet sure
                // there are no bad side effects.
                currentGameState.SessionData.SessionStartClassPosition = currentGameState.SessionData.ClassPosition;
                if (singleClass)
                {
                    currentGameState.SessionData.NumCarsInPlayerClassAtStartOfSession = currentGameState.SessionData.NumCarsOverallAtStartOfSession;
                }
            }
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.ClassPositionAtStartOfCurrentLap = currentGameState.SessionData.ClassPosition;
            }

            float PitApproachPosition = currentGameState.SessionData.TrackDefinition != null ? currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks : -1;
            
            // sort out all the class position stuff
            int numCarsInPlayerClass = 1;
            int leaderLapsCompleted = currentGameState.SessionData.CompletedLaps;
            foreach (OpponentData opponent in currentGameState.OpponentData.Values)
            {
                if (!opponent.IsActive)
                {
                    continue;
                }
                if (opponent.OverallPosition == 1)
                {
                    leaderLapsCompleted = opponent.CompletedLaps;
                }
                if (singleClass || CarData.IsCarClassEqual(opponent.CarClass, currentGameState.carClass))
                {
                    // special hack for iRacing until we finally kill those bastard opponent pitting spam messages
                    int minSecondsBetweenOpponentPitMessages = CrewChief.gameDefinition.gameEnum == GameEnum.IRACING ? 40 : 20;
                    // don't care about other classes
                    numCarsInPlayerClass++;
                    if (PitApproachPosition != -1
                        && opponent.DistanceRoundTrack < PitApproachPosition + 20
                        && opponent.DistanceRoundTrack > PitApproachPosition - 20)
                    {
                        opponent.PositionOnApproachToPitEntry = opponent.ClassPosition;
                    }
                    if (opponent.ClassPosition == 1)
                    {
                        if (opponent.ClassPositionAtPreviousTick != 1)
                        {
                            currentGameState.SessionData.HasLeadChanged = true;
                        }
                        currentGameState.SessionData.LeaderSectorNumber = opponent.CurrentSectorNumber;
                        if (opponent.JustEnteredPits && currentGameState.Now > nextLeaderPitMessageDue)
                        {
                            nextLeaderPitMessageDue = currentGameState.Now.AddSeconds(minSecondsBetweenOpponentPitMessages);
                            currentGameState.PitData.LeaderIsPitting = true;
                            currentGameState.PitData.OpponentForLeaderPitting = opponent;
                        }
                        else
                        {
                            currentGameState.PitData.LeaderIsPitting = false;
                            currentGameState.PitData.OpponentForLeaderPitting = null;
                        }
                    }
                    else if (opponent.ClassPosition == currentGameState.SessionData.ClassPosition - 1)
                    {
                        var useDerivedDeltas = true;
                        if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2 || CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT || CrewChief.gameDefinition.gameEnum == GameEnum.RF1)
                        {
                            // special case for R3E, RF1, RF2 and PCars2 - gap ahead is provided by the game - use these 
                            // (already set in the mapper) if the opponent is on the same lap
                            var lapDifference = opponent.DeltaTime.GetSignedLapDifference(currentGameState.SessionData.DeltaTime);
                            if (lapDifference == 0)
                            {
                                currentGameState.SessionData.LapsDeltaFront = 0;
                                useDerivedDeltas = false;
                            }
                        }
                        if (useDerivedDeltas)
                        {
                            var timeDelta = opponent.DeltaTime.GetAbsoluteTimeDeltaAllowingForLapDifferences(currentGameState.SessionData.DeltaTime);
                            currentGameState.SessionData.LapsDeltaFront = timeDelta.Item1;
                            currentGameState.SessionData.TimeDeltaFront = timeDelta.Item2;
                        }
                        if (opponent.JustEnteredPits && currentGameState.Now > nextOpponentAheadPitMessageDue)
                        {
                            nextOpponentAheadPitMessageDue = currentGameState.Now.AddSeconds(minSecondsBetweenOpponentPitMessages);
                            currentGameState.PitData.CarInFrontIsPitting = true;
                            currentGameState.PitData.OpponentForCarAheadPitting = opponent;
                        }
                        else
                        {
                            currentGameState.PitData.CarInFrontIsPitting = false;
                            currentGameState.PitData.OpponentForCarAheadPitting = null;
                        }
                    }
                    else if (opponent.ClassPosition == currentGameState.SessionData.ClassPosition + 1)
                    {
                        var useDerivedDeltas = true;
                        if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2 || CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM)
                        {
                            // special case for R3E and PCars2 - gap behind is provided by the game - use these 
                            // (already set in the mapper) if the opponent is on the same lap
                            var lapDifference = opponent.DeltaTime.GetSignedLapDifference(currentGameState.SessionData.DeltaTime);
                            if (lapDifference == 0)
                            {
                                currentGameState.SessionData.LapsDeltaBehind = 0;
                                useDerivedDeltas = false;
                            }
                        }
                        if (useDerivedDeltas)
                        {
                            var timeDelta = opponent.DeltaTime.GetAbsoluteTimeDeltaAllowingForLapDifferences(currentGameState.SessionData.DeltaTime);
                            currentGameState.SessionData.LapsDeltaBehind = timeDelta.Item1;
                            currentGameState.SessionData.TimeDeltaBehind = timeDelta.Item2;
                        }
                        if (opponent.JustEnteredPits && currentGameState.Now > nextOpponentBehindPitMessageDue)
                        {
                            nextOpponentBehindPitMessageDue = currentGameState.Now.AddSeconds(minSecondsBetweenOpponentPitMessages);
                            currentGameState.PitData.CarBehindIsPitting = true;
                            currentGameState.PitData.OpponentForCarBehindPitting = opponent;
                        }
                        else
                        {
                            currentGameState.PitData.CarBehindIsPitting = false;
                            currentGameState.PitData.OpponentForCarBehindPitting = null;
                        }
                    }
                }
            }
            if (!currentGameState.SessionData.SessionHasFixedTime)
            {
                // work out the laps remaining
                currentGameState.SessionData.SessionLapsRemaining = currentGameState.SessionData.SessionNumberOfLaps - leaderLapsCompleted;
            }
            
            if (currentGameState.SessionData.JustGoneGreen || currentGameState.SessionData.IsNewSession)
            {
                currentGameState.SessionData.NumCarsInPlayerClassAtStartOfSession = numCarsInPlayerClass;
            }
            currentGameState.SessionData.NumCarsInPlayerClass = numCarsInPlayerClass;
            
            // now derive some stuff that we always need to derive, using the correct class positions:
            String previousBehindKey = currentGameState.getOpponentKeyBehind(currentGameState.carClass, true);
            String currentBehindKey = currentGameState.getOpponentKeyBehind(currentGameState.carClass);
            String previousAheadKey = currentGameState.getOpponentKeyInFront(currentGameState.carClass, true);
            String currentAheadKey = currentGameState.getOpponentKeyInFront(currentGameState.carClass);
            if (currentGameState.SessionData.ClassPosition > 2)
            {
                // if we're first or second we don't care about lead changes
                String previousLeaderKey = currentGameState.getOpponentKeyAtClassPosition(1, currentGameState.carClass, true);
                String currentLeaderKey = currentGameState.getOpponentKeyAtClassPosition(1, currentGameState.carClass);                
            }
            // don't really need this any more:
            /*
            if (CrewChief.Debugging 
                && ((currentBehindKey == null && currentGameState.SessionData.ClassPosition < currentGameState.SessionData.NumCarsInPlayerClass)
                      || (currentAheadKey == null && currentGameState.SessionData.ClassPosition > 1)))
            {
                Console.WriteLine("Non-contiguous class positions");
                List<OpponentData> opponentsInClass = new List<OpponentData>();
                foreach (OpponentData opponent in currentGameState.OpponentData.Values)
                {
                    if (CarData.IsCarClassEqual(opponent.CarClass, currentGameState.carClass))
                    {
                        opponentsInClass.Add(opponent);
                    }
                    Console.WriteLine(String.Join("\n", opponentsInClass.OrderBy(o => o.ClassPosition)));
                }
            }*/

            currentGameState.SessionData.IsRacingSameCarBehind = currentBehindKey == previousBehindKey;
            currentGameState.SessionData.IsRacingSameCarInFront = currentAheadKey == previousAheadKey;
            if (!currentGameState.SessionData.IsRacingSameCarInFront)
            {
                currentGameState.SessionData.GameTimeAtLastPositionFrontChange = currentGameState.SessionData.SessionRunningTime;
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind)
            {
                currentGameState.SessionData.GameTimeAtLastPositionBehindChange = currentGameState.SessionData.SessionRunningTime;
            }
        }


        // so far, only the laps remaining is populated for non-race sessions. This is a bit of an edge case anyway - non-race sessions
        // with fixed number of laps is an American thing really.
        public virtual void populateDerivedNonRaceSessionData(GameStateData currentGameState)
        {
            if (!currentGameState.SessionData.SessionHasFixedTime)
            {
                currentGameState.SessionData.SessionLapsRemaining = currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps;
            }
        }

        // filters race position changes by delaying them a short time to prevent bouncing and noise interferring with event logic
        protected int getRacePosition(String driverName, int oldPosition, int newPosition, DateTime now)
        {
            if (driverName == null || oldPosition < 1)
            {
                return newPosition;
            }
            if (newPosition < 1 && CrewChief.gameDefinition.gameEnum != GameEnum.IRACING)
            { 
                Console.WriteLine("Can't update position to " + newPosition);
                return oldPosition;
            }
            PendingRacePositionChange pendingRacePositionChange = null;
            if (oldPosition == newPosition)
            {
                // clear any pending position change
                PendingRacePositionChanges.Remove(driverName);
                return oldPosition;
            }
            else if (PendingRacePositionChanges.TryGetValue(driverName, out pendingRacePositionChange))
            {
                if (newPosition == pendingRacePositionChange.newPosition)
                {
                    // the game is still reporting this driver is in the same race position, see if it's been long enough...
                    if (now > pendingRacePositionChange.positionSettledTime)
                    {
                        int positionToReturn = newPosition;
                        PendingRacePositionChanges.Remove(driverName);
                        return positionToReturn;
                    }
                    else
                    {
                        return oldPosition;
                    }
                }
                else
                {
                    // the new position is not consistent with the pending position change, bit of an edge case here
                    pendingRacePositionChange.newPosition = newPosition;
                    pendingRacePositionChange.positionSettledTime = now + PositionChangeLag;
                    return oldPosition;
                }
            }
            else
            {
                PendingRacePositionChanges.Add(driverName, new PendingRacePositionChange(newPosition, now + PositionChangeLag));
                return oldPosition;
            }
        }
    }
}
