using System;
using System.IO;
using System.Threading;

namespace GameTranslator.Patches.Utils
// AutoTranslator Codes, under MIT license
{
    internal sealed class SafeFileWatcher : IDisposable
    {
        public event Action DirectoryUpdated;

        private FileSystemWatcher _watcher;
        private bool _disposed;
        private int _counter = 0;
        private object _sync = new object();
        private Timer _timer;
        private readonly string _directory;

        public SafeFileWatcher(string directory)
        {
            _directory = directory;
            _timer = new Timer(RaiseEvent, null, Timeout.Infinite, Timeout.Infinite);

            EnableWatcher();
        }

        public void EnableWatcher()
        {
            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher(_directory);
                _watcher.Changed += Watcher_Changed;
                _watcher.Created += Watcher_Created;
                _watcher.Deleted += Watcher_Deleted;
                _watcher.EnableRaisingEvents = true;
            }
        }

        // Still using for other purposes
        public void Disable()
        {
            var counter = Interlocked.Increment(ref _counter);
            UpdateRaisingEvents(counter == 0);
        }

        // Still using for other purposes
        public void Enable()
        {
            var counter = Interlocked.Decrement(ref _counter);
            UpdateRaisingEvents(counter == 0);
        }

        // Still using for other purposes
        public void DisableWatcher()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        // Still using for other purposes
        private void UpdateRaisingEvents(bool enabled)
        {
            lock (_sync)
            {
                if (enabled)
                {
                    EnableWatcher();
                }
                else
                {
                    DisableWatcher();
                }
            }
        }

        public void RaiseEvent(object state)
        {
            DirectoryUpdated?.Invoke();
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            _timer.Change(1000, Timeout.Infinite);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            var fi = new FileInfo(e.FullPath);
            WaitForFile(fi);

            _timer.Change(1000, Timeout.Infinite);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            _timer.Change(1000, Timeout.Infinite);
        }

        private void WaitForFile(FileInfo file)
        {
            while (IsFileLocked(file))
            {
                Thread.Sleep(100);      // If the file always locked, just infinite loop here
            }
        }

        private bool IsFileLocked(FileInfo file)
        {
            try
            {
                using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _watcher?.Dispose();
                    _watcher = null;
                    _timer.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}