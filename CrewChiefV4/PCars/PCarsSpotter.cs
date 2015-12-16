using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;

namespace CrewChiefV4.PCars
{
    class PCarsSpotter : Spotter
    {
        private Boolean paused = false;

        // if the audio player is in the middle of another message, this 'immediate' message will have to wait.
        // If it's older than 1000 milliseconds by the time the player's got round to playing it, it's expired
        private int clearMessageExpiresAfter = 2000;
        private int clearAllRoundMessageExpiresAfter = 2000;
        private int holdMessageExpiresAfter = 1000;
        private int inTheMiddleMessageExpiresAfter = 1000;

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("pcars_spotter_car_length");

        // before saying 'clear', we need to be carLength + this value from the other car
        private float gapNeededForClear = UserSettings.GetUserSettings().getFloat("spotter_gap_for_clear");

        private float longCarLength;
        
        // don't play spotter messages if we're going < 10ms
        private float minSpeedForSpotterToOperate = UserSettings.GetUserSettings().getFloat("min_speed_for_spotter");

        // for PCars we use this purely as a de-noising parameter
        private float maxClosingSpeed = 30;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        // say "still there" every 3 seconds
        private TimeSpan repeatHoldFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("spotter_hold_repeat_frequency"));

        private Boolean spotterOnlyWhenBeingPassed = UserSettings.GetUserSettings().getBoolean("spotter_only_when_being_passed");

        private Boolean hasCarLeft;
        private Boolean hasCarRight;

        private float trackWidth = 10f;

        private float carWidth = 1.7f;

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
        
        private Boolean enabled;

        private Boolean initialEnabledState;

        private DateTime timeWhenChannelShouldBeClosed;

        private TimeSpan timeToWaitBeforeClosingChannelLeftOpen = TimeSpan.FromMilliseconds(500);

        private Boolean channelLeftOpenTimerStarted = false;
        
        private AudioPlayer audioPlayer;

        private NextMessageType nextMessageType;

        private DateTime timeToStartSpotting = DateTime.Now;

        private Boolean reportedOverlapLeft = false;

        private Boolean reportedOverlapRight = false;

        private Boolean wasInMiddle = false;

        private enum Side {
            right, left, none
        }

        private Dictionary<String, Side> lastKnownOpponentState = new Dictionary<String, Side>();

        private Dictionary<String, int> lastKnownOpponentStateUseCounter = new Dictionary<String, int>();

        private int maxSavedStateReuse = 10;

        private enum NextMessageType
        {
            none, clearLeft, clearRight, clearAllRound, carLeft, carRight, threeWide, stillThere
        }

