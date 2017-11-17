using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.Audio
{
    class NAudioBackgroundPlayer : BackgroundPlayer
    {
        private Boolean playing = false;

        private NAudio.Wave.WaveOutEvent waveOut = null;
        private NAudio.Wave.WaveFileReader reader = null;

        private TimeSpan backgroundLength = TimeSpan.Zero;

        public NAudioBackgroundPlayer(String backgroundFilesPath, String defaultBackgroundSound)
        {
            this.backgroundFilesPath = backgroundFilesPath;
            this.defaultBackgroundSound = defaultBackgroundSound;
        }

        public override void mute(bool doMute)
        {
            this.muted = doMute;
        }

        public override void play()
        {
            if (playing || muted)
            {
                return;
            }
            if (!initialised)
            {
                initialise(this.defaultBackgroundSound);
            }
            int backgroundOffset = Utilities.random.Next(0, (int) backgroundLength.TotalSeconds - backgroundLeadout);
            reader.CurrentTime = TimeSpan.FromSeconds(backgroundOffset);
            long samples = reader.SampleCount;

            NAudio.Wave.SampleProviders.SampleChannel sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(reader);
            sampleChannel.Volume = getBackgroundVolume();
            this.waveOut.DeviceNumber = AudioPlayer.naudioBackgroundPlaybackDeviceId;
            this.waveOut.Init(new NAudio.Wave.SampleProviders.SampleToWaveProvider(sampleChannel));
            this.waveOut.Play();
        }

        public override void stop()
        {
            if (initialised)
            {
                waveOut.Stop();
            }
        }

        public override void initialise(String initialBackgroundSound)
        {
            this.reader = new NAudio.Wave.WaveFileReader(Path.Combine(backgroundFilesPath, initialBackgroundSound));
            this.waveOut = new NAudio.Wave.WaveOutEvent();
            backgroundLength = reader.TotalTime;
            initialised = true;
        }

        public override void setBackgroundSound(String backgroundSoundName)
        {
            if (initialised)
            {
                stop();
                try
                {
                    this.reader.Dispose();
                    this.waveOut.Dispose();
                }
                catch (Exception) { }
            }
            this.reader = reader = new NAudio.Wave.WaveFileReader(Path.Combine(backgroundFilesPath, backgroundSoundName));
            this.waveOut = new NAudio.Wave.WaveOutEvent();
            backgroundLength = reader.TotalTime;
        }

        public override void dispose()
        {
            try
            {
                reader.Dispose();
                waveOut.Dispose();
            }
            catch (Exception) { }
            base.dispose();
        }
    }
}
