﻿/*
 * 
 * Official website: thecrewchief.org 
 * License: MIT
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrewChiefV4
{
    public static class ThreadManager
    {
        private const int THREAD_ALIVE_CHECK_PERIOD_MILLIS = 200;
        private const int THREAD_ALIVE_TOTAL_WAIT_SECS = 5;
        private const int THREAD_ALIVE_WAIT_ITERATIONS = ThreadManager.THREAD_ALIVE_TOTAL_WAIT_SECS * 1000 / ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS;

        private static List<Thread> rootThreads = new List<Thread>();
        public static void RegisterRootThread(Thread t)
        {
            lock (MainWindow.instanceLock)
            {
                if (MainWindow.instance != null
                    && MainWindow.instance.InvokeRequired)
                {
                    Debug.Assert(false, "This method is supposed to be invoked only from the UI thread.");
                    return;
                }
            }

            ThreadManager.rootThreads.Add(t);
        }

        public static void UnregisterRootThreads()
        {
            // This is special case, no locking here because this is only invoked on a UI thread, and lock is alread held.
            if (MainWindow.instance != null
                && MainWindow.instance.InvokeRequired)
            {
                Debug.Assert(false, "This method is supposed to be invoked only from the UI thread.");
                return;
            }

            ThreadManager.rootThreads.Clear();
        }

        public static void DoWatchStartup(CrewChief cc)
        {
            new Thread(() =>
            {
                ThreadManager.WaitForRootThreadsStart(cc);
            }).Start();
        }

        public static void DoWatchStop(CrewChief cc)
        {
            new Thread(() =>
            {
                ThreadManager.WaitForRootThreadsStop(cc);
            }).Start();
        }

        // This is not strictly necessary, because all this really does is makes sure .Start has been called on a thread, which is easy
        // to achieve.  Still, do this for symmetry.
        public static bool WaitForRootThreadsStart(CrewChief cc)
        {
            try
            {
                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null
                       && !MainWindow.instance.InvokeRequired)
                    {
                        Debug.Assert(false, "This method cannot be invoked from the UI thread.");
                        return false;
                    }
                }

                // TODO_THREADS: ok, this won't work.  If anyone tries to write to console while we are waiting, this will deadlock.  Need to keep thinking.
                // To reduce risk of a deadlock, keep retrying by waking main thread up.
                ThreadManager.Trace("Wating for root threads to start...");
                for (int i = 0; i < ThreadManager.THREAD_ALIVE_WAIT_ITERATIONS; ++i)
                {
                    var allThreadsRunning = true;
                    foreach (var t in rootThreads)
                    {
                        if (!t.IsAlive)
                        {
                            ThreadManager.Trace("Thread not running - " + t.Name);
                            allThreadsRunning = false;
                            break;
                        }
                    }

                    if (allThreadsRunning)
                    {
                        ThreadManager.Trace("Root threads started");
                        var isTraceFileSet = false;
                        lock (MainWindow.instanceLock)
                        {
                            isTraceFileSet = MainWindow.instance != null && string.IsNullOrWhiteSpace(MainWindow.instance.filenameTextbox.Text);
                        }

                        if (isTraceFileSet)
                        {
                            ThreadManager.Trace("Wating for run thread to read data file...");
                            while (true)
                            {
                                if (cc.dataFileReadDone)
                                {
                                    ThreadManager.Trace("Run thread data file read done");
                                    break;
                                }

                                Thread.Sleep(ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS);
                            }
                        }

                        return true;
                    }

                    Thread.Sleep(ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS);
                }

                ThreadManager.Trace("Wait for root threads start failed:");
                ThreadManager.TraceRootThreadStats();

                return false;
            }
            finally
            {
                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null)
                    {
                        MainWindow.instance.Invoke((MethodInvoker)delegate
                        {
                            MainWindow.instance.startApplicationButton.Enabled = true;
                        });
                    }
                }
            }
        }

        public static bool WaitForRootThreadsStop(CrewChief cc)
        {
            try
            {
                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null
                        && !MainWindow.instance.InvokeRequired)
                    {
                        Debug.Assert(false, "This method cannot be invoked from the UI thread.");
                        return false;
                    }
                }

                var recordSessionChecked = false;
                lock (MainWindow.instanceLock)
                {
                    recordSessionChecked = MainWindow.instance != null && MainWindow.instance.recordSession.Checked;
                }

                if (recordSessionChecked)
                {
                    ThreadManager.Trace("Wating for run thread to dump data file...");
                    while (true)
                    {
                        if (cc.dataFileDumpDone)
                        {
                            ThreadManager.Trace("Run thread data file dump done");
                            break;
                        }

                        Thread.Sleep(ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS);
                    }
                }

                ThreadManager.Trace("Wating for root threads to stop...");
                for (int i = 0; i < ThreadManager.THREAD_ALIVE_WAIT_ITERATIONS; ++i)
                {
                    var allThreadsStopped = true;
                    foreach (var t in rootThreads)
                    {
                        if (t.IsAlive)
                        {
                            // TODO_THREADS: remove?
                            ThreadManager.Trace("Thread still alive - " + t.Name);
                            allThreadsStopped = false;
                            break;
                        }
                    }

                    if (allThreadsStopped)
                    {
                        ThreadManager.Trace("Root threads stopped");
                        return true;
                    }
                    
                    Thread.Sleep(ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS);
                }

                ThreadManager.Trace("Wait for root threads stop failed:");
                ThreadManager.TraceRootThreadStats();

                return false;
            }
            finally
            {
                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null)
                    {
                        MainWindow.instance.Invoke((MethodInvoker)delegate
                        {
                            ThreadManager.UnregisterRootThreads();
                            MainWindow.instance.startApplicationButton.Enabled = true;
                        });
                    }
                }
            }
        }

        private static void TraceRootThreadStats()
        {
            // If we run into bad problems, we might need to also get stack trace out.
            foreach (var t in rootThreads)
                ThreadManager.Trace(string.Format("Thread Name: {0}  ThreadState: {1}  IsAlive: {2}\n", t.Name, t.ThreadState, t.IsAlive));
        }

        private static void Trace(string msg)
        {
            //if (!PlaybackModerator.enableTracing)
             //   return;

            Console.WriteLine(string.Format("ThreadManager: {0}", msg));
        }
    }
}
