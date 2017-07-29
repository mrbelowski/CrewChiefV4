using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using System.IO;

namespace CrewChiefV4
{
    class PreviousPositionAndVelocityData {
        public float xPosition;
        public float zPosition;
        public float xSpeed = 0;
        public float zSpeed = 0;
        public DateTime timeWhenLastUpdated;

        public PreviousPositionAndVelocityData(float xPosition, float zPosition, DateTime timeWhenLastUpdated)
        {
            this.xPosition = xPosition;
            this.zPosition = zPosition;
            this.timeWhenLastUpdated = timeWhenLastUpdated;
        }
    }

    class NoisyCartesianCoordinateSpotter
    {
        private float calculateOpponentSpeedsEvery = 200f;

        private float carBehindExtraLength = 0.4f;

        // if the audio player is in the middle of another message, this 'immediate' message will have to wait.
        // If it's older than 2000 milliseconds by the time the player's got round to playing it, it's expired
        private int clearMessageExpiresAfter = 2000;
        private int clearAllRoundMessageExpiresAfter = 2000;
        private int holdMessageExpiresAfter = 1000;
        private int inTheMiddleMessageExpiresAfter = 1000;

        private float carLength;

        // before saying 'clear', we need to be carLength + this value from the other car
        private float gapNeededForClear = UserSettings.GetUserSettings().getFloat("spotter_gap_for_clear");

        private float longCarLength;
        
