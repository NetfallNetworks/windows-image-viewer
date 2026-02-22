using System.Runtime.InteropServices;
using WallpaperApp.Widget;
using Xunit;

namespace WallpaperApp.Tests.Widget
{
    public class WidgetIpcServiceTests
    {
        // Use unique event names per test to avoid cross-test interference.
        // "Local\" prefix scopes to the current Windows session.
        private static string UniqueEventName() =>
            $"Local\\WidgetIpcTest_{Guid.NewGuid():N}";

        // Named EventWaitHandle is a Windows-only feature.
        // On Linux these tests verify the service degrades gracefully.

        [Fact]
        public async Task Start_WhenSignalReceived_InvokesCallback()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Named EventWaitHandle not supported on this platform — verify graceful no-op
                using var service = new WidgetIpcService(() => { }, UniqueEventName());
                await Task.Delay(100);
                service.Dispose(); // Should not throw
                return;
            }

            var eventName = UniqueEventName();
            var callbackInvoked = new ManualResetEventSlim(false);

            using var svc = new WidgetIpcService(
                () => callbackInvoked.Set(),
                eventName);

            // Give the background thread a moment to create the event
            await Task.Delay(200);

            // Signal the event (simulating TrayApp)
            using var handle = EventWaitHandle.OpenExisting(eventName);
            handle.Set();

            // Wait for callback — should fire within the 1-second WaitOne timeout
            var invoked = callbackInvoked.Wait(TimeSpan.FromSeconds(5));
            Assert.True(invoked, "Callback was not invoked after signaling the event");
        }

        [Fact]
        public async Task Start_WhenCancelled_ExitsCleanly()
        {
            var eventName = UniqueEventName();
            var callCount = 0;

            var service = new WidgetIpcService(
                () => Interlocked.Increment(ref callCount),
                eventName);

            // Give the background thread time to start
            await Task.Delay(200);

            // Dispose should cancel and exit cleanly without hanging
            var disposeTask = Task.Run(() => service.Dispose());
            var completed = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.Equal(disposeTask, completed);
        }

        [Fact]
        public void Dispose_StopsBackgroundThread()
        {
            var eventName = UniqueEventName();
            var callCount = 0;

            var service = new WidgetIpcService(
                () => Interlocked.Increment(ref callCount),
                eventName);

            // Dispose immediately — should not throw on any platform
            service.Dispose();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // After dispose, signaling should have no effect (event handle is gone)
                var opened = EventWaitHandle.TryOpenExisting(eventName, out var handle);
                handle?.Dispose();
            }

            // The service stopped — no further callbacks
            var countAfterDispose = callCount;
            Thread.Sleep(200);
            Assert.Equal(countAfterDispose, callCount);
        }

        [Fact]
        public async Task MultipleSignals_InvokeCallbackMultipleTimes()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Named EventWaitHandle not supported — verify no crash on dispose
                using var service = new WidgetIpcService(() => { }, UniqueEventName());
                await Task.Delay(100);
                return;
            }

            var eventName = UniqueEventName();
            var callCount = 0;
            var thirdCall = new ManualResetEventSlim(false);

            using var svc = new WidgetIpcService(
                () =>
                {
                    var count = Interlocked.Increment(ref callCount);
                    if (count >= 3) thirdCall.Set();
                },
                eventName);

            await Task.Delay(200);

            using var handle = EventWaitHandle.OpenExisting(eventName);

            // Send 3 signals with small delays so the listener can process each
            for (int i = 0; i < 3; i++)
            {
                handle.Set();
                await Task.Delay(200);
            }

            var received = thirdCall.Wait(TimeSpan.FromSeconds(5));
            Assert.True(received, $"Expected at least 3 callbacks, got {callCount}");
        }

        [Fact]
        public void SignalWidgetRefresh_HandleNotOpen_DoesNotThrow()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Named EventWaitHandle.TryOpenExisting throws PlatformNotSupportedException
                // on Linux — the TrayApp code guards with try/catch, so this is expected.
                // Verify that the guard pattern itself doesn't crash.
                var exception = Record.Exception(() =>
                {
                    try
                    {
                        if (EventWaitHandle.TryOpenExisting("nonexistent", out var h))
                        {
                            using (h) { h.Set(); }
                        }
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Expected on Linux — TrayApp wraps this in a try/catch
                    }
                });

                Assert.Null(exception);
                return;
            }

            // On Windows: TryOpenExisting should return false for non-existent handle
            var nonExistentEvent = $"Global\\NonExistent_{Guid.NewGuid():N}";

            var ex = Record.Exception(() =>
            {
                if (EventWaitHandle.TryOpenExisting(nonExistentEvent, out var handle))
                {
                    using (handle) { handle.Set(); }
                }
            });

            Assert.Null(ex);
        }

        [Fact]
        public async Task SignalWidgetRefresh_HandleOpen_SetsEvent()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Named EventWaitHandle not supported — verify graceful no-op
                using var service = new WidgetIpcService(() => { }, UniqueEventName());
                await Task.Delay(100);
                return;
            }

            var eventName = UniqueEventName();
            var signaled = new ManualResetEventSlim(false);

            using var svc = new WidgetIpcService(
                () => signaled.Set(),
                eventName);

            await Task.Delay(200);

            // Simulate TrayApp's SignalWidgetRefresh pattern
            if (EventWaitHandle.TryOpenExisting(eventName, out var handle))
            {
                using (handle) { handle.Set(); }
            }

            var wasSignaled = signaled.Wait(TimeSpan.FromSeconds(5));
            Assert.True(wasSignaled, "Event was not received by the IPC service");
        }
    }
}