        public PCarsSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.longCarLength = carLength + gapNeededForClear;
        }

        public void clearState()
        {
            hasCarLeft = false;
            hasCarRight = false;
            timeWhenChannelShouldBeClosed = DateTime.Now;
            channelLeftOpenTimerStarted = false;
            nextMessageType = NextMessageType.none;
            timeToStartSpotting = DateTime.Now;
            this.reportedOverlapLeft = false;
            this.reportedOverlapRight = false;
            lastKnownOpponentState.Clear();
            lastKnownOpponentStateUseCounter.Clear();
            audioPlayer.closeChannel();
        }

        public void pause()
        {
            paused = true;
        }

        public void unpause()
        {
            paused = false;
        }

        public void trigger(Object lastStateObj, Object currentStateObj)
        {
            if (paused)
            {
                audioPlayer.closeChannel();
                return;
            }
            CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper currentWrapper = (CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)currentStateObj;
            pCarsAPIStruct currentState = currentWrapper.data;

            // game state is 3 for paused, 5 for replay. No idea what 4 is...
            if (currentState.mGameState == (uint)eGameState.GAME_FRONT_END ||
                (currentState.mGameState == (uint)eGameState.GAME_INGAME_PAUSED && !System.Diagnostics.Debugger.IsAttached) ||
                currentState.mGameState == (uint)eGameState.GAME_VIEWING_REPLAY || currentState.mGameState == (uint)eGameState.GAME_EXITED)
            {
                // don't ignore the paused game updates if we're in debug mode
                audioPlayer.closeChannel();
                return;
            }
            CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper previousWrapper = (CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)lastStateObj;
            pCarsAPIStruct lastState = previousWrapper.data;

            DateTime now = new DateTime(currentWrapper.ticksWhenRead);
            float interval = (float)(((double)currentWrapper.ticksWhenRead - (double)previousWrapper.ticksWhenRead) / (double)TimeSpan.TicksPerSecond);

            if (currentState.mRaceState == (int)eRaceState.RACESTATE_RACING &&
                lastState.mRaceState != (int)eRaceState.RACESTATE_RACING)
            {
                timeToStartSpotting = now.Add(TimeSpan.FromSeconds(timeAfterRaceStartToActivate));
            }
            // this check looks a bit funky... whe we start a practice session, the raceState is not_started
            // until we cross the line for the first time. Which is retarded really.
            if (currentState.mRaceState == (int)eRaceState.RACESTATE_INVALID || now < timeToStartSpotting ||
                (currentState.mSessionState == (int)eSessionState.SESSION_RACE && currentState.mRaceState == (int) eRaceState.RACESTATE_NOT_STARTED))
            {
                return;
            }

            float currentSpeed = currentState.mSpeed;
            float previousSpeed = lastState.mSpeed;

            if (enabled && currentState.mParticipantData.Count() > 1)
            {
                Tuple<int, pCarsAPIParticipantStruct> playerDataWithIndex = PCarsGameStateMapper.getPlayerDataStruct(currentState.mParticipantData, currentState.mViewedParticipantIndex);
                int playerIndex = playerDataWithIndex.Item1;
                pCarsAPIParticipantStruct playerData = playerDataWithIndex.Item2;
                float playerX = playerData.mWorldPosition[0];
                float playerY = playerData.mWorldPosition[2];
                if (playerX == 0 || playerY == 0 || playerX == -1 || playerY == -1 || 
                    lastState.mParticipantData == null || lastState.mParticipantData.Length == 0 || lastState.mViewedParticipantIndex < 0)
                {
                    return;
                }
                Tuple<int, pCarsAPIParticipantStruct> previousPlayerDataWithIndex = PCarsGameStateMapper.getPlayerDataStruct(lastState.mParticipantData, lastState.mViewedParticipantIndex);
                pCarsAPIParticipantStruct previousPlayerData = previousPlayerDataWithIndex.Item2;

                if (currentSpeed > minSpeedForSpotterToOperate && currentState.mPitMode == (uint) ePitMode.PIT_MODE_NONE)
                {
                    int carsOnLeft = 0;
                    int carsOnRight = 0;
                    for (int i = 0; i < currentState.mParticipantData.Count(); i++)
                    {
                        if (i == playerIndex)
                        {
                            continue;
                        }
                        if (carsOnLeft >= 1 && carsOnRight >= 1)
                        {
                            // stop processing - we already know there's a car on both sides
                            break;
                        }
                        
                        pCarsAPIParticipantStruct opponentData = currentState.mParticipantData[i];

                        float previousOpponentX = 0;
                        float previousOpponentY = 0;
                        try
                        {
                            pCarsAPIParticipantStruct previousOpponentData = PCarsGameStateMapper.getParticipantDataForName(lastState.mParticipantData, opponentData.mName, i);
                            previousOpponentX = previousOpponentData.mWorldPosition[0];
                            previousOpponentY = previousOpponentData.mWorldPosition[2];
                        }
                        catch (Exception)
                        {
                            // ignore - the mParticipantData array is frequently full of crap
                        }
                        float currentOpponentX = opponentData.mWorldPosition[0];
                        float currentOpponentY = opponentData.mWorldPosition[2];

                        if (opponentData.mIsActive) {
                            if (currentOpponentX != 0 && currentOpponentY != 0 &&
                                    currentOpponentX != -1 && currentOpponentY != -1 &&
                                previousOpponentX != 0 && previousOpponentY != 0 &&
                                    previousOpponentX != -1 && previousOpponentY != -1 &&
                                opponentIsRacing(currentOpponentX, currentOpponentY, previousOpponentX, previousOpponentY, playerData, previousPlayerData, interval))
                            {
                                Side side = getSide(currentState.mOrientation[1], playerX, playerY, currentOpponentX, currentOpponentY);
                                if (side == Side.left)
                                {
                                    carsOnLeft++;
                                    if (lastKnownOpponentState.ContainsKey(opponentData.mName))
                                    {
                                        lastKnownOpponentState[opponentData.mName] = Side.left;
                                    }
                                    else
                                    {
                                        lastKnownOpponentState.Add(opponentData.mName, Side.left);
                                    }
                                }
                                else if (side == Side.right)
                                {
                                    carsOnRight++;
                                    if (lastKnownOpponentState.ContainsKey(opponentData.mName))
                                    {
                                        lastKnownOpponentState[opponentData.mName] = Side.right;
                                    }
                                    else
                                    {
                                        lastKnownOpponentState.Add(opponentData.mName, Side.right);
                                    }
                                }
                                else
                                {
                                    if (lastKnownOpponentState.ContainsKey(opponentData.mName))
                                    {
                                        lastKnownOpponentState[opponentData.mName] = Side.none;
                                    }
                                    else
                                    {
                                        lastKnownOpponentState.Add(opponentData.mName, Side.none);
                                    }
                                }                             
                            }
                            else
                            {
                                // no usable position data, use the last known state
                                if (lastKnownOpponentState.ContainsKey(opponentData.mName))
                                {
                                    int lastStateUseCount = 1;
                                    if (lastKnownOpponentStateUseCounter.ContainsKey(opponentData.mName))
                                    {
                                        lastStateUseCount = lastKnownOpponentStateUseCounter[opponentData.mName] + 1;
                                    }
                                    else
                                    {
                                        lastKnownOpponentStateUseCounter.Add(opponentData.mName, 0);
                                    }
                                    if (lastStateUseCount < maxSavedStateReuse)
                                    {
                                        lastKnownOpponentStateUseCounter[opponentData.mName] = lastStateUseCount;
                                        if (lastKnownOpponentState[opponentData.mName] == Side.left)
                                        {
                                            carsOnLeft++;
                                        }
                                        else if (lastKnownOpponentState[opponentData.mName] == Side.right)
                                        {
                                            carsOnRight++;
                                        }
                                    }
                                    else
                                    {
                                        // we've used too many saved states for this missing opponent position
                                        lastKnownOpponentState.Remove(opponentData.mName);
                                        lastKnownOpponentStateUseCounter.Remove(opponentData.mName);
                                    }
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
        }

        private Boolean opponentIsRacing(float currentOpponentX, float currentOpponentY, float previousOpponentX, float previousOpponentY,
            pCarsAPIParticipantStruct playerData, pCarsAPIParticipantStruct previousPlayerData, float interval)
        {
            float deltaX = Math.Abs(currentOpponentX - playerData.mWorldPosition[0]);
            float deltaY = Math.Abs(currentOpponentY - playerData.mWorldPosition[2]);
            if (deltaX > trackWidth || deltaY > trackWidth)
            {
                return false;
            }
            float opponentVelocityX = Math.Abs(currentOpponentX - previousOpponentX) / interval;
            float opponentVelocityY = Math.Abs(currentOpponentY - previousOpponentY) / interval;
            // hard code this - if the opponent car is going < 4m/s on both axis we're not interested
            if (opponentVelocityX < 4 && opponentVelocityY < 4)
            {
                return false;
            }

            float playerVelocityX = Math.Abs(playerData.mWorldPosition[0] - previousPlayerData.mWorldPosition[0]) / interval;
            float playerVelocityY = Math.Abs(playerData.mWorldPosition[2] - previousPlayerData.mWorldPosition[2]) / interval;

            if (Math.Abs(playerVelocityX - opponentVelocityX) > maxClosingSpeed || Math.Abs(playerVelocityY - opponentVelocityY) > maxClosingSpeed)
            {
               // Console.WriteLine("high closing speed: x = " + (playerVelocityX - opponentVelocityX) + " y = " + (playerVelocityY - opponentVelocityY));
                return false;
            }
            return true;
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
                Console.WriteLine("3 wide, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                nextMessageType = NextMessageType.threeWide;
                nextMessageDue = now;
            }
            else if (carsOnLeftCount > 0 && carsOnRightCount == 0 && !hasCarLeft && !hasCarRight)
            {
                Console.WriteLine("car left, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                nextMessageType = NextMessageType.carLeft;
                nextMessageDue = now;
            }
            else if (carsOnLeftCount == 0 && carsOnRightCount > 0 && !hasCarLeft && !hasCarRight)
            {
                Console.WriteLine("car right, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                nextMessageType = NextMessageType.carRight;
                nextMessageDue = now;
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

        private Side getSide(float playerRotation, float playerX, float playerY, float oppponentX, float opponentY)
        {
            float rawXCoordinate = oppponentX - playerX;
            float rawYCoordinate = opponentY - playerY;
            if (playerRotation < 0)
            {
                playerRotation = (float)(2 * Math.PI) + playerRotation;
            }
            playerRotation = (float)(2 * Math.PI) - playerRotation;
            
            // now transform the position by rotating the frame of reference to align it north-south. The player's car is at the origin pointing north.
            // We assume that both cars have similar orientations (or at least, any orientation difference isn't going to be relevant)
            float alignedXCoordinate = ((float)Math.Cos(playerRotation) * rawXCoordinate) + ((float)Math.Sin(playerRotation) * rawYCoordinate);
            float alignedYCoordinate = ((float)Math.Cos(playerRotation) * rawYCoordinate) - ((float)Math.Sin(playerRotation) * rawXCoordinate);


            // when checking for an overlap, use the 'short' (actual) car length if we're not already overlapping on that side.
            // If we're already overlapping, use the 'long' car length - this means we don't call 'clear' till there's a small gap
            if (Math.Abs(alignedXCoordinate) < trackWidth && Math.Abs(alignedXCoordinate) > carWidth)
            {
                // we're not directly behind / ahead, but are within a track width of this car
                if (alignedXCoordinate < 0)
                {
                    if (hasCarRight)
                    {
                        if (Math.Abs(alignedYCoordinate) < longCarLength)
                        {
                            return Side.right;
                        }
                    }
                    else if (Math.Abs(alignedYCoordinate) < carLength)
                    {
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
                    else if (Math.Abs(alignedYCoordinate) < carLength)
                    {
                        return Side.left;
                    }
                }
            }
            return Side.none;
        }

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null), false);
            audioPlayer.closeChannel();
        }

        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null), false);
            audioPlayer.closeChannel();
        }
    }
}
