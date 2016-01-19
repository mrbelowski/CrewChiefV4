using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;

namespace CrewChiefV4.RaceRoom
{
    class R3ESpotter : Spotter
    {
        private Boolean paused = false;
        // if the audio player is in the middle of another message, this 'immediate' message will have to wait.
        // If it's older than 1000 milliseconds by the time the player's got round to playing it, it's expired
        private int clearMessageExpiresAfter = 2000;
        private int holdMessageExpiresAfter = 1000;

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("r3e_spotter_car_length");

        // before saying 'clear', we need to be carLength + this value from the other car
        private float gapNeededForClear = UserSettings.GetUserSettings().getFloat("spotter_gap_for_clear");

        // don't play spotter messages if we're going < 10ms
        private float minSpeedForSpotterToOperate = UserSettings.GetUserSettings().getFloat("min_speed_for_spotter");

        // if the closing speed is > 5ms (about 12mph) then don't trigger spotter messages - 
        // this prevents them being triggered when passing stationary cars
        private float maxClosingSpeed = UserSettings.GetUserSettings().getFloat("max_closing_speed_for_spotter");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        // say "still there" every 3 seconds
        private TimeSpan repeatHoldFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("spotter_hold_repeat_frequency"));

        private Boolean spotterOnlyWhenBeingPassed = UserSettings.GetUserSettings().getBoolean("spotter_only_when_being_passed");

        private Boolean isCurrentlyOverlapping;

        private String folderClear = "spotter/clear";
        private String folderHoldYourLine = "spotter/hold_your_line";
        private String folderStillThere = "spotter/still_there";

        // don't play 'clear' or 'hold' messages unless we've actually been clear or overlapping for some time
        private TimeSpan clearMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_clear_delay"));
        private TimeSpan overlapMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_overlap_delay"));

        private DateTime timeOfNextHoldMessage;

        private DateTime timeWhenWeCanSayClear;
        private DateTime timeWhenWeCanSayHold;

        private Boolean newlyClear = true;
        private Boolean newlyOverlapping = true;

        private Boolean enabled;

        private Boolean initialEnabledState;

        private DateTime timeWhenChannelShouldBeClosed;

        private TimeSpan timeToWaitBeforeClosingChannelLeftOpen = TimeSpan.FromMilliseconds(500);

        // this is -1 * the time taken to travel 1 car length at the minimum spotter speed
        private float biggestAllowedNegativeTimeDelta;

        private Boolean channelLeftOpenTimerStarted = false;

        private DateTime timeWhenWeveHadEnoughUnusableData;

        private TimeSpan maxTimeToKeepChannelOpenWhileReceivingUnusableData = TimeSpan.FromSeconds(2);

        private Boolean lastSpotterDataIsUsable;

        private AudioPlayer audioPlayer;

