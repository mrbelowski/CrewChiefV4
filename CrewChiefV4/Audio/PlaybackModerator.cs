/*
 * 
 * 
 * thecrewchief.org 
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static bool lastSoundWasSpotter = false;
        private static AudioPlayer audioPlayer = null;

        private static string prevFirstKey = "";
        private static string prevLastKey = "";
        private static SingleSound lastSoundPreProcessed = null;

        public static void PreProcessSound(SingleSound sound)
        {
            if (PlaybackModerator.audioPlayer == null)
                return;

            //PlaybackModerator.Trace($"Pre-Processing sound: {sound.fullPath}  isSpotter: {sound.isSpotter}  isBleep: {sound.isBleep} ");

            PlaybackModerator.InjectBeepOutIn(sound);

            PlaybackModerator.lastSoundPreProcessed = sound;
        }

        public static void PreProcessAddedKeys(List<string> keys)
        {
            if (keys == null || keys.Count == 0)
                return;

            PlaybackModerator.prevFirstKey = keys.First();
            PlaybackModerator.prevLastKey = keys.Last();
        }

        public static string GetSuggestedBleepStart()
        {
            var resolvedSoundName = "start_bleep";

            // If there's nothing to do return default value.
            if (PlaybackModerator.isSpotterAndChiefSameVoice
                || !PlaybackModerator.insertBeepOutInBetweenSpotterAndChief)
                return resolvedSoundName;

            if (!string.IsNullOrWhiteSpace(PlaybackModerator.prevFirstKey)
                && PlaybackModerator.prevFirstKey.Contains("spotter"))
            {
                // Spotter uses opposite bleeps.
                resolvedSoundName = "alternate_start_bleep";
            }

            return resolvedSoundName;
        }

        public static string GetSuggestedBleepShorStart()
        {
            var resolvedSoundName = "short_start_bleep";

            // If there's nothing to do return default value.
            if (PlaybackModerator.isSpotterAndChiefSameVoice
                || !PlaybackModerator.insertBeepOutInBetweenSpotterAndChief)
                return resolvedSoundName;

            if (!string.IsNullOrWhiteSpace(PlaybackModerator.prevFirstKey) 
                && PlaybackModerator.prevFirstKey.Contains("spotter"))
            {
                // Spotter uses opposite bleeps.
                resolvedSoundName = "alternate_short_start_bleep";
            }

            return resolvedSoundName;
        }

        public static string GetSuggestedBleepEnd()
        {
            var resolvedSoundName = "end_bleep";

            // If there's nothing to do return default value.
            if (PlaybackModerator.isSpotterAndChiefSameVoice
                || !PlaybackModerator.insertBeepOutInBetweenSpotterAndChief)
                return resolvedSoundName;

            if (!string.IsNullOrWhiteSpace(PlaybackModerator.prevLastKey)
                && PlaybackModerator.prevLastKey.Contains("spotter"))
            {
                // Spotter uses opposite bleeps.
                resolvedSoundName = "alternate_end_bleep";

                if (PlaybackModerator.lastSoundPreProcessed != null
                    && !PlaybackModerator.lastSoundPreProcessed.isSpotter)
                    PlaybackModerator.Trace(
                        $"WARNING Last key and last sound pre-processed do not agree on role: {PlaybackModerator.lastSoundPreProcessed.fullPath} vs {PlaybackModerator.prevLastKey} ");
            }
            else
            {
                if (PlaybackModerator.lastSoundPreProcessed != null
                    && PlaybackModerator.lastSoundPreProcessed.isSpotter)
                    PlaybackModerator.Trace(
                        $"WARNING Last key and last sound pre-processed do not agree on role: {PlaybackModerator.lastSoundPreProcessed.fullPath} vs {PlaybackModerator.prevLastKey} ");
            }

            return resolvedSoundName;
        }


        public static void SetTracing(bool enabled)
        {
            PlaybackModerator.enableTracing = enabled;
        }

        public static void SetAudioPlayer(AudioPlayer audioPlayer)
        {
            Debug.Assert(PlaybackModerator.audioPlayer == null, "audioPlayer is set already");

            PlaybackModerator.audioPlayer = audioPlayer;
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

        // TODO: next issue to figure out is how to open channel correctly depending on who is about to talk?
        // Audio player has that info sort of.  There's only one openRadioChannel call, so it is fairly straightforward.
        // On channel close, we need to check who was last to talk and use appropriate bleep. 
        private static void InjectBeepOutIn(SingleSound sound)
        {
            Debug.Assert(PlaybackModerator.audioPlayer != null, "audioPlayer is not set.");

            // Only consider injection is preference is set and Spotter and Chief are different personas.
            if (PlaybackModerator.isSpotterAndChiefSameVoice 
                || !PlaybackModerator.insertBeepOutInBetweenSpotterAndChief)
                return;

            // Skip bleep sounds.
            if (sound.isBleep)
                return;

            // Inject bleep out/in if needed.
            var isSpotterSound = sound.isSpotter;  // Alternatively, we could assign a role to each queued sound.  We'll need this if we get more "personas" than Chief and Spotter.
            if (((!PlaybackModerator.lastSoundWasSpotter && isSpotterSound)  // If we are flipping from the Chief to Spotter
                || (PlaybackModerator.lastSoundWasSpotter && !isSpotterSound))  // Or from the Spotter to Chief
                && PlaybackModerator.audioPlayer.isChannelOpen())  // And, channel is still open
            {
                // Ok, so idea here is that Chief and Spotter have different bleeps.  So we use opposing sets.
                String keyBleepOut = null;
                String keyBleepIn = null;
                if (isSpotterSound)
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

                PlaybackModerator.Trace($"Injecting: {keyBleepOut} and {keyBleepIn} messages.");

                // insert bleep out/in
                PlaybackModerator.audioPlayer.getSoundCache().Play(keyBleepOut);
                PlaybackModerator.audioPlayer.getSoundCache().Play(keyBleepIn);
                //                    queuedMessage.messageFolders.Insert(0, keyBleepOut);
                // would be nice to have some slight random silence here
                //                  queuedMessage.messageFolders.Insert(1, keyBleepIn);
            }

            PlaybackModerator.lastSoundWasSpotter = isSpotterSound;
        }
    }
}
