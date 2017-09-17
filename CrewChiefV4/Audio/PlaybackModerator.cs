﻿/*
 * The idea behind PlaybackModerator class is to allow us to adjust playback after all the high level logic is evaluated,
 * messages resolved, duplicates removed etc.  It is plugged into SingleSound play and couple of other low level places.
 * Currently, the only two things it does is injects fake beep-out/in between Spotter and Chief messages and decides which 
 * sounds should be used for open/close of radio channel.  In the future we might use it to mess with playback: remove/add sounds,
 * corrupt them etc.
 * 
 * Official website: thecrewchief.org 
 * License: MIT
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CrewChiefV4.Audio
{
    public static class PlaybackModerator
    {
        private static bool enableTracing = false;

        // This field is necessary to avoid construction of NoisyCartesianCoordinateSpotter before AudioPlayer.
        private static string defaultSpotterId = "Jim (default)";
        private static bool isSpotterAndChiefSameVoice = UserSettings.GetUserSettings().getString("spotter_name") == defaultSpotterId;
        private static bool insertBeepOutBetweenSpotterAndChief = UserSettings.GetUserSettings().getBoolean("insert_beep_out_between_spotter_and_chief");
        private static bool insertBeepInBetweenSpotterAndChief = UserSettings.GetUserSettings().getBoolean("insert_beep_in_between_spotter_and_chief");
        private static bool rejectMessagesWhenTalking = UserSettings.GetUserSettings().getBoolean("reject_message_when_talking");
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
            return GetSuggestedStartBleep("start_bleep" /*chiefBleepSoundName*/, "alternate_start_bleep" /*spotterBleepSoundName*/);
        }

        private static string GetSuggestedStartBleep(string chiefBleepSoundName, string spotterBleepSoundName)
        {
            var resolvedSoundName = chiefBleepSoundName;

            // If there's nothing to do return default value.
            if (!PlaybackModerator.IsFakeBleepInjectionEnabled())
                return resolvedSoundName;

            // We need to capture the fact that channel was opened as Spotter or Chief, so that
            // subsequent injection is aware of that.
            PlaybackModerator.lastSoundWasSpotter = false;

            if (PlaybackModerator.PrevFirstKeyWasSpotter())
            {
                // Spotter uses opposite bleeps.
                resolvedSoundName = spotterBleepSoundName;
                PlaybackModerator.lastSoundWasSpotter = true;

                PlaybackModerator.Trace("Opening radio channel as Spotter");
            }
            else
                PlaybackModerator.Trace("Opening radio channel as Chief");

            return resolvedSoundName;
        }

        public static string GetSuggestedBleepShorStart()
        {
            return GetSuggestedStartBleep("short_start_bleep" /*chiefBleepSoundName*/, "alternate_short_start_bleep" /*spotterBleepSoundName*/);
        }

        public static string GetSuggestedBleepEnd()
        {
            var resolvedSoundName = "end_bleep";

            // If there's nothing to do return default value.
            if (!PlaybackModerator.IsFakeBleepInjectionEnabled())
                return resolvedSoundName;

            if (PlaybackModerator.PrevLastKeyWasSpotter())
            {
                // Spotter uses opposite bleeps.
                resolvedSoundName = "alternate_end_bleep";

                PlaybackModerator.Trace("Closing radio channel as Spotter");

                if (PlaybackModerator.lastSoundPreProcessed != null
                    && !PlaybackModerator.lastSoundPreProcessed.isSpotter)
                    PlaybackModerator.Trace(string.Format(
                        "WARNING Last key and last sound pre-processed do not agree on role: {0} vs {1} ", 
                        PlaybackModerator.lastSoundPreProcessed.fullPath, PlaybackModerator.prevLastKey));
            }
            else
            {
                PlaybackModerator.Trace("Closing radio channel as Chief");

                if (PlaybackModerator.lastSoundPreProcessed != null
                    && PlaybackModerator.lastSoundPreProcessed.isSpotter)
                    PlaybackModerator.Trace(string.Format(
                        "WARNING Last key and last sound pre-processed do not agree on role: {0} vs {1} ", 
                        PlaybackModerator.lastSoundPreProcessed.fullPath, PlaybackModerator.lastSoundPreProcessed.fullPath));
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

            Console.WriteLine(string.Format("PlaybackModerator: {0}", msg));
        }

        //public static void PostProcessSound()
        //{ }

        public static bool ShouldPlaySound()
        {
            return !rejectMessagesWhenTalking || !SpeechRecogniser.waitingForSpeech;
        }

        private static void InjectBeepOutIn(SingleSound sound)
        {
            Debug.Assert(PlaybackModerator.audioPlayer != null, "audioPlayer is not set.");

            // Only consider injection is preference is set and Spotter and Chief are different personas.
            if (!PlaybackModerator.IsFakeBleepInjectionEnabled())
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
                string keyBleepOut = null;
                string keyBleepIn = null;

                string traceMsgPostfix = null;
                if (isSpotterSound)
                {
                    // Spotter uses opposite blips.
                    keyBleepOut = "end_bleep";  // Chief uses regular bleeps.
                    keyBleepIn = "alternate_short_start_bleep";

                    traceMsgPostfix = "Spotter interrupted Chief.";
                }
                else  // Chief comes in.
                {
                    keyBleepOut = "alternate_end_bleep";  // Spotter uses alternate bleeps
                    keyBleepIn = "short_start_bleep";

                    traceMsgPostfix = "Chief interrupted Spotter.";
                }

                PlaybackModerator.Trace(string.Format("Injecting: {0} and {1} messages. {2}", keyBleepOut, keyBleepIn, traceMsgPostfix));

                // insert bleep out/in
                if (PlaybackModerator.insertBeepOutBetweenSpotterAndChief)
                    PlaybackModerator.audioPlayer.getSoundCache().Play(keyBleepOut);

                // would be nice to have some slight random silence here
                if (PlaybackModerator.insertBeepInBetweenSpotterAndChief)
                    PlaybackModerator.audioPlayer.getSoundCache().Play(keyBleepIn);
            }

            PlaybackModerator.lastSoundWasSpotter = isSpotterSound;
        }

        private static bool IsFakeBleepInjectionEnabled()
        {
            return !PlaybackModerator.isSpotterAndChiefSameVoice
                && (PlaybackModerator.insertBeepOutBetweenSpotterAndChief || PlaybackModerator.insertBeepInBetweenSpotterAndChief);
        }

        private static bool PrevFirstKeyWasSpotter()
        {
            // The spotter 'radio check' is radio_check_SpotterName, so also check for this:
            return !string.IsNullOrWhiteSpace(PlaybackModerator.prevFirstKey)
                && (PlaybackModerator.prevFirstKey.Contains("spotter") || PlaybackModerator.prevFirstKey.Contains("radio_check_"));
        }

        private static bool PrevLastKeyWasSpotter()
        {
            // The spotter 'radio check' is radio_check_SpotterName, so also check for this:
            return !string.IsNullOrWhiteSpace(PlaybackModerator.prevLastKey)
                && (PlaybackModerator.prevLastKey.Contains("spotter") || PlaybackModerator.prevLastKey.Contains("radio_check_"));
        }
    }
}
