using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace NotepadEx.Util
{
    public static class ProcessSync
    {
        // A unique name for our system-wide mutex. The "Global\" prefix makes it visible
        // across all user sessions on a machine. The GUID ensures it's unique.
        private const string MutexName = "Global\\{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}-NotepadEx";
        private static readonly Mutex SettingsMutex;

        static ProcessSync()
        {
            // In .NET 6+, Mutex constructor with a name is Windows-only. This is fine for WPF.
            // This code creates a new mutex or opens an existing one.
            SettingsMutex = new Mutex(false, MutexName);
        }

        /// <summary>
        /// Executes an action within a system-wide mutex lock to ensure thread and process safety.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void RunSynchronized(Action action)
        {
            try
            {
                // Wait for up to 5 seconds to acquire the mutex.
                // This prevents a process from getting stuck forever if another one crashes.
                if(SettingsMutex.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        // The action is executed only after we have the lock.
                        action();
                    }
                    finally
                    {
                        // CRITICAL: Always release the mutex, even if an exception occurs.
                        SettingsMutex.ReleaseMutex();
                    }
                }
                else
                {
                    // Could not get the mutex in time. For this app, we can probably
                    // just fail silently. The user might see default settings/theme on one instance.
                }
            }
            catch(AbandonedMutexException)
            {
                // This can happen if another process terminates without releasing the mutex.
                // We can still proceed, but it's good to be aware of.
                // For robustness, we could retry the action, but for now, we'll just execute it.
                try
                {
                    action();
                }
                finally
                {
                    SettingsMutex.ReleaseMutex();
                }
            }
        }
    }
}