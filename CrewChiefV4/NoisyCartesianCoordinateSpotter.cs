using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;

namespace CrewChiefV4
{
    class PreviousPositionAndVelocityData {
        public float xPosition;
        public float yPosition;
        public DateTime timeWhenLastUpdated;

        public List<float> previousXSpeeds = new List<float>();
        public List<float> previousYSpeeds = new List<float>();

        public PreviousPositionAndVelocityData(float xPosition, float yPosition, DateTime timeWhenLastUpdated) {
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            previousXSpeeds.Add(0f);
            previousYSpeeds.Add(0f);
            this.timeWhenLastUpdated = timeWhenLastUpdated;
        }
    }
    class NoisyCartesianCoordinateSpotter
    {
        private int speedsToAverage = 7;

        // if the audio player is in the middle of another message, this 'immediate' message will have to wait.
        // If it's older than 1000 milliseconds by the time the player's got round to playing it, it's expired
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

        private Boolean spotterOnlyWhenBeingPassed = UserSettings.GetUserSettings().getBoolean("spotter_only_when_being_passed");

        private Boolean hasCarLeft;
        private Boolean hasCarRight;

        private float trackWidth;
        private float carWidth;
        private float maxClosingSpeed;

        private String folderStillThere = "spotter/still_there";
        private String folderInTheMiddle = "spotter/in_the_middle";
        private String folderCarLeft = "spotter/car_left";
        private String folderCarRight = "spotter/car_right"; 
        private String folderClearLeft = "spotter/clear_left";
        private String folderClearRight = "spotter/clear_right";
        private String folderClearAllRound = "spotter/clear_all_round";

        // don't play 'clear' or 'hold' messages unless we've actually been clear or overlapping for some time
        private TimeSpan clearMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_clear_delay"));
        private TimeSpan overlapMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_overlap_delay"));

        private DateTime nextMessageDue = DateTime.Now;
        
        private DateTime timeWhenChannelShouldBeClosed;

        private TimeSpan timeToWaitBeforeClosingChannelLeftOpen = TimeSpan.FromMilliseconds(500);

        private Boolean channelLeftOpenTimerStarted = false;
        
        private AudioPlayer audioPlayer;

        private NextMessageType nextMessageType;

        private Boolean reportedOverlapLeft = false;

        private Boolean reportedOverlapRight = false;

        private Boolean wasInMiddle = false;

        private enum Side {
            right, left, none
        }

        private Dictionary<int, Side> lastKnownOpponentState = new Dictionary<int, Side>();

        private Dictionary<int, int> lastKnownOpponentStateUseCounter = new Dictionary<int, int>();

        private Dictionary<int, PreviousPositionAndVelocityData> previousPositionAndVelocityData = new Dictionary<int, PreviousPositionAndVelocityData>();

        private int maxSavedStateReuse = 10;

        private enum NextMessageType
        {
            none, clearLeft, clearRight, clearAllRound, carLeft, carRight, threeWide, stillThere
        }

        public NoisyCartesianCoordinateSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState, float carLength, float carWidth, float trackWidth, float maxClosingSpeed)
        {
            this.audioPlayer = audioPlayer;
            this.carLength = carLength;
            this.longCarLength = carLength + gapNeededForClear;
            this.carWidth = carWidth;
            this.trackWidth = trackWidth;
            this.maxClosingSpeed = maxClosingSpeed;
        }

