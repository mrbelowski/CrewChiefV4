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
        
        // All access should be done via main thread, because that is the thread owning MediaPlayer object.
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
                Console.WriteLine("Setting background sounds file to  " + backgroundSoundName);

                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null)
                    {
                        this.mainThreadContext.Post(delegate
                        {
                            if (MainWindow.instance == null)
                            {
                                return;
                            }
                            String path = Path.Combine(backgroundFilesPath, backgroundSoundName);
                            if (initialised)
                            {
                                backgroundPlayer.Close();
                                backgroundPlayer.Volume = 0.0;
                                backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                            }
                        }, null);
                    }
                }
            }
        }

        public override void initialise(String initialBackgroundSound)
        {
            if (backgroundPlayer != null
                && initialised
                && getBackgroundVolume() > 0)
                return;

            lock (MainWindow.instanceLock)
            {
                if (MainWindow.instance != null)
                {
                    this.mainThreadContext.Post(delegate
                    {
                        if (MainWindow.instance == null)
                        {
                            return;
                        }

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
            }
        }

        public override void play()
        {
            if (getBackgroundVolume() > 0)
            {
                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null)
                    {
                        this.mainThreadContext.Post(delegate
                        {
                            if (MainWindow.instance == null)
                            {
                                return;
                            }

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
                                backgroundOffset = Utilities.random.Next(0, backgroundDuration - backgroundLeadout);
                            }
                            backgroundPlayer.Position = TimeSpan.FromSeconds(backgroundOffset);

                            // Restore the desired volume.
                            backgroundPlayer.Volume = getBackgroundVolume();
                            backgroundPlayer.Play();
                        }, null);
                    }
                }
            }
        }

        public override void stop()
        {
            if (backgroundPlayer == null || !initialised)
                return;

            lock (MainWindow.instanceLock)
            {
                if (MainWindow.instance != null)
                {
                    this.mainThreadContext.Post(delegate
                    {
                        if (MainWindow.instance == null)
                        {
                            return;
                        }

                        try
                        {
                            backgroundPlayer.Stop();
                            backgroundPlayer.Volume = 0.0;
                        }
                        catch (Exception) { }
                    }, null);
                }
            }
        }

        public override void mute(bool doMute)
        {
            if (backgroundPlayer == null || !initialised)
                return;

            lock (MainWindow.instanceLock)
            {
                if (MainWindow.instance != null)
                {
                    this.mainThreadContext.Post(delegate
                    {
                        if (MainWindow.instance == null)
                        {
                            return;
                        }

                        if (doMute && !backgroundPlayer.IsMuted)
                            backgroundPlayer.IsMuted = true;
                        else if (!doMute && backgroundPlayer.IsMuted)
                            backgroundPlayer.IsMuted = false;
                    }, null);
                }
            }
        }

        private void backgroundPlayer_MediaEnded(object sender, EventArgs e)
        {
            // This is called from the main thread, no need to marshal.
            Console.WriteLine("Looping...");
            backgroundPlayer.Position = TimeSpan.FromMilliseconds(1);
        }
    }
}
