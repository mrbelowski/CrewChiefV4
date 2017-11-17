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

        // will be re-used and only disposed when we stop the app or switch background sounds
        private NAudio.Wave.WaveFileReader reader = null;
        // this is initialsed and disposed each time it's used. We hold a reference here so it can be stopped externally
        private NAudio.Wave.WaveOutEvent waveOut = null;

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
            lock (this)
            {
                float volume = getBackgroundVolume();
                if (playing || muted || volume <= 0)
                {
                    return;
                }
                if (!initialised)
                {
                    initialise(this.defaultBackgroundSound);
                }
                int backgroundOffset = Utilities.random.Next(0, (int)backgroundLength.TotalSeconds - backgroundLeadout);
                reader.CurrentTime = TimeSpan.FromSeconds(backgroundOffset);
                long samples = reader.SampleCount;

                NAudio.Wave.SampleProviders.SampleChannel sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(reader);
                sampleChannel.Volume = volume;
                this.waveOut = new NAudio.Wave.WaveOutEvent();
                this.waveOut.DeviceNumber = AudioPlayer.naudioBackgroundPlaybackDeviceId;
                this.waveOut.Init(new NAudio.Wave.SampleProviders.SampleToWaveProvider(sampleChannel));
                this.waveOut.Play();
            }
        }

        public override void stop()
        {
            lock (this)
            {
                if (initialised)
                {
                    try
                    {
                        this.waveOut.Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }

        public override void initialise(String initialBackgroundSound)
        {
            lock (this)
            {
                this.reader = new NAudio.Wave.WaveFileReader(Path.Combine(backgroundFilesPath, initialBackgroundSound));
                backgroundLength = reader.TotalTime;
                initialised = true;
            }
        }

        public override void setBackgroundSound(String backgroundSoundName)
        {
            lock (this)
            {
                if (initialised)
                {
                    stop();
                    try
                    {
                        this.reader.Dispose();
                    }
                    catch (Exception) { }
                }
                this.reader = new NAudio.Wave.WaveFileReader(Path.Combine(backgroundFilesPath, backgroundSoundName));
                backgroundLength = reader.TotalTime;
            }
        }

        public override void dispose()
        {
            lock (this)
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
}