        public void clearState()
        {
            hasCarLeft = false;
            hasCarRight = false;
            timeWhenChannelShouldBeClosed = DateTime.Now;
            channelLeftOpenTimerStarted = false;
            nextMessageType = NextMessageType.none;
            this.reportedOverlapLeft = false;
            this.reportedOverlapRight = false;
            lastKnownOpponentState.Clear();
            lastKnownOpponentStateUseCounter.Clear();
            audioPlayer.closeChannel();
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
                int carsOnLeft = 0;
                int carsOnRight = 0;
                for (int i = 0; i < currentOpponentPositions.Count; i++)
                {
                    if (carsOnLeft >= 1 && carsOnRight >= 1)
                    {
                        // stop processing - we already know there's a car on both sides
                        break;
                    }
                    float[] currentOpponentPosition = currentOpponentPositions[i];
                    if (currentOpponentPosition[0] != 0 && currentOpponentPosition[1] != 0 &&   
                        currentOpponentPosition[0] != -1 && currentOpponentPosition[1] != -1)
                    {
                        if (opponentPositionInRange(currentOpponentPosition, currentPlayerPosition))
                        {
                            Boolean isOpponentVelocityInRange = true;
                            if (previousPositionAndVelocityData.ContainsKey(i))
                            {
                                PreviousPositionAndVelocityData opponentPreviousPositionAndVelocityData = previousPositionAndVelocityData[i];   
                                float timeDiffSeconds = ((float)(now - opponentPreviousPositionAndVelocityData.timeWhenLastUpdated).TotalMilliseconds) / 1000f;
                                if (timeDiffSeconds > 0)
                                {
                                    if (opponentPreviousPositionAndVelocityData.previousXSpeeds.Count == speedsToAverage)
                                    {
                                        opponentPreviousPositionAndVelocityData.previousXSpeeds.RemoveAt(speedsToAverage - 1);
                                        opponentPreviousPositionAndVelocityData.previousYSpeeds.RemoveAt(speedsToAverage - 1);
                                    }
                                    opponentPreviousPositionAndVelocityData.previousXSpeeds.Insert(0, (currentOpponentPosition[0] - opponentPreviousPositionAndVelocityData.xPosition) / timeDiffSeconds);
                                    opponentPreviousPositionAndVelocityData.previousYSpeeds.Insert(0, (currentOpponentPosition[1] - opponentPreviousPositionAndVelocityData.yPosition) /timeDiffSeconds);
                                    opponentPreviousPositionAndVelocityData.xPosition = currentOpponentPosition[0];
                                    opponentPreviousPositionAndVelocityData.yPosition = currentOpponentPosition[1];
                                    opponentPreviousPositionAndVelocityData.timeWhenLastUpdated = now;

                                    isOpponentVelocityInRange = checkOpponentVelocityInRange(playerVelocityData[1], playerVelocityData[2],
                                            opponentPreviousPositionAndVelocityData.previousXSpeeds.Average(), opponentPreviousPositionAndVelocityData.previousYSpeeds.Average());
                                }
                                
                            }
                            else
                            {
                                previousPositionAndVelocityData.Add(i, new PreviousPositionAndVelocityData(currentOpponentPosition[0], currentOpponentPosition[1], now));
                            }
                            Side side = getSide(playerRotationInRadians, currentPlayerPosition[0], currentPlayerPosition[1], currentOpponentPosition[0], currentOpponentPosition[1], isOpponentVelocityInRange);
                            if (side == Side.left)
                            {
                                carsOnLeft++;
                                if (lastKnownOpponentState.ContainsKey(i))
                                {
                                    lastKnownOpponentState[i] = Side.left;
                                }
                                else
                                {
                                    lastKnownOpponentState.Add(i, Side.left);
                                }
                            }
                            else if (side == Side.right)
                            {
                                carsOnRight++;
                                if (lastKnownOpponentState.ContainsKey(i))
                                {
                                    lastKnownOpponentState[i] = Side.right;
                                }
                                else
                                {
                                    lastKnownOpponentState.Add(i, Side.right);
                                }
                            }
                            else
                            {
                                if (lastKnownOpponentState.ContainsKey(i))
                                {
                                    lastKnownOpponentState[i] = Side.none;
                                }
                                else
                                {
                                    lastKnownOpponentState.Add(i, Side.none);
                                }
                            }
                        }
                    }
                    else
                    {
                        // no usable position data, use the last known state
                        if (lastKnownOpponentState.ContainsKey(i)) {
                            int lastStateUseCount = 1;
                            if (lastKnownOpponentStateUseCounter.ContainsKey(i))
                            {
                                lastStateUseCount = lastKnownOpponentStateUseCounter[i] + 1;
                            }
                            else
                            {
                                lastKnownOpponentStateUseCounter.Add(i, 0);
                            }
                            if (lastStateUseCount < maxSavedStateReuse)
                            {
                                lastKnownOpponentStateUseCounter[i] = lastStateUseCount;
                                if (lastKnownOpponentState[i] == Side.left)
                                {
                                    carsOnLeft++;
                                }
                                else if (lastKnownOpponentState[i] == Side.right)
                                {
                                    carsOnRight++;
                                }
                            }
                            else
                            {
                                // we've used too many saved states for this missing opponent position
                                lastKnownOpponentState.Remove(i);
                                lastKnownOpponentStateUseCounter.Remove(i);
                            }
                        }
                    }
                }     
                getNextMessage(carsOnLeft, carsOnRight, now);
                playNextMessage(carsOnLeft, carsOnRight, now);
                hasCarLeft = carsOnLeft > 0;
                hasCarRight = carsOnRight > 0;
            }
            else if (hasCarLeft || hasCarRight)
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
                    hasCarLeft = false;
                    hasCarRight = false;
                    audioPlayer.closeChannel();
                    channelLeftOpenTimerStarted = false;
                }
            }
        }

        private Boolean checkOpponentVelocityInRange(float playerX, float playerZ, float opponentX, float opponentZ)
        {
            return Math.Abs(playerX - opponentX) < maxClosingSpeed && Math.Abs(playerZ - opponentZ) < maxClosingSpeed;
        }

        private Side getSide(float playerRotationInRadians, float playerX, float playerY, float oppponentX, float opponentY, Boolean isOpponentSpeedInRange)
        {
            float rawXCoordinate = oppponentX - playerX;
            float rawYCoordinate = opponentY - playerY;

            // now transform the position by rotating the frame of reference to align it north-south. The player's car is at the origin pointing north.
            // We assume that both cars have similar orientations (or at least, any orientation difference isn't going to be relevant)
            float alignedXCoordinate = ((float)Math.Cos(playerRotationInRadians) * rawXCoordinate) + ((float)Math.Sin(playerRotationInRadians) * rawYCoordinate);
            float alignedYCoordinate = ((float)Math.Cos(playerRotationInRadians) * rawYCoordinate) - ((float)Math.Sin(playerRotationInRadians) * rawXCoordinate);

            //Console.WriteLine("raw x " + rawXCoordinate + ", raw y = " + rawYCoordinate + ", aligned x " + alignedXCoordinate + ", aligned y " + alignedYCoordinate);

            // when checking for an overlap, use the 'short' (actual) car length if we're not already overlapping on that side.
            // If we're already overlapping, use the 'long' car length - this means we don't call 'clear' till there's a small gap

            // we only want to check for width separation if we haven't already got an overlap
            if (Math.Abs(alignedXCoordinate) < trackWidth)
            {
                // we're within a track width of this car
                if (alignedXCoordinate < 0)
                {
                    if (hasCarRight)
                    {
                        if (Math.Abs(alignedYCoordinate) < longCarLength)
                        {
                            return Side.right;
                        }
                    }
                    else if (Math.Abs(alignedYCoordinate) < carLength && alignedXCoordinate * -1 > carWidth && isOpponentSpeedInRange)
                    {
                        // we have a new overlap on this side, it's only valid if we're not inside the other car and the speed isn't out of range
                        return Side.right;
                    }
                }
                else
                {
                    if (hasCarLeft)
                    {
                        if (Math.Abs(alignedYCoordinate) < longCarLength)
                        {
                            return Side.left;
                        }
                    }
                    else if (Math.Abs(alignedYCoordinate) < carLength && alignedXCoordinate > carWidth && isOpponentSpeedInRange)
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
            return deltaX <= trackWidth && deltaY <= trackWidth;
        }

        private void getNextMessage(int carsOnLeftCount, int carsOnRightCount, DateTime now)
        {
            if (carsOnLeftCount == 0 && carsOnRightCount == 0 && hasCarLeft && hasCarRight)
            {
                Console.WriteLine("clear all round");
                nextMessageType = NextMessageType.clearAllRound;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnLeftCount == 0 && hasCarLeft && ((carsOnRightCount == 0 && !hasCarRight) || (carsOnRightCount > 0 && hasCarRight)))
            {
                Console.WriteLine("clear left, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                // just gone clear on the left - might still be a car right
                nextMessageType = NextMessageType.clearLeft;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnRightCount == 0 && hasCarRight && ((carsOnLeftCount == 0 && !hasCarLeft) || (carsOnLeftCount > 0 && hasCarLeft)))
            {
                Console.WriteLine("clear right, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                // just gone clear on the right - might still be a car left
                nextMessageType = NextMessageType.clearRight;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnLeftCount > 0 && carsOnRightCount > 0 && (!hasCarLeft || !hasCarRight))
            {
                // new 'in the middle'
                // if there's a 'pending clear' at this point (that is, we would have said "clear" but the delay time isn't up yet), then we're 
                // still in overlap-mode so don't want to say this immediately
                if (reportedOverlapLeft && reportedOverlapRight)
                {
                    Console.WriteLine("Delayed 3 wide, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                    nextMessageDue = now.Add(repeatHoldFrequency);
                }
                else
                {
                    Console.WriteLine("3 wide, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                    nextMessageDue = now;
                }
                nextMessageType = NextMessageType.threeWide;                
            }
            else if (carsOnLeftCount > 0 && carsOnRightCount == 0 && !hasCarLeft && !hasCarRight)
            {
                // if there's a 'pending clear' at this point (that is, we would have said "clear" but the delay time isn't up yet), then we're 
                // still in overlap-mode so don't want to say this immediately
                if (reportedOverlapLeft)
                {
                    Console.WriteLine("Delayed car left, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                    nextMessageDue = now.Add(repeatHoldFrequency);
                }
                else
                {
                    Console.WriteLine("Car left, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                    nextMessageDue = now;
                }
                nextMessageType = NextMessageType.carLeft;
            }
            else if (carsOnLeftCount == 0 && carsOnRightCount > 0 && !hasCarLeft && !hasCarRight)
            {
                if (reportedOverlapRight)
                {
                    Console.WriteLine("Delayed car right, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                    nextMessageDue = now.Add(repeatHoldFrequency);
                }
                else
                {
                    Console.WriteLine("Car right, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                    nextMessageDue = now;
                }
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
            return true;
        }

        private void playNextMessage(int carsOnLeftCount, int carsOnRightCount, DateTime now)
        {
            if (nextMessageType != NextMessageType.none && now > nextMessageDue)
            {
                if (messageIsValid(nextMessageType, carsOnLeftCount, carsOnRightCount))
                {
                    switch (nextMessageType)
                    {
                        case NextMessageType.threeWide:
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.removeImmediateClip(folderCarLeft);
                            audioPlayer.removeImmediateClip(folderCarRight);
                            audioPlayer.removeImmediateClip(folderClearAllRound);
                            audioPlayer.removeImmediateClip(folderClearLeft);
                            audioPlayer.removeImmediateClip(folderClearRight);
                            QueuedMessage inTheMiddleMessage = new QueuedMessage(folderInTheMiddle, 0, null);
                            inTheMiddleMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + inTheMiddleMessageExpiresAfter;
                            audioPlayer.playClipImmediately(inTheMiddleMessage, true, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedOverlapLeft = true;
                            reportedOverlapRight = true;
                            wasInMiddle = true;
                            break;
                        case NextMessageType.carLeft:
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.removeImmediateClip(folderInTheMiddle);
                            audioPlayer.removeImmediateClip(folderCarRight);
                            audioPlayer.removeImmediateClip(folderClearAllRound);
                            audioPlayer.removeImmediateClip(folderClearLeft);
                            audioPlayer.removeImmediateClip(folderClearRight);
                            QueuedMessage carLeftMessage = new QueuedMessage(folderCarLeft, 0, null);
                            carLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playClipImmediately(carLeftMessage, true, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedOverlapLeft = true;
                            break;
                        case NextMessageType.carRight:
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.removeImmediateClip(folderCarLeft);
                            audioPlayer.removeImmediateClip(folderInTheMiddle);
                            audioPlayer.removeImmediateClip(folderClearAllRound);
                            audioPlayer.removeImmediateClip(folderClearLeft);
                            audioPlayer.removeImmediateClip(folderClearRight);
                            QueuedMessage carRightMessage = new QueuedMessage(folderCarRight, 0, null);
                            carRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playClipImmediately(carRightMessage, true, true);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedOverlapRight = true;
                            break;
                        case NextMessageType.clearAllRound:
                            if (reportedOverlapLeft || reportedOverlapRight)
                            {
                                QueuedMessage clearAllRoundMessage = new QueuedMessage(folderClearAllRound, 0, null);
                                clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearAllRoundMessageExpiresAfter;
                                audioPlayer.removeImmediateClip(folderCarLeft);
                                audioPlayer.removeImmediateClip(folderStillThere);
                                audioPlayer.removeImmediateClip(folderCarRight);
                                audioPlayer.removeImmediateClip(folderInTheMiddle);
                                audioPlayer.removeImmediateClip(folderClearLeft);
                                audioPlayer.removeImmediateClip(folderClearRight);
                                audioPlayer.playClipImmediately(clearAllRoundMessage, false);
                                nextMessageType = NextMessageType.none;
                            }
                            audioPlayer.closeChannel();
                            reportedOverlapLeft = false;
                            reportedOverlapRight = false;
                            wasInMiddle = false;
                            break;
                        case NextMessageType.clearLeft:
                            if (reportedOverlapLeft)
                            {
                                if (carsOnRightCount == 0 && wasInMiddle)
                                {
                                    QueuedMessage clearAllRoundMessage = new QueuedMessage(folderClearAllRound, 0, null);
                                    clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateClip(folderCarLeft);
                                    audioPlayer.removeImmediateClip(folderStillThere);
                                    audioPlayer.removeImmediateClip(folderCarRight);
                                    audioPlayer.removeImmediateClip(folderInTheMiddle);
                                    audioPlayer.removeImmediateClip(folderClearRight);
                                    audioPlayer.removeImmediateClip(folderClearLeft);
                                    audioPlayer.playClipImmediately(clearAllRoundMessage, false);
                                    nextMessageType = NextMessageType.none;
                                    audioPlayer.closeChannel();
                                    reportedOverlapRight = false;
                                    wasInMiddle = false;
                                }
                                else
                                {
                                    QueuedMessage clearLeftMessage = new QueuedMessage(folderClearLeft, 0, null);
                                    clearLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateClip(folderCarLeft);
                                    audioPlayer.removeImmediateClip(folderStillThere);
                                    audioPlayer.removeImmediateClip(folderCarRight);
                                    audioPlayer.removeImmediateClip(folderInTheMiddle);
                                    audioPlayer.removeImmediateClip(folderClearRight);
                                    audioPlayer.removeImmediateClip(folderClearAllRound);
                                    audioPlayer.playClipImmediately(clearLeftMessage, false);
                                    if (wasInMiddle)
                                    {
                                        nextMessageType = NextMessageType.carRight;
                                        nextMessageDue = now.Add(repeatHoldFrequency);
                                    }
                                    else
                                    {
                                        nextMessageType = NextMessageType.none;
                                    }
                                }
                            }
                            if (carsOnRightCount == 0)
                            {
                                audioPlayer.closeChannel();
                            }
                            reportedOverlapLeft = false;
                            break;
                        case NextMessageType.clearRight:
                            if (reportedOverlapRight)
                            {
                                if (carsOnLeftCount == 0 && wasInMiddle)
                                {
                                    QueuedMessage clearAllRoundMessage = new QueuedMessage(folderClearAllRound, 0, null);
                                    clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateClip(folderCarLeft);
                                    audioPlayer.removeImmediateClip(folderStillThere);
                                    audioPlayer.removeImmediateClip(folderCarRight);
                                    audioPlayer.removeImmediateClip(folderInTheMiddle);
                                    audioPlayer.removeImmediateClip(folderClearLeft);
                                    audioPlayer.removeImmediateClip(folderClearRight);
                                    audioPlayer.playClipImmediately(clearAllRoundMessage, false);
                                    nextMessageType = NextMessageType.none;
                                    audioPlayer.closeChannel();
                                    reportedOverlapLeft = false;
                                    wasInMiddle = false;
                                }
                                else
                                {
                                    QueuedMessage clearRightMessage = new QueuedMessage(folderClearRight, 0, null);
                                    clearRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateClip(folderCarLeft);
                                    audioPlayer.removeImmediateClip(folderStillThere);
                                    audioPlayer.removeImmediateClip(folderCarRight);
                                    audioPlayer.removeImmediateClip(folderInTheMiddle);
                                    audioPlayer.removeImmediateClip(folderClearLeft);
                                    audioPlayer.removeImmediateClip(folderClearAllRound);
                                    audioPlayer.playClipImmediately(clearRightMessage, false);
                                    if (wasInMiddle)
                                    {
                                        nextMessageType = NextMessageType.carLeft;
                                        nextMessageDue = now.Add(repeatHoldFrequency);
                                    }
                                    else
                                    {
                                        nextMessageType = NextMessageType.none;
                                    }
                                }
                            }
                            if (carsOnLeftCount == 0)
                            {
                                audioPlayer.closeChannel();
                            }
                            reportedOverlapRight = false;
                            break;
                        case NextMessageType.stillThere:
                            if (reportedOverlapLeft || reportedOverlapRight)
                            {
                                QueuedMessage holdYourLineMessage = new QueuedMessage(folderStillThere, 0, null);
                                holdYourLineMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                                audioPlayer.removeImmediateClip(folderClearRight);
                                audioPlayer.removeImmediateClip(folderClearLeft);
                                audioPlayer.removeImmediateClip(folderClearAllRound);
                                audioPlayer.playClipImmediately(holdYourLineMessage, true, false);
                                nextMessageType = NextMessageType.stillThere;
                                nextMessageDue = now.Add(repeatHoldFrequency);
                            }
                            break;
                        case NextMessageType.none:
                            break;
                    }
                }
                else
                {
                    audioPlayer.closeChannel();
                    reportedOverlapLeft = false;
                    reportedOverlapRight = false;
                }
            }
        }
    }
}