        public R3ESpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.biggestAllowedNegativeTimeDelta = -1 * carLength / minSpeedForSpotterToOperate;
        }

        public void clearState()
        {
            isCurrentlyOverlapping = false;
            timeOfNextHoldMessage = DateTime.Now;
            timeWhenWeveHadEnoughUnusableData = DateTime.Now;
            newlyClear = true;
            newlyOverlapping = true;
            enabled = initialEnabledState;
            timeWhenChannelShouldBeClosed = DateTime.Now;
            channelLeftOpenTimerStarted = false;
            lastSpotterDataIsUsable = false;
        }

        /**
         * The time delta should always be positive. It does briefly go negative at the end of the passing
         * phase (when the cars are exactly along side). Any negative value that we see which happens before
         * a valid overlap is considered to be noise in the data.
         */
        private Boolean checkDelta(float rawDelta, float speed)
        {
            return rawDelta > 0 ||
                (isCurrentlyOverlapping && rawDelta > (-1 * carLength / speed));
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
                return;
            }
            RaceRoomShared lastState = ((CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)lastStateObj).data;
            RaceRoomShared currentState = ((CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)currentStateObj).data;
            DateTime now = DateTime.Now;

            float currentSpeed = currentState.CarSpeed;
            float previousSpeed = lastState.CarSpeed;
            if (enabled && currentState.Player.GameSimulationTime > timeAfterRaceStartToActivate &&
                currentState.ControlType == (int)RaceRoomConstant.Control.Player && currentSpeed > minSpeedForSpotterToOperate)
            {
                channelLeftOpenTimerStarted = false;
                timeWhenChannelShouldBeClosed = now.Add(timeToWaitBeforeClosingChannelLeftOpen);
                float currentDeltaFront = currentState.TimeDeltaFront;
                float currentDeltaBehind = currentState.TimeDeltaBehind;
                float previousDeltaFront = lastState.TimeDeltaFront;
                float previousDeltaBehind = lastState.TimeDeltaBehind;

                if (checkDelta(currentDeltaFront, currentSpeed) && checkDelta(currentDeltaBehind, currentSpeed) && 
                    checkDelta(previousDeltaFront, previousSpeed) && checkDelta(previousDeltaBehind, previousSpeed))
                {
                    lastSpotterDataIsUsable = true;
                    // if we think there's already a car along side, add a little to the car length so we're
                    // sure it's gone before calling clear
                    float carLengthToUse = carLength;
                    if (isCurrentlyOverlapping)
                    {
                        carLengthToUse += gapNeededForClear;
                    }
                    // the time deltas might be small negative numbers while the cars are along side. They might also be large 
                    // negative numbers if the data are noisy - either way, use the absolute value to check for overlap
                    Boolean carAlongSideInFront = carLengthToUse / currentSpeed > Math.Abs(currentDeltaFront);
                    Boolean carAlongSideBehind = carLengthToUse / currentSpeed > Math.Abs(currentDeltaBehind);

                    if (!carAlongSideInFront && !carAlongSideBehind)
                    {
                        // we're clear here, so when we next detect we're overlapping we know this must be
                        // a new overlap. This may be valid or due to noise.
                        newlyOverlapping = true;
                        if (isCurrentlyOverlapping)
                        {
                            if (newlyClear)
                            {
                                // start the timer...
                                newlyClear = false;
                                timeWhenWeCanSayClear = now.Add(clearMessageDelay);
                            }
                            // only play "clear" if we've been clear for the specified time
                            if (now > timeWhenWeCanSayClear)
                            {
                                // don't play this message if the channel's closed   
                                isCurrentlyOverlapping = false;
                                if (audioPlayer.isChannelOpen())
                                {
                                    Console.WriteLine("Clear - delta front = " + currentDeltaFront + " delta behind = " + currentDeltaBehind);
                                    QueuedMessage clearMessage = new QueuedMessage(folderClear, 0, null);
                                    clearMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                    audioPlayer.removeImmediateClip(folderStillThere);
                                    audioPlayer.playClipImmediately(clearMessage, false);
                                    
                                }
                                else
                                {
                                    Console.WriteLine("Not playing clear message - channel is already closed");
                                }
                            }
                        }
                    }
                    else
                    {
                        // we're overlapping here, so when we next detect we're 'clear' we know this must be
                        // a new clear. This may be valid or due to noise
                        newlyClear = true;
                        if (newlyOverlapping)
                        {
                            timeWhenWeCanSayHold = now.Add(overlapMessageDelay);
                            newlyOverlapping = false;
                        }
                        if (now > timeWhenWeCanSayHold)
                        {
                            if (isCurrentlyOverlapping)
                            {
                                // play "still there" if we've not played one for a while
                                if (now > timeOfNextHoldMessage)
                                {
                                    // channel's already open, still there
                                    Console.WriteLine("Still there - delta front = " + currentDeltaFront + " delta behind = " + currentDeltaBehind);
                                    timeOfNextHoldMessage = now.Add(repeatHoldFrequency);
                                    QueuedMessage stillThereMessage = new QueuedMessage(folderStillThere, 0, null);
                                    stillThereMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                                    audioPlayer.playClipImmediately(stillThereMessage, false);
                                }
                            }
                            else
                            {
                                // only say a car is overlapping if the closing speed isn't too high
                                float timeElapsed = (float)currentState.Player.GameSimulationTime - (float)lastState.Player.GameSimulationTime;
                                float closingSpeedInFront = (previousDeltaFront - currentDeltaFront) * currentSpeed / timeElapsed;
                                float closingSpeedBehind = (previousDeltaBehind - currentDeltaBehind) * currentSpeed / timeElapsed;

                                if ((carAlongSideInFront && Math.Abs(closingSpeedInFront) < maxClosingSpeed) ||
                                    (carAlongSideBehind && Math.Abs(closingSpeedBehind) < maxClosingSpeed))
                                {
                                    Boolean frontOverlapIsReducing = carAlongSideInFront && closingSpeedInFront > 0;
                                    Boolean rearOverlapIsReducing = carAlongSideBehind && closingSpeedBehind > 0;
                                    if (rearOverlapIsReducing || (frontOverlapIsReducing && !spotterOnlyWhenBeingPassed))
                                    {
                                        Console.WriteLine("New overlap");
                                        Console.WriteLine("delta front  = " + currentDeltaFront + " closing speed front  = " + closingSpeedInFront);
                                        Console.WriteLine("delta behind = " + currentDeltaBehind + " closing speed behind = " + closingSpeedBehind);
                                        timeOfNextHoldMessage = now.Add(repeatHoldFrequency);
                                        isCurrentlyOverlapping = true;
                                        QueuedMessage holdMessage = new QueuedMessage(folderHoldYourLine, 0, null);
                                        holdMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                                        audioPlayer.playClipImmediately(holdMessage, true, true);                                        
                                    }
                                }
                            }
                        }                        
                    }
                }
                else if (isCurrentlyOverlapping)
                {
                    if (lastSpotterDataIsUsable)
                    {
                        // this is the first chunk of unusable data
                        lastSpotterDataIsUsable = false;
                        timeWhenWeveHadEnoughUnusableData = now.Add(maxTimeToKeepChannelOpenWhileReceivingUnusableData);
                    }
                    if (now > timeWhenWeveHadEnoughUnusableData)
                    {
                        Console.WriteLine("Had " + maxTimeToKeepChannelOpenWhileReceivingUnusableData.Seconds + " seconds of unusable spotter data, closing channel");
                        isCurrentlyOverlapping = false;
                        
                    }
                }
            }
            else if (isCurrentlyOverlapping)
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
                    isCurrentlyOverlapping = false;
                    
                    channelLeftOpenTimerStarted = false;
                    isCurrentlyOverlapping = false;
                }      
            }
        }

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null), false);
            
        }

        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null), false);
            
        }
    }
}
