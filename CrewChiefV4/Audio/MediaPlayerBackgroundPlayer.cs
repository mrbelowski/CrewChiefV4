using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CrewChiefV4.Audio
{
    class MediaPlayerBackgroundPlayer : BackgroundPlayer
    {
        private SynchronizationContext mainThreadContext = null;
        
        // All access should be via UI thread.
        private MediaPlayer backgroundPlayer;

        public MediaPlayerBackgroundPlayer(SynchronizationContext mainThreadContext, String backgroundFilesPath, String defaultBackgroundSound)
        {
            this.mainThreadContext = mainThreadContext;
            this.backgroundFilesPath = backgroundFilesPath;
            this.defaultBackgroundSound = defaultBackgroundSound;
        }

        public override void setBackgroundSound(String backgroundSoundName)
        {
            if (!initialised)
            {
                initialise(backgroundSoundName);
            }
            else if (getBackgroundVolume() > 0 && !muted)
            {
                try
                {
                    this.mainThreadContext.Send(delegate
                    {
                        Console.WriteLine("Setting background sounds file to  " + backgroundSoundName);
                        String path = Path.Combine(backgroundFilesPath, backgroundSoundName);
                        if (initialised)
                        {
                            backgroundPlayer.Close();
                            backgroundPlayer.Volume = 0.0;
                            backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                        }
                    }, null);
                }
                catch (Exception)
                {
                    // ignore - edge case where the app's closing so as removed the UI thread when this call is invoked
                }
            }
        }

        public override void initialise(String initialBackgroundSound)
        {
            if (backgroundPlayer != null
                && initialised
                && getBackgroundVolume() > 0)
                return;

            try
            {
                this.mainThreadContext.Send(delegate
                {
                    if (!initialised && getBackgroundVolume() > 0)
                    {
                        backgroundPlayer = new MediaPlayer();
                        backgroundPlayer.MediaEnded += new EventHandler(backgroundPlayer_MediaEnded);

                        // Start background player muted, as otherwise it causes some noise (sounds like some buffers are flushed).
                        backgroundPlayer.Volume = 0.0;
                        String path = Path.Combine(backgroundFilesPath, initialBackgroundSound);
                        backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                        initialised = true;
                    }
                }, null);
            }
            catch (Exception)
            {
                // ignore - edge case where the app's closing so as removed the UI thread when this call is invoked
            }
        }

        public override void play()
        {
            if (getBackgroundVolume() > 0)
            {
                try
                {
                    this.mainThreadContext.Send(delegate
                    {
                        // this looks like we're doing it the wrong way round but there's a short
                        // delay playing the event sound, so if we kick off the background before the bleep

                        // ensure the BGP is initialised:
                        this.initialise(defaultBackgroundSound);

                        int backgroundDuration = 0;
                        int backgroundOffset = 0;
                        if (backgroundPlayer.NaturalDuration.HasTimeSpan)
                        {
                            backgroundDuration = (backgroundPlayer.NaturalDuration.TimeSpan.Minutes * 60) +
                                backgroundPlayer.NaturalDuration.TimeSpan.Seconds;
                            //Console.WriteLine("Duration from file is " + backgroundDuration);
                            backgroundOffset = Utilities.random.Next(0, backgroundDuration - backgroundLeadout);
                        }
                        //Console.WriteLine("Background offset = " + backgroundOffset);
                        backgroundPlayer.Position = TimeSpan.FromSeconds(backgroundOffset);

                        // Restore the desired volume.
                        backgroundPlayer.Volume = getBackgroundVolume();
                        backgroundPlayer.Play();
                    }, null);
                }
                catch (Exception)
                {
                    // ignore - edge case where the app's closing so as removed the UI thread when this call is invoked
                }
            }
        }

        public override void stop()
        {
            // TODO_THREADS: this needs to be done before form closes.
            if (backgroundPlayer == null || !initialised)
                return;
            try
            {
                this.mainThreadContext.Send(delegate
                {
                    try
                    {
                        backgroundPlayer.Stop();
                        backgroundPlayer.Volume = 0.0;
                    }
                    catch (Exception) { }
                }, null);
            }
            catch (Exception)
            {
                // ignore - edge case where the app's closing so as removed the UI thread when this call is invoked
            }
        }

        public override void mute(bool doMute)
        {
            if (backgroundPlayer == null || !initialised)
                return;

            try
            {
                this.mainThreadContext.Send(delegate
                {
                    if (doMute && !backgroundPlayer.IsMuted)
                        backgroundPlayer.IsMuted = true;
                    else if (!doMute && backgroundPlayer.IsMuted)
                        backgroundPlayer.IsMuted = false;
                }, null);
            }
            catch (Exception)
            {
                // ignore - edge case where the app's closing so as removed the UI thread when this call is invoked
            }
        }

        private void backgroundPlayer_MediaEnded(object sender, EventArgs e)
        {
            try
            {
                this.mainThreadContext.Send(delegate
                {
                    Console.WriteLine("Looping...");
                    backgroundPlayer.Position = TimeSpan.FromMilliseconds(1);
                }, null);
            }
            catch (Exception)
            {
                // ignore - edge case where the app's closing so as removed the UI thread when this call is invoked
            }
        }
    }
}