        // don't play spotter messages if we're going < 10ms
        private float minSpeedForSpotterToOperate = UserSettings.GetUserSettings().getFloat("min_speed_for_spotter");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        // say "still there" every 3 seconds
        private TimeSpan repeatHoldFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("spotter_hold_repeat_frequency"));
        // use half the 'still there' wait if we're bouncing between clear and overlap
        private TimeSpan bouncingWait = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("spotter_hold_repeat_frequency") / 2);

        // this is the delay between saying "car left" then "3 wide, you're on the right"
        private TimeSpan onSingleOverlapTo3WideDelay = TimeSpan.FromSeconds(0.5);

        private int carsOnLeftAtPreviousTick;
        private int carsOnRightAtPreviousTick;

        private float trackZoneToConsider = 20f;
        private float carWidth;
        private float maxClosingSpeed = UserSettings.GetUserSettings().getFloat("max_closing_speed_for_spotter");

        private static String folderStillThere = "spotter/still_there";
        private static String folderInTheMiddle = "spotter/in_the_middle";
        private static String folderCarLeft = "spotter/car_left";
        private static String folderCarRight = "spotter/car_right";
        private static String folderCarInside = "spotter/car_inside";
        private static String folderCarOutside = "spotter/car_outside";
        private static String folderClearLeft = "spotter/clear_left";
        private static String folderClearRight = "spotter/clear_right";
        private static String folderClearInside = "spotter/clear_inside";
        private static String folderClearOutside = "spotter/clear_outside";
        private static String folderClearAllRound = "spotter/clear_all_round";
        private static String folderThreeWideYoureOnRight = "spotter/three_wide_on_right";
        private static String folderThreeWideYoureOnLeft = "spotter/three_wide_on_left";
        private static String folderThreeWideYoureOnInside = "spotter/three_wide_on_inside";
        private static String folderThreeWideYoureOnOutside = "spotter/three_wide_on_outside";

        private static String spotterFolderPrefix = "spotter_";
        public static String defaultSpotterId = "Jim (default)";
        public static List<String> availableSpotters = new List<String>();

        public static Boolean hasOvalSpecificSounds = false;

        private Random random = new Random();

        /**
         * static constructor to initialise spotter subfolder stuff.
         * 
         */
        static NoisyCartesianCoordinateSpotter()
        {
            availableSpotters.Clear();
            availableSpotters.Add(defaultSpotterId);
            try
            {
                DirectoryInfo soundsDirectory = new DirectoryInfo(AudioPlayer.soundFilesPath + "/voice");
                DirectoryInfo[] directories = soundsDirectory.GetDirectories();
                foreach (DirectoryInfo folder in directories)
                {
                    if (folder.Name.StartsWith(spotterFolderPrefix) && folder.Name.Length > spotterFolderPrefix.Length)
                    {
                        availableSpotters.Add(folder.Name.Substring(spotterFolderPrefix.Length));
                    }
                }
                String selectedSpotter = UserSettings.GetUserSettings().getString("spotter_name");                
                // TODO: select boxes and UI stuff - this may change
                if (!defaultSpotterId.Equals(selectedSpotter))
                {
                    if (Directory.Exists(AudioPlayer.soundFilesPath + "/voice/spotter_" + selectedSpotter))
                    {
                        Console.WriteLine("Using spotter " + selectedSpotter);
                        folderStillThere = "spotter_" + selectedSpotter + "/still_there";
                        folderInTheMiddle = "spotter_" + selectedSpotter + "/in_the_middle";
                        folderCarLeft = "spotter_" + selectedSpotter + "/car_left";
                        folderCarRight = "spotter_" + selectedSpotter + "/car_right";
                        folderClearLeft = "spotter_" + selectedSpotter + "/clear_left";
                        folderClearRight = "spotter_" + selectedSpotter + "/clear_right";
                        folderClearAllRound = "spotter_" + selectedSpotter + "/clear_all_round";
                        folderThreeWideYoureOnRight = "spotter_" + selectedSpotter + "/three_wide_on_right";
                        folderThreeWideYoureOnLeft = "spotter_" + selectedSpotter + "/three_wide_on_left";
                        folderCarInside = "spotter_" + selectedSpotter + "/car_inside";
                        folderCarOutside = "spotter_" + selectedSpotter + "/car_outside";
                        folderClearInside = "spotter_" + selectedSpotter + "/clear_inside";
                        folderClearOutside = "spotter_" + selectedSpotter + "/clear_outside";
                        folderThreeWideYoureOnInside = "spotter_" + selectedSpotter + "/three_wide_on_inside";
                        folderThreeWideYoureOnOutside = "spotter_" + selectedSpotter + "/three_wide_on_outside";
                    }
                    else
                    {
                        Console.WriteLine("No spotter called " + selectedSpotter + " exists, dropping back to the default (Jim)");
                        UserSettings.GetUserSettings().setProperty("spotter_name", defaultSpotterId);
                        UserSettings.GetUserSettings().saveUserSettings();                        
                    }
                }
                // check the oval specific stuff exists before enabling it:
                hasOvalSpecificSounds = Directory.Exists(AudioPlayer.soundFilesPath + "/voice/" + folderCarInside);                
                if (hasOvalSpecificSounds) 
                {
                    Console.WriteLine("Spotter " + selectedSpotter + " has oval-specific sounds - these will be used for tracks marked as 'oval'");
                }
                else
                {
                    Console.WriteLine("Spotter " + selectedSpotter + " has no oval-specific sounds");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("No sound folders available");
            }
        }

        // don't play 'clear' or 'hold' messages unless we've actually been clear or overlapping for some time
        private TimeSpan clearMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_clear_delay"));
        private TimeSpan overlapMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_overlap_delay"));
        private static Boolean use3WideLeftAndRight = UserSettings.GetUserSettings().getBoolean("spotter_enable_three_wide_left_and_right");

        private DateTime nextMessageDue = DateTime.Now;
        
        private DateTime timeWhenChannelShouldBeClosed;

        private TimeSpan timeToWaitBeforeClosingChannelLeftOpen = TimeSpan.FromMilliseconds(5000);

        private Boolean channelLeftOpenTimerStarted = false;
        
        private AudioPlayer audioPlayer;

        private NextMessageType nextMessageType;

        private Boolean reportedSingleOverlapLeft = false;

        private Boolean reportedSingleOverlapRight = false; 
        
        private Boolean reportedDoubleOverlapLeft = false;

        private Boolean reportedDoubleOverlapRight = false;

        private Boolean wasInMiddle = false;

        private static int maxOverlapsPerSide = use3WideLeftAndRight ? 2 : 1;

        private enum Side {
            right, left, none
        }

        private Dictionary<int, PreviousPositionAndVelocityData> previousPositionAndVelocityData = new Dictionary<int, PreviousPositionAndVelocityData>();

        private enum NextMessageType
        {
            none, clearLeft, clearRight, clearAllRound, carLeft, carRight, threeWide, stillThere, threeWideYoureOnTheLeft, threeWideYoureOnTheRight
        }

        public NoisyCartesianCoordinateSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState, float carLength, float carWidth)
        {
            this.audioPlayer = audioPlayer;
            this.setCarDimensions(carLength, carWidth);
        }

        internal void setCarDimensions(float carLength, float carWidth)
        {
            this.carLength = carLength;
            this.longCarLength = carLength + gapNeededForClear;
            this.carWidth = carWidth;
        }

        public void clearState()
        {
            carsOnLeftAtPreviousTick = 0;
            carsOnRightAtPreviousTick = 0;
            timeWhenChannelShouldBeClosed = DateTime.Now;
            channelLeftOpenTimerStarted = false;
            nextMessageType = NextMessageType.none;
            this.reportedSingleOverlapLeft = false;
            this.reportedSingleOverlapRight = false;
            this.reportedDoubleOverlapLeft = false;
            this.reportedDoubleOverlapRight = false;
            
            previousPositionAndVelocityData.Clear();
        }
        
        public void triggerInternal(float playerRotationInRadians, float[] currentPlayerPosition,
            float[] playerVelocityData, List<float[]> currentOpponentPositions)
        {
            DateTime now = DateTime.Now;

            if (currentPlayerPosition[0] != 0 && currentPlayerPosition[1] != 0 &&
                currentPlayerPosition[0] != -1 && currentPlayerPosition[1] != -1 &&
                playerVelocityData[0] > minSpeedForSpotterToOperate)
            {
                channelLeftOpenTimerStarted = false;
                int carsOnLeft = 0;
                int carsOnRight = 0;
                List<int> activeIDs = new List<int>();
                for (int i = 0; i < currentOpponentPositions.Count; i++)
                {
                    float[] currentOpponentPosition = currentOpponentPositions[i];
                    if (currentOpponentPosition[0] != 0 && currentOpponentPosition[1] != 0 &&   
                        currentOpponentPosition[0] != -1 && currentOpponentPosition[1] != -1)
                    {
                        activeIDs.Add(i);
                        if (opponentPositionInRange(currentOpponentPosition, currentPlayerPosition))
                        {
                            Boolean isOpponentVelocityInRange = false;
                            if (previousPositionAndVelocityData.ContainsKey(i))
                            {
                                PreviousPositionAndVelocityData opponentPreviousPositionAndVelocityData = previousPositionAndVelocityData[i];
                                float timeDiffMillis = ((float)(now - opponentPreviousPositionAndVelocityData.timeWhenLastUpdated).TotalMilliseconds);
                                if (timeDiffMillis >= calculateOpponentSpeedsEvery)
                                {
                                    opponentPreviousPositionAndVelocityData.timeWhenLastUpdated = now;
                                    opponentPreviousPositionAndVelocityData.xSpeed = 1000f * (currentOpponentPosition[0] - opponentPreviousPositionAndVelocityData.xPosition) / timeDiffMillis;
                                    opponentPreviousPositionAndVelocityData.zSpeed = 1000f * (currentOpponentPosition[1] - opponentPreviousPositionAndVelocityData.zPosition) / timeDiffMillis;
                                    opponentPreviousPositionAndVelocityData.xPosition = currentOpponentPosition[0];
                                    opponentPreviousPositionAndVelocityData.zPosition = currentOpponentPosition[1];
                                }
                                // we've updated this guys cached position and velocity, but we only need to check his speed if we don't already have 2 overlaps on both sides
                                if (carsOnLeft < maxOverlapsPerSide || carsOnRight < maxOverlapsPerSide)
                                {
                                    isOpponentVelocityInRange = checkOpponentVelocityInRange(playerVelocityData[1], playerVelocityData[2],
                                            opponentPreviousPositionAndVelocityData.xSpeed, opponentPreviousPositionAndVelocityData.zSpeed);
                                }
                            }
                            else
                            {
                                previousPositionAndVelocityData.Add(i, new PreviousPositionAndVelocityData(currentOpponentPosition[0], currentOpponentPosition[1], now));
                            }
                            // again, if we already have 2 overlaps on both sides here we don't need to calculate another
                            if (carsOnLeft < maxOverlapsPerSide || carsOnRight < maxOverlapsPerSide)
                            {
                                Side side = getSide(playerRotationInRadians, currentPlayerPosition[0], currentPlayerPosition[1], currentOpponentPosition[0], 
                                    currentOpponentPosition[1], isOpponentVelocityInRange);
                                if (side == Side.left)
                                {
                                    carsOnLeft++;
                                }
                                else if (side == Side.right)
                                {
                                    carsOnRight++;
                                }
                            }
                        } 
                        else if (previousPositionAndVelocityData.ContainsKey(i))
                        {
                            // once this opponent goes out of range, we must reset his cached speed and position data
                            previousPositionAndVelocityData.Remove(i);
                        }
                    }
                }
                List<int> opponentsToPurge = new List<int>();
                foreach (int cachedOpponentDataKey in previousPositionAndVelocityData.Keys)
                {
                    if (!activeIDs.Contains(cachedOpponentDataKey)) {
                        opponentsToPurge.Add(cachedOpponentDataKey);
                    }
                }
                foreach (int idToPurge in opponentsToPurge) {
                    if (previousPositionAndVelocityData.ContainsKey(idToPurge))
                    {
                        previousPositionAndVelocityData.Remove(idToPurge);
                    }
                }
                getNextMessage(carsOnLeft, carsOnRight, now);
                playNextMessage(carsOnLeft, carsOnRight, now);
                carsOnLeftAtPreviousTick = carsOnLeft;
                carsOnRightAtPreviousTick = carsOnRight;
            }
            else if (carsOnLeftAtPreviousTick > 0 || carsOnRightAtPreviousTick > 0)
            {
                if (!channelLeftOpenTimerStarted)
                {
                    timeWhenChannelShouldBeClosed = now.Add(timeToWaitBeforeClosingChannelLeftOpen);
                    channelLeftOpenTimerStarted = true;
                }
                if (now > timeWhenChannelShouldBeClosed)
                {
                    Console.WriteLine("Closing channel left open in spotter");
                    timeWhenChannelShouldBeClosed = DateTime.MaxValue;
                    carsOnLeftAtPreviousTick = 0;
                    carsOnRightAtPreviousTick = 0;
                    reportedDoubleOverlapLeft = false;
                    reportedDoubleOverlapRight = false;
                    reportedSingleOverlapLeft = false;
                    reportedSingleOverlapRight = false;
                    channelLeftOpenTimerStarted = false;
                }
            }
        }

        private Boolean checkOpponentVelocityInRange(float playerX, float playerZ, float opponentX, float opponentZ)
        {
            return Math.Abs(playerX - opponentX) < maxClosingSpeed && Math.Abs(playerZ - opponentZ) < maxClosingSpeed;
        }

        private Side getSide(float playerRotationInRadians, float playerX, float playerZ, float oppponentX, float opponentZ, Boolean isOpponentSpeedInRange)
        {
            float rawXCoordinate = oppponentX - playerX;
            float rawZCoordinate = opponentZ - playerZ;

            // now transform the position by rotating the frame of reference to align it north-south. The player's car is at the origin pointing north.
            // We assume that both cars have similar orientations (or at least, any orientation difference isn't going to be relevant)
            float alignedXCoordinate = ((float)Math.Cos(playerRotationInRadians) * rawXCoordinate) + ((float)Math.Sin(playerRotationInRadians) * rawZCoordinate);
            float alignedZCoordinate = ((float)Math.Cos(playerRotationInRadians) * rawZCoordinate) - ((float)Math.Sin(playerRotationInRadians) * rawXCoordinate);

            //Console.WriteLine("raw x " + rawXCoordinate + ", raw y = " + rawYCoordinate + ", aligned x " + alignedXCoordinate + ", aligned y " + alignedYCoordinate);

            // when checking for an overlap, use the 'short' (actual) car length if we're not already overlapping on that side.
            // If we're already overlapping, use the 'long' car length - this means we don't call 'clear' till there's a small gap

            // +ve alignedZCoordinate => the opponent is *behind*

            // we only want to check for width separation if we haven't already got an overlap
            if (Math.Abs(alignedXCoordinate) < trackZoneToConsider)
            {
                // we're within a track width of this car
                if (alignedXCoordinate < 0)
                {
                    if (carsOnRightAtPreviousTick > 0)
                    {
                        if (Math.Abs(alignedZCoordinate) < longCarLength)
                        {
                            return Side.right;
                        }
                    }
                    else if (((alignedZCoordinate < 0 && alignedZCoordinate * -1 < carLength) || (alignedZCoordinate > 0 && alignedZCoordinate < carLength + carBehindExtraLength)) &&
                        Math.Abs(alignedXCoordinate) > carWidth && isOpponentSpeedInRange)
                    {
                        // we have a new overlap on this side, it's only valid if we're not inside the other car and the speed isn't out of range
                        return Side.right;
                    }
                }
                else
                {
                    if (carsOnLeftAtPreviousTick > 0)
                    {
                        if (Math.Abs(alignedZCoordinate) < longCarLength)
                        {
                            return Side.left;
                        }
                    }
                    else if (((alignedZCoordinate < 0 && alignedZCoordinate * -1 < carLength) || (alignedZCoordinate > 0 && alignedZCoordinate < carLength + carBehindExtraLength)) &&
                        Math.Abs(alignedXCoordinate) > carWidth && isOpponentSpeedInRange)
                    {
                        return Side.left;
                    }
                }
            }
            return Side.none;
        }

        private Boolean opponentPositionInRange(float[] opponentPosition, float[] playerPosition)
        {
            float deltaX = Math.Abs(opponentPosition[0] - playerPosition[0]);
            float deltaY = Math.Abs(opponentPosition[1] - playerPosition[1]);
            return deltaX <= trackZoneToConsider && deltaY <= trackZoneToConsider;
        }

        private void getNextMessage(int carsOnLeftCount, int carsOnRightCount, DateTime now)
        {
            if (carsOnLeftCount == 0 && carsOnRightCount == 0 && carsOnLeftAtPreviousTick > 0 && carsOnRightAtPreviousTick > 0)
            {
                Console.WriteLine("clear all round");
                nextMessageType = NextMessageType.clearAllRound;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnLeftCount == 0 && carsOnLeftAtPreviousTick > 0 && 
                ((carsOnRightCount == 0 && carsOnRightAtPreviousTick == 0) || (carsOnRightCount > 0 && carsOnRightAtPreviousTick > 0)))
            {
                // just gone clear on the left - might still be a car right
                nextMessageType = NextMessageType.clearLeft;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnRightCount == 0 && carsOnRightAtPreviousTick > 0 && 
                ((carsOnLeftCount == 0 && carsOnLeftAtPreviousTick == 0) || (carsOnLeftCount > 0 && carsOnLeftAtPreviousTick > 0)))
            {
                // just gone clear on the right - might still be a car left
                nextMessageType = NextMessageType.clearRight;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnLeftCount > 0 && carsOnRightCount > 0 && (carsOnLeftAtPreviousTick == 0 || carsOnRightAtPreviousTick == 0))
            {
                // new 'in the middle'
                // if there's a 'pending clear' at this point (that is, we would have said "clear" but the delay time isn't up yet), then we're 
                // still in overlap-mode so don't want to say this immediately
                if ((reportedSingleOverlapLeft || reportedDoubleOverlapLeft) && (reportedSingleOverlapRight || reportedDoubleOverlapRight))
                {
                    nextMessageDue = now.Add(bouncingWait);
                }
                else
                {
                    nextMessageDue = now;
                }
                nextMessageType = NextMessageType.threeWide;                
            }
            else if (carsOnLeftCount > 0 && carsOnRightCount == 0 && carsOnLeftAtPreviousTick == 0 && carsOnRightAtPreviousTick == 0)
            {
                // if there's a 'pending clear' at this point (that is, we would have said "clear" but the delay time isn't up yet), then we're 
                // still in overlap-mode so don't want to say this immediately
                if (reportedSingleOverlapLeft || reportedDoubleOverlapLeft)
                {
                    nextMessageDue = now.Add(bouncingWait);
                }
                else
                {
                    nextMessageDue = now;
                }
                // we'll only ever go straight to 'three wide' here if both cars appear along side at exactly the same time, so this is a bit of an edge-case
                if (use3WideLeftAndRight && carsOnLeftCount > 1)
                {
                    nextMessageType = NextMessageType.threeWideYoureOnTheRight;
                }
                else
                {
                    nextMessageType = NextMessageType.carLeft;
                }
            }
            else if (carsOnLeftCount == 0 && carsOnRightCount > 0 && carsOnLeftAtPreviousTick == 0 && carsOnRightAtPreviousTick == 0)
            {                
                if (reportedSingleOverlapRight || reportedDoubleOverlapRight)
                {
                    nextMessageDue = now.Add(bouncingWait);
                }
                else
                {
                    nextMessageDue = now;
                }
                // we'll only ever go straight to 'three wide' here if both cars appear along side at exactly the same time, so this is a bit of an edge-case
                if (use3WideLeftAndRight && carsOnRightCount > 1)
                {
                    nextMessageType = NextMessageType.threeWideYoureOnTheLeft;
                }
                else
                {
                    nextMessageType = NextMessageType.carRight;
                }
            }



            // special cases for 3-wide-on-left / right. If we're already overlapping on the left and we now have another overlap, report 3 wide.
            // The data can be *really* noisy here with the number of cars bouncing between 1 and 2
            else if (use3WideLeftAndRight && carsOnLeftCount > 1 && carsOnRightCount == 0 && carsOnLeftAtPreviousTick == 1 && carsOnRightAtPreviousTick == 0)
            {
                if (reportedSingleOverlapLeft)
                {
                    nextMessageDue = now.Add(onSingleOverlapTo3WideDelay);                    
                }
                else if (reportedDoubleOverlapLeft)
                {
                    // don't reset the message due time here
                    // nextMessageDue = now.Add(repeatHoldFrequency);
                }
                else
                {
                    nextMessageDue = now;
                }
                nextMessageType = NextMessageType.threeWideYoureOnTheRight;                          
            }
            else if (use3WideLeftAndRight && carsOnLeftCount == 0 && carsOnRightCount > 1 && carsOnLeftAtPreviousTick == 0 && carsOnRightAtPreviousTick == 1)
            {
                if (reportedSingleOverlapRight)
                {
                    nextMessageDue = now.Add(onSingleOverlapTo3WideDelay);
                }
                else if (reportedDoubleOverlapRight)
                {
                    // don't reset the message due time here
                    // nextMessageDue = now.Add(repeatHoldFrequency);
                }
                else
                {
                    nextMessageDue = now;
                    
                }
                nextMessageType = NextMessageType.threeWideYoureOnTheLeft;                                
            }
            // go from 3 wide on right to single car left
            else if (use3WideLeftAndRight && carsOnLeftCount == 1 && carsOnRightCount == 0 && carsOnLeftAtPreviousTick > 1 && carsOnRightAtPreviousTick == 0)
            {
                nextMessageType = NextMessageType.carLeft;
            }
            // go from 3 wide on left to single car right
            else if (use3WideLeftAndRight && carsOnLeftCount == 0 && carsOnRightCount == 1 && carsOnLeftAtPreviousTick == 0 && carsOnRightAtPreviousTick > 1)
            {
                nextMessageType = NextMessageType.carRight;
            }
        }

        private Boolean messageIsValid(NextMessageType nextMessageType, int carsOnLeftCount, int carsOnRightCount)
        {
            if (nextMessageType == NextMessageType.carLeft && carsOnLeftCount == 0)
            {
                return false;
            }
            if (nextMessageType == NextMessageType.carRight && carsOnRightCount == 0)
            {
                return false;
            } 
            if (nextMessageType == NextMessageType.threeWide && (carsOnRightCount == 0 || carsOnLeftCount == 0))
            {
                return false;
            } 
            if (nextMessageType == NextMessageType.stillThere && carsOnRightCount == 0 && carsOnLeftCount == 0)
            {
                return false;
            }
            if (nextMessageType == NextMessageType.threeWideYoureOnTheLeft && carsOnRightCount < 2)
            {
                return false;
            }
            if (nextMessageType == NextMessageType.threeWideYoureOnTheRight && carsOnLeftCount < 2)
            {
                return false;
            }
            return true;
        }

        private void playNextMessage(int carsOnLeftCount, int carsOnRightCount, DateTime now)
        {
            if (nextMessageType != NextMessageType.none && now > nextMessageDue)
            {
                if (messageIsValid(nextMessageType, carsOnLeftCount, carsOnRightCount))
                {
                    // this is the message selected for oval or road course overlaps and clears, used in the case statements later.
                    String selectedMessage;
                    switch (nextMessageType)
                    {
                        case NextMessageType.threeWide:
                            audioPlayer.removeImmediateMessages(new String[] { folderStillThere, folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderClearAllRound,
                                folderClearLeft, folderClearRight, folderClearInside, folderClearOutside, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft,
                                folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside });
                            QueuedMessage inTheMiddleMessage = new QueuedMessage(folderInTheMiddle, 0, null);
                            inTheMiddleMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + inTheMiddleMessageExpiresAfter;
                            audioPlayer.playSpotterMessage(inTheMiddleMessage, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedSingleOverlapLeft = true;
                            reportedSingleOverlapRight = true;
                            reportedDoubleOverlapLeft = false;
                            reportedDoubleOverlapRight = false;
                            wasInMiddle = true;
                            break;
                        case NextMessageType.threeWideYoureOnTheLeft:
                            audioPlayer.removeImmediateMessages(new String[] { folderStillThere, folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderClearAllRound, 
                                folderClearLeft, folderClearRight, folderClearInside, folderClearOutside, folderInTheMiddle, folderThreeWideYoureOnRight, folderThreeWideYoureOnOutside });
                            if (hasOvalSpecificSounds && GlobalBehaviourSettings.useOvalLogic && random.Next() > 0.2)
                            {
                                selectedMessage = folderThreeWideYoureOnInside;
                            }
                            else
                            {
                                selectedMessage = folderThreeWideYoureOnLeft;
                            }
                            QueuedMessage threeWideOnLeftMessage = new QueuedMessage(selectedMessage, 0, null);
                            threeWideOnLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playSpotterMessage(threeWideOnLeftMessage, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedDoubleOverlapRight = true;
                            reportedSingleOverlapRight = false;
                            break;
                        case NextMessageType.threeWideYoureOnTheRight:
                            audioPlayer.removeImmediateMessages(new String[] { folderStillThere, folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderClearAllRound,
                                folderClearLeft, folderClearRight, folderClearInside, folderClearOutside, folderInTheMiddle, folderThreeWideYoureOnLeft, folderThreeWideYoureOnInside });
                            if (hasOvalSpecificSounds && GlobalBehaviourSettings.useOvalLogic && random.Next() > 0.2)
                            {
                                selectedMessage = folderThreeWideYoureOnOutside;
                            }
                            else
                            {
                                selectedMessage = folderThreeWideYoureOnRight;
                            }
                            QueuedMessage threeWideOnRightMessage = new QueuedMessage(selectedMessage, 0, null);
                            threeWideOnRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playSpotterMessage(threeWideOnRightMessage, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedDoubleOverlapLeft = true;
                            reportedSingleOverlapLeft = false;
                            break;
                        case NextMessageType.carLeft:
                            audioPlayer.removeImmediateMessages(new String[] { folderStillThere, folderInTheMiddle, folderCarRight, folderCarOutside, folderClearAllRound, 
                                folderClearLeft, folderClearRight, folderClearInside, folderClearOutside, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft,  
                                folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside });
                            if (hasOvalSpecificSounds && GlobalBehaviourSettings.useOvalLogic && random.Next() > 0.2)
                            {
                                selectedMessage = folderCarInside;
                            }
                            else
                            {
                                selectedMessage = folderCarLeft;
                            }
                            QueuedMessage carLeftMessage = new QueuedMessage(selectedMessage, 0, null);
                            carLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playSpotterMessage(carLeftMessage, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedSingleOverlapLeft = true;
                            reportedDoubleOverlapLeft = false;
                            break;
                        case NextMessageType.carRight:
                            audioPlayer.removeImmediateMessages(new String[] { folderStillThere, folderCarLeft, folderCarInside, folderInTheMiddle, folderClearAllRound, 
                                folderClearLeft, folderClearRight, folderClearInside, folderClearOutside, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft,
                                folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside });
                            if (hasOvalSpecificSounds && GlobalBehaviourSettings.useOvalLogic && random.Next() > 0.2)
                            {
                                selectedMessage = folderCarOutside;
                            }
                            else
                            {
                                selectedMessage = folderCarRight;
                            }
                            QueuedMessage carRightMessage = new QueuedMessage(selectedMessage, 0, null);
                            carRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playSpotterMessage(carRightMessage, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedSingleOverlapRight = true;
                            reportedDoubleOverlapRight = false;
                            break;
                        case NextMessageType.clearAllRound:
                            if (reportedSingleOverlapLeft || reportedSingleOverlapRight || reportedDoubleOverlapLeft || reportedDoubleOverlapRight)
                            {
                                QueuedMessage clearAllRoundMessage = new QueuedMessage(folderClearAllRound, 0, null);
                                clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearAllRoundMessageExpiresAfter;
                                audioPlayer.removeImmediateMessages(new String[] { folderStillThere,folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderInTheMiddle, 
                                    folderClearLeft, folderClearRight, folderClearInside, folderClearOutside, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft, 
                                    folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside});
                                audioPlayer.playSpotterMessage(clearAllRoundMessage, false);
                                nextMessageType = NextMessageType.none;
                            }
                            
                            reportedSingleOverlapLeft = false;
                            reportedSingleOverlapRight = false;
                            reportedDoubleOverlapLeft = false;
                            reportedDoubleOverlapRight = false;
                            wasInMiddle = false;
                            break;
                        case NextMessageType.clearLeft:
                            if (reportedSingleOverlapLeft || reportedDoubleOverlapLeft)
                            {
                                if (carsOnRightCount == 0 && wasInMiddle)
                                {
                                    QueuedMessage clearAllRoundMessage = new QueuedMessage(folderClearAllRound, 0, null);
                                    clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateMessages(new String[] {folderStillThere, folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderInTheMiddle, 
                                        folderClearRight, folderClearLeft, folderClearInside, folderClearOutside, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft, 
                                        folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside});
                                    audioPlayer.playSpotterMessage(clearAllRoundMessage, false);
                                    nextMessageType = NextMessageType.none;
                                    reportedDoubleOverlapRight = false;
                                    reportedSingleOverlapRight = false;
                                    wasInMiddle = false;
                                }
                                else
                                {
                                    if (hasOvalSpecificSounds && GlobalBehaviourSettings.useOvalLogic && random.Next() > 0.2)
                                    {
                                        selectedMessage = folderClearInside;
                                    }
                                    else
                                    {
                                        selectedMessage = folderClearLeft;
                                    }
                                    QueuedMessage clearLeftMessage = new QueuedMessage(selectedMessage, 0, null);
                                    clearLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateMessages(new String[] { folderStillThere, folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderInTheMiddle,
                                        folderClearRight, folderClearOutside, folderClearAllRound, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft, 
                                        folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside});
                                    
                                    if (wasInMiddle)
                                    {
                                        audioPlayer.playSpotterMessage(clearLeftMessage, true);
                                        nextMessageType = NextMessageType.carRight;
                                        nextMessageDue = now.Add(repeatHoldFrequency);
                                        
                                    }
                                    else
                                    {
                                        audioPlayer.playSpotterMessage(clearLeftMessage, false);
                                        nextMessageType = NextMessageType.none;
                                        // belt'n'braces - the overlapRight stuff should have already been cleared by the time we reach here so this 
                                        // may be unnecessary, but it does no harm to make sure
                                        reportedSingleOverlapRight = false;
                                        reportedDoubleOverlapRight = false;
                                    }
                                }
                            }
                            reportedSingleOverlapLeft = false;
                            reportedDoubleOverlapLeft = false;
                            break;
                        case NextMessageType.clearRight:
                            if (reportedSingleOverlapRight || reportedDoubleOverlapRight)
                            {
                                if (carsOnLeftCount == 0 && wasInMiddle)
                                {
                                    QueuedMessage clearAllRoundMessage = new QueuedMessage(folderClearAllRound, 0, null);
                                    clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateMessages(new String[] {folderStillThere, folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderInTheMiddle, 
                                        folderClearRight, folderClearLeft, folderClearInside, folderClearOutside, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft,
                                        folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside});
                                    audioPlayer.playSpotterMessage(clearAllRoundMessage, false);
                                    nextMessageType = NextMessageType.none;
                                    reportedDoubleOverlapLeft = false;
                                    reportedSingleOverlapLeft = false;
                                    wasInMiddle = false;
                                }
                                else
                                {
                                    if (hasOvalSpecificSounds && GlobalBehaviourSettings.useOvalLogic && random.Next() > 0.2)
                                    {
                                        selectedMessage = folderClearOutside;
                                    }
                                    else
                                    {
                                        selectedMessage = folderClearRight;
                                    }
                                    QueuedMessage clearRightMessage = new QueuedMessage(selectedMessage, 0, null);
                                    clearRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateMessages(new String[] { folderStillThere, folderCarLeft, folderCarRight, folderCarInside, folderCarOutside, folderInTheMiddle, 
                                        folderClearLeft, folderClearInside, folderClearAllRound, folderThreeWideYoureOnRight, folderThreeWideYoureOnLeft,
                                        folderThreeWideYoureOnInside, folderThreeWideYoureOnOutside});                                    
                                    if (wasInMiddle)
                                    {
                                        audioPlayer.playSpotterMessage(clearRightMessage, true);
                                        nextMessageType = NextMessageType.carLeft;
                                        nextMessageDue = now.Add(repeatHoldFrequency);
                                    }
                                    else
                                    {
                                        audioPlayer.playSpotterMessage(clearRightMessage, false);
                                        nextMessageType = NextMessageType.none;
                                        // belt'n'braces - the overlapLeft stuff should have already been cleared by the time we reach here so this 
                                        // may be unnecessary, but it does no harm to make sure
                                        reportedSingleOverlapLeft = false;
                                        reportedDoubleOverlapLeft = false;
                                    }
                                }
                            }
                            reportedSingleOverlapRight = false;
                            reportedDoubleOverlapRight = false;
                            break;
                        case NextMessageType.stillThere:
                            if (reportedSingleOverlapLeft || reportedSingleOverlapRight || reportedDoubleOverlapLeft || reportedDoubleOverlapRight)
                            {
                                QueuedMessage holdYourLineMessage = new QueuedMessage(folderStillThere, 0, null);
                                holdYourLineMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                                audioPlayer.removeImmediateMessages(new String[] { folderClearRight, folderClearLeft, folderClearInside, folderClearOutside, folderClearAllRound });
                                audioPlayer.playSpotterMessage(holdYourLineMessage, true);
                                nextMessageType = NextMessageType.stillThere;
                                nextMessageDue = now.Add(repeatHoldFrequency);
                            }
                            break;
                        case NextMessageType.none:
                            break;
                    }
                }
            }
        }
    }
}
