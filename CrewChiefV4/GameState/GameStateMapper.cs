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
         */
        public virtual void correctForMulticlassPositions(GameStateData currentGameState)
        {
            if (currentGameState.SessionData.JustGoneGreen || currentGameState.SessionData.IsNewSession)
            {
                currentGameState.SessionData.SessionStartClassPosition = currentGameState.SessionData.ClassPosition;
            }
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.ClassPositionAtStartOfCurrentLap = currentGameState.SessionData.ClassPosition;
            }
            foreach (OpponentData opponent in currentGameState.OpponentData.Values)
            {
                if (opponent.CarClass != currentGameState.carClass || opponent.Position != opponent.ClassPosition)
                {
                    // don't care about other classes or cases where the position and class position match
                    continue;
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
    }
}
