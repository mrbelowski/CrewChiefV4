using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.GameState
{
    public abstract class GameStateMapper
    {
        protected SpeechRecogniser speechRecogniser;

        /** May return null if the game state raw data is considered invalid */
        public abstract GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState);

        public abstract void versionCheck(Object memoryMappedFileStruct);

        public abstract SessionType mapToSessionType(Object memoryMappedFileStruct);

        public virtual void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        // called after mapping - 
        //
        // need to correct as many of the following as we can:
        /*
            currentGameState.SessionData.SessionStartPosition
            currentGameState.SessionData.PositionAtStartOfCurrentLap
            currentGameState.SessionData.LeaderSectorNumber
            currentGameState.PitData.LeaderIsPitting
            currentGameState.PitData.OpponentForLeaderPitting
            currentGameState.PitData.CarInFrontIsPitting
            currentGameState.PitData.OpponentForCarAheadPitting
            opponentData.PositionOnApproachToPitEntry
            currentGameState.SessionData.TimeDeltaBehind
            currentGameState.SessionData.TimeDeltaFront
          
            --- don't think we can get this one ---
            currentGameState.SessionData.HasLeadChanged
         */
        // This method populates data derived from the mapped data, that's common to all games. Currently this is only
        // for fixing multiclass data but may be extended to tidy up some of the copy-paste chaos in the mappers.
        public virtual void populateDerivedData(GameStateData currentGameState, GameStateData previousGameState)
        {
            Boolean singleClass = GameStateData.NumberOfClasses == 1 || GameStateData.forceSingleClass(currentGameState);
            // always set the session start class position and lap start class position:
            if (currentGameState.SessionData.JustGoneGreen || currentGameState.SessionData.IsNewSession)
            {
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

            // if we're single class, no need to process the opponents data
            if (singleClass)
            {
                currentGameState.SessionData.NumCarsInPlayerClass = currentGameState.SessionData.NumCarsOverall;
                return;
            }
            int numCarsInPlayerClass = 1;
            float PitApproachPosition = currentGameState.SessionData.TrackDefinition != null ? currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks : -1;

            // have to re-calculate these for multiclass:
            currentGameState.SessionData.IsRacingSameCarBehind = previousGameState != null &&
                previousGameState.getOpponentKeyBehind(currentGameState.carClass) == currentGameState.getOpponentKeyBehind(currentGameState.carClass);
            currentGameState.SessionData.IsRacingSameCarInFront = previousGameState != null &&
                previousGameState.getOpponentKeyInFront(currentGameState.carClass) == currentGameState.getOpponentKeyInFront(currentGameState.carClass);
            if (!currentGameState.SessionData.IsRacingSameCarInFront)
            {
                currentGameState.SessionData.GameTimeAtLastPositionFrontChange = currentGameState.SessionData.SessionRunningTime;
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind)
            {
                currentGameState.SessionData.GameTimeAtLastPositionBehindChange = currentGameState.SessionData.SessionRunningTime;
            }

            foreach (OpponentData opponent in currentGameState.OpponentData.Values)
            {                
                if (opponent.CarClass == currentGameState.carClass)
                {
                    numCarsInPlayerClass++;
                    // don't care about other classes
                    if (opponent.OverallPosition == opponent.ClassPosition)
                    {
                        // don't care about opponents who's class position matches their overall position
                        continue;
                    }
                    if (PitApproachPosition != -1
                        && opponent.DistanceRoundTrack < PitApproachPosition + 20
                        && opponent.DistanceRoundTrack > PitApproachPosition - 20)
                    {
                        opponent.PositionOnApproachToPitEntry = opponent.ClassPosition;
                    }
                    if (opponent.ClassPosition == 1)
                    {
                        currentGameState.SessionData.LeaderSectorNumber = opponent.CurrentSectorNumber;
                        if (opponent.JustEnteredPits)
                        {
                            currentGameState.PitData.LeaderIsPitting = true;
                            currentGameState.PitData.OpponentForLeaderPitting = opponent;
                        }
                        else
                        {
                            currentGameState.PitData.LeaderIsPitting = false;
                            currentGameState.PitData.OpponentForLeaderPitting = null;
                        }
                    }
                    if (opponent.ClassPosition == currentGameState.SessionData.ClassPosition - 1)
                    {
                        currentGameState.SessionData.TimeDeltaFront = opponent.DeltaTime.GetAbsoluteTimeDeltaAllowingForLapDifferences(currentGameState.SessionData.DeltaTime);
                        if (opponent.JustEnteredPits)
                        {
                            currentGameState.PitData.CarInFrontIsPitting = true;
                            currentGameState.PitData.OpponentForCarAheadPitting = opponent;
                        }
                        else
                        {
                            currentGameState.PitData.CarInFrontIsPitting = false;
                            currentGameState.PitData.OpponentForCarAheadPitting = null;
                        }
                    }
                    if (opponent.ClassPosition == currentGameState.SessionData.ClassPosition + 1)
                    {
                        currentGameState.SessionData.TimeDeltaBehind = opponent.DeltaTime.GetAbsoluteTimeDeltaAllowingForLapDifferences(currentGameState.SessionData.DeltaTime);
                        if (opponent.JustEnteredPits)
                        {
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
            if (currentGameState.SessionData.JustGoneGreen || currentGameState.SessionData.IsNewSession)
            {
                currentGameState.SessionData.NumCarsInPlayerClassAtStartOfSession = numCarsInPlayerClass;
            }
            currentGameState.SessionData.NumCarsInPlayerClass = numCarsInPlayerClass;
        }
    }
}
