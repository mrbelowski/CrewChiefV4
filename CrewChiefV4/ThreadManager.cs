/*
 * This class' responsibility is to provide some corrdination between starting/stopping threads in CC.  Here's proposal for strategy of
 * dealing with threads in CC, aka "Architecture":
 *  TODO_THREADS: update for temp and resource threads
 * 1. each root thread (the one we start from MainWindow/UI thread) has to be registered with ThreadManager and named
 * 
 * 1.1: FUTURE: each temporary Thread should also be registered
 * 
 * 2. if any of the root thread creates a long lived thread, it has to wait for it to exit when root thread exits.
 * 
 * 3. each thread should release/dispose it's resources on exit, unless it is too complicated/unpractical (See GlobalResources class)
 * 
 * 4. global shared resources will be released after form close (and all root threads stopped, if they stop within agreed time, otherwise - undefined behavior).
 * 
 * 5. access to main window should be synchronized with lock.  Be extra careful, if you are marshalling to the main thread, do a Post, not Send, so that lock is not held
 *    Failing to follow above might cause deadlocks.
 * 
 * 6. For Sleeps consider using Utilities.InterruptedSleep to avoid long shutdown delays.
 * 
 * 7. Work worker threads that pump some data, don't just use Sleep, use Events to wake them up.
 * 
 * Future: unsolved problems
 *  - Download threasds
 *  - File dump in main run thread
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

        public static void UnregisterTemporaryThread(Thread t)
        {
            if (t == null)
                return;

            lock (ThreadManager.temporaryThreadsLock)
            {
                if (ThreadManager.temporaryThreads.Contains(t))
                {
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
        // Upon exit, enables Start/Stop button.
        //
        public static bool WaitForRootAndTemporaryThreadsStop(CrewChief cc)
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
