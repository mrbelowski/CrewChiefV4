/*
 * This class' responsibility is to provide corrdination around creation and destruction of threads in CC.  Here's proposal for strategy of
 * dealing with threads in CC, aka "Architecture":
 * 
 * If any of the threads creates a thread, ideally it has to wait for a child thread to exit when parent thread exits.  Another alternative is
 * strict locking of shared resources.  However, waiting and locking is not always practical.  ThreadManager class is intended to
 * help Start/Stop/Shutdown of CC to be predictable and clean by allowing threads to be registered and waited on at Start/Stop/Shutdown.
 * This avoids unpredictable crashes/exceptions while accessing resources no longer available.
 * 
 * 1. There are three categories of threads in CC:
 * 
 *      - Root threads: generally started from UI thread on Start button click, and stopped on Stop button click.
 *        Each root thread has to be named and registered with ThreadManager.RegisterRootThread.
 * 
 *      - Temporary threads: various short-lived/helper threads.  For example, wait for speech, or waiting for something to happen.
 *        Generally, started by Root threads or their children.
 *        
 *        If a parent thread does not wait for a temporary thread on termination, temporary thread has to named and be registered
 *        with ThreadManager.RegisterTemporaryThread.  Additionally, when a new instance of temporary thread is created,
 *        ThreadManager.UnregisterTemporaryThread has to be called on previous one.  See existing code on how to do this.
 * 
 *      - Resource threads: sound loading, caching, downloading etc.  Those threads are generally independent threads that can run without
 *        CC root threads running.  Each resource thread has to be named and registered with ThreadManager.RegisterResourceThread.
 *
 * 2. Each thread should release/dispose it's resources on exit, unless it is too complicated/unpractical (See GlobalResources class)
 * 
 * 3. Global shared resources will be released after form close (and all threads stopped, if they stop within agreed time,
 *    otherwise - undefined behavior).
 * 
 * 4. Access to the main window should be synchronized with MainWindow.instanceLock.  Be extra careful, if you are marshalling to the main thread,
 *    do a Post, not Send, so that lock is not held.  Failing to follow above might cause deadlocks.
 * 
 * 5. For Sleeps longer than 2 seconds, consider using Utilities.InterruptedSleep to avoid Shutdown/Stop delays.
 * 
 * 6. For worker threads that pump some data, don't just use Sleep, use Events to wake them up.
 * 
 * Official website: thecrewchief.org 
 * License: MIT
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CrewChiefV4
{
    public static class ThreadManager
    {
        private const int THREAD_ALIVE_CHECK_PERIOD_MILLIS = 200;
        private const int THREAD_ALIVE_TOTAL_WAIT_SECS = 5;
        private const int THREAD_ALIVE_WAIT_ITERATIONS = ThreadManager.THREAD_ALIVE_TOTAL_WAIT_SECS * 1000 / ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS;

        private const int SHUTDOWN_THREAD_ALIVE_CHECK_PERIOD_MILLIS = 50;
        private const int SHUTDOWN_THREAD_ALIVE_TOTAL_WAIT_SECS = 5;
        private const int SHUTDOWN_THREAD_ALIVE_WAIT_ITERATIONS = ThreadManager.SHUTDOWN_THREAD_ALIVE_TOTAL_WAIT_SECS * 1000 / ThreadManager.SHUTDOWN_THREAD_ALIVE_CHECK_PERIOD_MILLIS;

        private const int TEMP_THREADS_TRIM_THRESHOLD = 50;

        private static List<Thread> rootThreads = new List<Thread>();
        private static HashSet<Thread> temporaryThreads = new HashSet<Thread>();
        private static List<Thread> resourceThreads = new List<Thread>();

        private static object rootThreadsLock = new object();
        private static object temporaryThreadsLock = new object();
        private static object resourceThreadsLock = new object();

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

            lock (ThreadManager.rootThreadsLock)
            {
                ThreadManager.rootThreads.Add(t);
            }
        }

        public static void RegisterTemporaryThread(Thread t)
        {
            lock (ThreadManager.temporaryThreadsLock)
            {
                ThreadManager.TrimTemporaryThreads();

                if (!ThreadManager.temporaryThreads.Contains(t))
                    ThreadManager.temporaryThreads.Add(t);
                else
                    Debug.Assert(false, "Temporary thread already registered, this should not happen.");
            }
        }

        private static void TrimTemporaryThreads()
        {
            lock (ThreadManager.temporaryThreadsLock)
            {
                // Normally, temporary threads are short-lived and kicked off infrequently.  Sometimes, however,
                // new thread of the same work is kicked off while previous instance is still alive.  Such threads
                // are kept in a temporaryThreads collection upon unregistering them, so that they are still under TM control.
                //
                // However, that means that temporaryThreads collection grows unbounded.  In the extreme case, this
                // is also an indicator of a severe bug in the app itsel, and needs to be investigated, understood and fixed.
                if (ThreadManager.temporaryThreads.Count > ThreadManager.TEMP_THREADS_TRIM_THRESHOLD)
                {
                    var msg = "Trimming temporary thread collection.  This should not happen normally, see log file for ThreadManager warnings.";
                    Debug.Assert(false, msg);
                    ThreadManager.Trace(msg);

                    // Remove temporary threads that aren't running anymore.
                    ThreadManager.temporaryThreads.RemoveWhere(t => !t.IsAlive);
                }
            }
        }

        private static void UnregisterRootThreads()
        {
            lock (ThreadManager.rootThreadsLock)
            {
                ThreadManager.rootThreads.Clear();
            }
        }

        public static void RegisterResourceThread(Thread t)
        {
            lock (ThreadManager.resourceThreadsLock)
            {
                ThreadManager.resourceThreads.Add(t);
            }
        }

        // It might be valuable to make this function optionally wait on thread.
        public static void UnregisterTemporaryThread(Thread t)
        {
            if (t == null)
                return;

            lock (ThreadManager.temporaryThreadsLock)
            {
                if (ThreadManager.temporaryThreads.Contains(t))
                {
                    // This is not necessarily a problem, but this message is here to make thread author think about spammy threads.
                    var warningMsg = "WARNING - Temporary thread is still alive upon unregistering, we might need to investigate here.";
                    Debug.Assert(!t.IsAlive, warningMsg);

                    if (t.IsAlive)
                        ThreadManager.Trace(warningMsg + "  Name - " + t.Name);
                    else
                        ThreadManager.temporaryThreads.Remove(t);
                }
                else
                    Debug.Assert(false, "Temporary thread is not registered, this should not happen.");
            }
        }


        public static void DoWatchStartup(CrewChief cc)
        {
            // Thread does not need registration as it is watcher thread.
            new Thread(() =>
            {
                ThreadManager.WaitForRootThreadsStart(cc);
            }).Start();
        }

        public static void DoWatchStop(CrewChief cc)
        {
            // Thread does not need registration as it is watcher thread.
            new Thread(() =>
            {
                ThreadManager.WaitForRootAndTemporaryThreadsStop(cc);
            }).Start();
        }

        //
        // This method is not strictly necessary, because all it really does is makes sure .Start has been called on a thread, which is easy
        // to achieve.  Still, do this for symmetry.
        // For internal trace playback scenario, it waits for file read to complete.
        //
        // Upon exit, enables Start/Stop button.
        //
        private static bool WaitForRootThreadsStart(CrewChief cc)
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

                ThreadManager.Trace("Wating for root threads to start...");
                for (int i = 0; i < ThreadManager.THREAD_ALIVE_WAIT_ITERATIONS; ++i)
                {
                    var allThreadsRunning = true;
                    lock (ThreadManager.rootThreadsLock)
                    {
                        foreach (var t in ThreadManager.rootThreads)
                        {
                            if (!t.IsAlive)
                            {
                                allThreadsRunning = false;
                                break;
                            }
                        }
                    }

                    if (allThreadsRunning)
                    {
                        ThreadManager.Trace("Root threads started");
                        var isTraceFileSet = false;
                        lock (MainWindow.instanceLock)
                        {
                            isTraceFileSet = MainWindow.instance != null && !string.IsNullOrWhiteSpace(MainWindow.instance.filenameTextbox.Text);
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
                ThreadManager.TraceThreadSetStats(ThreadManager.rootThreads, ThreadManager.rootThreadsLock, "Root");

                return false;
            }
            finally
            {
                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null)
                    {
                        // Not entirely sure if Invoke is necessary here, seems to work well without it.  If we decide
                        // invoke is needed, Post might be best option here.
                        MainWindow.instance.startApplicationButton.Enabled = true;
                    }
                }
            }
        }

        //
        // This method waits for:
        // - Data file dump to complete.
        // - Root threads to stop
        // - Temporary threads to stop
        //
        // Upon exit, enables Start/Stop button and calls MainWindow.uiSyncAppStop to update UI controls.
        //
        private static bool WaitForRootAndTemporaryThreadsStop(CrewChief cc)
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

                var rootThreadsStopped = ThreadManager.ThreadWaitHelper(
                    ThreadManager.rootThreads,
                    ThreadManager.rootThreadsLock,
                    "Root",
                    ThreadManager.THREAD_ALIVE_WAIT_ITERATIONS,
                    ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS,
                    false /*isShutdown*/);

                var tempThreadsStopped = ThreadManager.ThreadWaitHelper(
                    ThreadManager.temporaryThreads,
                    ThreadManager.temporaryThreadsLock,
                    "Temporary",
                    ThreadManager.THREAD_ALIVE_WAIT_ITERATIONS,
                    ThreadManager.THREAD_ALIVE_CHECK_PERIOD_MILLIS,
                    false /*isShutdown*/);

                return rootThreadsStopped && tempThreadsStopped;
            }
            finally
            {
                lock (MainWindow.instanceLock)
                {
                    if (MainWindow.instance != null)
                    {
                        ThreadManager.UnregisterRootThreads();
                        MainWindow.instance.uiSyncAppStop();
                        MainWindow.instance.startApplicationButton.Enabled = true;
                    }
                }
            }
        }

        // Note: wait for file dump on shutdown is not supported.
        public static bool WaitForRootThreadsShutdown()
        {
            // There's no race here, because Root threads are added on the UI thread,
            // and main window is closed already.
            if (ThreadManager.rootThreads.Count == 0)
                return true;

            var rootThreadsStopped = ThreadManager.ThreadWaitHelper(
                ThreadManager.rootThreads,
                ThreadManager.rootThreadsLock,
                "Root",
                ThreadManager.SHUTDOWN_THREAD_ALIVE_WAIT_ITERATIONS,
                ThreadManager.SHUTDOWN_THREAD_ALIVE_CHECK_PERIOD_MILLIS,
                true /*isShutdown*/);

            // Note: wait for file dump on shutdown is not supported, if this assert annoys you, remove it.
            // Alternatively, change this code to wait for dump to finish?
            Debug.Assert(rootThreadsStopped, "Shutdown - Wait for root threads stop failed, please investigate.");

            return rootThreadsStopped;
        }

        public static bool WaitForTemporaryThreadsShutdown()
        {
            // There's no race here, because by the time we call this function Root threads should've stopped already,
            // which means no new Temporary threads are added (if Root threads stopped within 5 seconds).
            if (ThreadManager.temporaryThreads.Count == 0)
                return true;

            var tempThreadsStopped = ThreadManager.ThreadWaitHelper(
                ThreadManager.temporaryThreads,
                ThreadManager.temporaryThreadsLock,
                "Temporary",
                ThreadManager.SHUTDOWN_THREAD_ALIVE_WAIT_ITERATIONS,
                ThreadManager.SHUTDOWN_THREAD_ALIVE_CHECK_PERIOD_MILLIS,
                true /*isShutdown*/);

            Debug.Assert(tempThreadsStopped, "Shutdown - Wait for temporary threads stop failed, please investigate.");

            return tempThreadsStopped;
        }

        public static bool WaitForResourceThreadsShutdown()
        {
            // There's no race here as both Root threads and main window should be closed by now.
            if (ThreadManager.resourceThreads.Count == 0)
                return true;

            var resourceThreadsStopped = ThreadManager.ThreadWaitHelper(
                ThreadManager.resourceThreads,
                ThreadManager.resourceThreadsLock,
                "Resource",
                ThreadManager.SHUTDOWN_THREAD_ALIVE_WAIT_ITERATIONS,
                ThreadManager.SHUTDOWN_THREAD_ALIVE_CHECK_PERIOD_MILLIS,
                true /*isShutdown*/);

            Debug.Assert(resourceThreadsStopped, "Shutdown - Wait for Resource threads stop failed, please investigate.");

            return resourceThreadsStopped;
        }

        private static bool ThreadWaitHelper(
            IEnumerable<Thread> threadSet,
            object setLock,
            string threadSetName,
            int waitIterations,
            int waitMillis,
            bool isShutdown)
        {
            ThreadManager.Trace((isShutdown ? "Shutdown - " : "") + "Wating for " + threadSetName + " threads to stop...");
            for (int i = 0; i < waitIterations; ++i)
            {
                var allThreadsStopped = true;
                lock (setLock) // I might've used threadSet here, but I don't understand if locking on IEnumerable of collection is same as on collection itself.
                {
                    foreach (var t in threadSet)
                    {
                        if (t.IsAlive)
                        {
                            allThreadsStopped = false;
                            break;
                        }
                    }
                }

                if (allThreadsStopped)
                {
                    ThreadManager.Trace((isShutdown ? "Shutdown - " : "") + threadSetName + " threads stopped");
                    return true;
                }

                Thread.Sleep(waitMillis);
            }

            ThreadManager.Trace((isShutdown ? "Shutdown - " : "") + "Wait for " + threadSetName + " threads stop failed:");
            ThreadManager.TraceThreadSetStats(threadSet, setLock, threadSetName);

            return false;
        }

        private static void TraceThreadSetStats(IEnumerable<Thread> threadSet, object setLock, string setMsgPrefix)
        {
            // If we run into bad problems, we might also need to get stack trace out.
            lock (setLock)
            {
                foreach (var t in threadSet)
                    ThreadManager.Trace(string.Format("{0} thread Name: {1}  ThreadState: {2}  IsAlive: {3}", setMsgPrefix, t.Name, t.ThreadState, t.IsAlive));
            }
        }

        private static void Trace(string msg)
        {
            var msgPrefixed = string.Format("ThreadManager: {0}", msg);
            if (MainWindow.instance != null)  // Safe to take no lock here, because WriteLine is locking.
                Console.WriteLine(msgPrefixed);
            else
                Debug.WriteLine(msgPrefixed);
        }
    }
}
