/*
 * TODO_THREADS:
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
