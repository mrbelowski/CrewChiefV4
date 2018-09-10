using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class SessionEndMessages
    {
        public static String sessionEndMessageIdentifier = "SESSION_END";

        public static String folderPodiumFinish = "lap_counter/podium_finish";

        private String folderWonRace = "lap_counter/won_race";

        public static String folderFinishedRace = "lap_counter/finished_race";

        private String folderGoodFinish = "lap_counter/finished_race_good_finish";

        public static String folderFinishedRaceLast = "lap_counter/finished_race_last";

        private String folderEndOfSession = "lap_counter/end_of_session";

        private String folderEndOfSessionPole = "lap_counter/end_of_session_pole";

        private Boolean enableSessionEndMessages = UserSettings.GetUserSettings().getBoolean("enable_session_end_messages");

        private AudioPlayer audioPlayer;

        private int minSessionRunTimeForEndMessages = 60;

        private DateTime lastSessionEndMessagesPlayedAt = DateTime.MinValue;

        public SessionEndMessages(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public void trigger(float sessionRunningTime, SessionType sessionType, SessionPhase lastSessionPhase, int startPosition,
            int finishPosition, int numCars, int completedLaps, Boolean isDisqualified, Boolean isDNF, DateTime now)
        {
            if (!enableSessionEndMessages)
            {
                Console.WriteLine("Session end, position = " + finishPosition + ", session end messages are disabled");
                return;
            }
            if (lastSessionEndMessagesPlayedAt.AddSeconds(10) > now)
            {
                Console.WriteLine("Skipping duplicate session end message call - last call was " + (now - lastSessionEndMessagesPlayedAt).TotalSeconds.ToString("0.00") + " seconds ago");
                return;
            }
            if (sessionType == SessionType.Race)
            {
                if (sessionRunningTime >= minSessionRunTimeForEndMessages || completedLaps > 0)
                {
                    if (lastSessionPhase == SessionPhase.Finished)
                    {
                        // only play session end message for races if we've actually finished, not restarted
                        lastSessionEndMessagesPlayedAt = now;
                        playFinishMessage(sessionType, startPosition, finishPosition, numCars, isDisqualified, isDNF, completedLaps);
                    }
                    else
                    {
                        Console.WriteLine("Skipping race session end message because the previous phase wasn't Finished");
                    }
                }
                else
                {
                    Console.WriteLine("Skipping race session end message because it didn't run for a lap or " + minSessionRunTimeForEndMessages + " seconds");
                }
            }
            else if (sessionType == SessionType.Practice || sessionType == SessionType.Qualify)
            {
                if (sessionRunningTime >= minSessionRunTimeForEndMessages)
                {
                    if (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow || 
                        lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Checkered)
                    {
                        lastSessionEndMessagesPlayedAt = now;
                        playFinishMessage(sessionType, startPosition, finishPosition, numCars, false, isDNF, completedLaps);
                    }
                    else
                    {
                        Console.WriteLine("Skipping non-race session end message because the previous phase wasn't green, finished, or checkered");
                    }
                }
                else
                {
                    Console.WriteLine("Skipping non-race session end message because the session didn't run for " + minSessionRunTimeForEndMessages + " seconds");
                }
            }
        }

        public void playFinishMessage(SessionType sessionType, int startPosition, int position, int numCars, Boolean isDisqualified, Boolean isDNF, int completedLaps)
        {
            audioPlayer.suspendPearlsOfWisdom();
            if (position < 1)
            {
                Console.WriteLine("Session finished but position is < 1");
            }
            else if (sessionType == SessionType.Race)
            {
                Boolean isLast = position == numCars;
                if (isDisqualified) 
                {
                    Boolean playedRant = false;
                    if (completedLaps > 1)
                    {
                        playedRant = audioPlayer.playRant(sessionEndMessageIdentifier, AbstractEvent.MessageContents(Penalties.folderDisqualified));
                    }
                    if (!playedRant)
                    {
                        audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier, AbstractEvent.MessageContents(
                            Penalties.folderDisqualified), 0, null), 10);
                    }                    
                }
                else if (isDNF)
                {
                    audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier,
                        AbstractEvent.MessageContents(folderFinishedRaceLast), 0, null), 10);
                }
                else if (position == 1)
                {
                    audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier, AbstractEvent.MessageContents(
                        folderWonRace), 0, null), 10);
                }
                else if (position < 4)
                {
                    audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier, AbstractEvent.MessageContents(
                        folderPodiumFinish), 0, null), 10);
                }
                else if (position >= 4 && !isLast)
                {
                    // check if this a significant improvement over the start position
                    if (startPosition > position &&
                        ((startPosition <= 6 && position <= 5) ||
                         (startPosition <= 10 && position <= 6) ||
                         (startPosition - position >= 6)))
                    {
                        audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier, AbstractEvent.MessageContents(
                                Position.folderStub + position, folderGoodFinish), 0, null), 10);
                    }
                    else
                    {
                        // if it's a shit finish, maybe launch into a tirade
                        Boolean playedRant = false;
                        int positionsLost = position - startPosition;
                        // if we've lost 9 or more positions, and this is more than half the field size, maybe play a rant
                        if (numCars > 2 && completedLaps > 1 && positionsLost > 8 && (float)positionsLost / (float)numCars >= 0.5f)
                        {
                            playedRant = audioPlayer.playRant(sessionEndMessageIdentifier, AbstractEvent.MessageContents(Position.folderStub + position));
                        }
                        if (!playedRant)
                        {
                            audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier, AbstractEvent.MessageContents(
                                Position.folderStub + position, folderFinishedRace), 0, null), 10);
                        }
                    }
                }
                else if (isLast)
                {
                    Boolean playedRant = false;
                    if (numCars > 5 && completedLaps > 1)
                    {
                        playedRant = audioPlayer.playRant(sessionEndMessageIdentifier, AbstractEvent.MessageContents(Position.folderStub + position));
                    }
                    if (!playedRant)
                    {
                        audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier,
                            AbstractEvent.MessageContents(folderFinishedRaceLast), 0, null), 10);
                    }
                }
            }
            else
            {
                if (sessionType == SessionType.Qualify && position == 1)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderEndOfSessionPole, 0, null));
                }
                else
                {
                    audioPlayer.playMessage(new QueuedMessage(sessionEndMessageIdentifier, AbstractEvent.MessageContents(folderEndOfSession,
                    Position.folderStub + position), 0, null), 10);
                }
            }
        }
    }
}
