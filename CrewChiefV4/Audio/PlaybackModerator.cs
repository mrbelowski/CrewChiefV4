/*
 * 
 * 
 * thecrewchief.org 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.Audio
{
    public static class PlaybackModerator
    {
        private static bool enableTracing = false;

        // This field is necessary to avoid construction of NoisyCartesianCoordinateSpotter before AudioPlayer.
        private static string defaultSpotterId = "Jim (default)";
        private static bool isSpotterAndChiefSameVoice = UserSettings.GetUserSettings().getString("spotter_name") == defaultSpotterId;
        private static bool insertBeepOutInBetweenSpotterAndChief = UserSettings.GetUserSettings().getBoolean("insert_beep_between_spotter_and_chief");

        public static void PreProcessSound(SingleSound sound)
        {
            PlaybackModerator.Trace($"Pre-Processing sound: {sound.fullPath}  isSpotter: {sound.isSpotter}  isBleep: {sound.isBleep} ");

            /*
            if (!this.isSpotterAndChiefSameVoice && this.insertBeepOutInBetweenSpotterAndChief)
            {
                // Inject bleep out/in if needed.
                var isSpotterKey = key.StartsWith("spotter");  // Alternatively, we could assign a role to each queued sound.  We'll need this if we get more "personas" than Chief and Spotter.
                if (((!this.lastAddedKeyWasSpotter && isSpotterKey)  // If we are flipping from the Chief to Spotter
                    || (this.lastAddedKeyWasSpotter && !isSpotterKey))  // Or from the Spotter to Chief
                    && this.isChannelOpen())  // And, channel is still open
                {
                    // Ok, so idea here is that Chief and Spotter have different bleeps.  So we use opposing sets.
                    String keyBleepOut = null;
                    String keyBleepIn = null;
                    if (isSpotterKey)
                    {
                        // Spotter uses opposite blips.
                        keyBleepOut = "end_bleep";  // Chief uses regular bleeps.
                        keyBleepIn = "alternate_short_start_bleep";
                    }
                    else  // Chief comes in.
                    {
                        keyBleepOut = "alternate_end_bleep";  // Spotter uses alternate bleeps
                        keyBleepIn = "short_start_bleep";
                    }

                    // the message folders will be null for messages with delayed (just-in-time) resolution, so rather than inserting
                    // them directly, add them to the delayed message's stuff to resolve.
                    if (queuedMessage.messageFolders == null)
                    {
                        queuedMessage.delayedMessagBeepOut = keyBleepOut;
                        queuedMessage.delayedMessagBeepIn = keyBleepIn;
                    }
                    else
                    {
                        // insert bleep out/in
                        queuedMessage.messageFolders.Insert(0, keyBleepOut);
                        // would be nice to have some slight random silence here
                        queuedMessage.messageFolders.Insert(1, keyBleepIn);
                    }
                }

                this.lastAddedKeyWasSpotter = isSpotterKey;
            }*/

        }

        public static void SetTracing(bool enabled)
        {
            PlaybackModerator.enableTracing = enabled;
        }

        private static void Trace(string msg)
        {
            if (!PlaybackModerator.enableTracing)
                return;

            Console.WriteLine(msg);
        }
        //public static void PostProcessSound()
        //{ }

        //public static bool ShouldPlaySound()
        //{ }

    }
}
