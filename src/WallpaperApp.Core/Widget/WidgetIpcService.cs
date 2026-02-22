namespace WallpaperApp.Widget
{
    /// <summary>
    /// Listens for IPC signals from the TrayApp via a named <see cref="EventWaitHandle"/>.
    /// When signaled, invokes the provided callback so the widget provider can push
    /// updated cards to all active widget instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service owns the EventWaitHandle lifetime and runs a background thread
    /// that blocks on <see cref="EventWaitHandle.WaitOne(TimeSpan)"/> in a loop.
    /// Disposing the service cancels the background thread and releases the handle.
    /// </para>
    /// <para>
    /// Named EventWaitHandle is a Windows-only feature. On non-Windows platforms,
    /// the service starts but immediately exits the listener loop (no-op).
    /// </para>
    /// </remarks>
    public sealed class WidgetIpcService : IDisposable
    {
        private readonly Action _onSignaled;
        private readonly string _eventName;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _listenerTask;
        private bool _disposed;

        /// <summary>
        /// Creates and starts the IPC listener.
        /// </summary>
        /// <param name="onSignaled">
        /// Callback invoked on the background thread when the TrayApp signals a wallpaper update.
        /// Must be thread-safe.
        /// </param>
        /// <param name="eventName">
        /// Named event handle name. Defaults to <see cref="WidgetIpcConstants.WidgetRefreshEventName"/>.
        /// </param>
        public WidgetIpcService(Action onSignaled, string? eventName = null)
        {
            _onSignaled = onSignaled ?? throw new ArgumentNullException(nameof(onSignaled));
            _eventName = eventName ?? WidgetIpcConstants.WidgetRefreshEventName;
            _listenerTask = Task.Run(RunListenerLoop);
        }

        private void RunListenerLoop()
        {
            EventWaitHandle ipcEvent;
            try
            {
                ipcEvent = new EventWaitHandle(
                    initialState: false,
                    mode: EventResetMode.AutoReset,
                    name: _eventName,
                    createdNew: out _);
            }
            catch (PlatformNotSupportedException)
            {
                // Named EventWaitHandle is not supported on this platform (Linux/macOS).
                // Widget IPC is a Windows-only feature — exit silently.
                return;
            }

            using (ipcEvent)
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        // WaitOne with a 1-second timeout so we can check cancellation periodically
                        if (ipcEvent.WaitOne(TimeSpan.FromSeconds(1)))
                        {
                            _onSignaled();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // Transient error — wait briefly before retrying to avoid tight spin
                        if (!_cts.IsCancellationRequested)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops the background listener thread and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();

            try
            {
                // Wait for the listener thread to exit (bounded to avoid hangs at shutdown)
                _listenerTask.Wait(TimeSpan.FromSeconds(3));
            }
            catch (AggregateException)
            {
                // Listener task may have faulted — swallow at dispose time
            }

            _cts.Dispose();
        }
    }
}
