/*
 * This class is responsible for disposing global shared resources.  Disposal is done after main window is closed and root threads are stopped.
 * 
 * The reason for this is that with multiple threads using shared resources, it is not easy to make shutdown order of those threads predictable.  This
 * is basically a compromise between completely correct implementation (proper locking, and ordered shutdown of threads) and practical solution.  In other
 * words, it's a hack :)
 * 
 * Only resources that are too difficult too assign proper ownership to should be put here as a last resort, this class shouldn't grow much.
 * 
 * Also, see ThreadManager for general threading suggestions for the Crew Chief.
 * 
 * Official website: thecrewchief.org 
 * License: MIT
 */
using CrewChiefV4.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4
{
    // Resources shared without clear ownership.
    public static class GlobalResources
    {
        public static SpeechRecogniser speechRecogniser
        {
            get
            {
                return GlobalResources.speechRecogniserReference;
            }
            set
            {
                Debug.Assert(GlobalResources.speechRecogniserReference == null, "speechRecognizer is reinitialized without proper disposal");
                GlobalResources.speechRecogniserReference = value;
            }
        }

        private static SpeechRecogniser speechRecogniserReference = null;

        public static AudioPlayer audioPlayer
        {
            get
            {
                return GlobalResources.audioPlayerReference;
            }
            set
            {
                Debug.Assert(GlobalResources.audioPlayerReference == null, "audioPlayer is reinitialized without proper disposal");
                GlobalResources.audioPlayerReference = value;
            }
        }

        private static AudioPlayer audioPlayerReference = null;

        public static ControllerConfiguration controllerConfiguration
        {
            get
            {
                return GlobalResources.controllerConfigurationReference;
            }
            set
            {
                Debug.Assert(GlobalResources.controllerConfigurationReference == null, "controllerConfiguration is reinitialized without proper disposal");
                GlobalResources.controllerConfigurationReference = value;
            }
        }

        private static ControllerConfiguration controllerConfigurationReference = null;

        public static void Dispose()
        {
            if (GlobalResources.speechRecogniserReference != null)
            {
                GlobalResources.speechRecogniserReference.recognizeAsyncCancel();
                GlobalResources.speechRecogniserReference.stopTriggerRecogniser();

                // TODO: do we need to wait a few millis here? The cancel call is async, the SRE will may be finishing its work when .Dispose is called
                GlobalResources.speechRecogniserReference.Dispose();
                GlobalResources.speechRecogniserReference = null;
            }

            if (GlobalResources.audioPlayerReference != null)
            {
                GlobalResources.audioPlayerReference.Dispose();
                GlobalResources.audioPlayerReference = null;
            }

            if (GlobalResources.controllerConfigurationReference != null)
            {
                GlobalResources.controllerConfigurationReference.Dispose();
                GlobalResources.controllerConfigurationReference = null;
            }
        }
    }
}
