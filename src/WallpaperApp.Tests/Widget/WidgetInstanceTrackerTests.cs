using WallpaperApp.Widget;
using Xunit;

namespace WallpaperApp.Tests.Widget
{
    public class WidgetInstanceTrackerTests
    {
        [Fact]
        public void AddWidget_ThenGetAll_ReturnsAllInstances()
        {
            var tracker = new WidgetInstanceTracker();
            tracker.AddOrUpdate("widget-1", WidgetSize.Small);
            tracker.AddOrUpdate("widget-2", WidgetSize.Medium);

            var all = tracker.GetAll();

            Assert.Equal(2, all.Count);
            Assert.Equal(WidgetSize.Small, all["widget-1"]);
            Assert.Equal(WidgetSize.Medium, all["widget-2"]);
        }

        [Fact]
        public void RemoveWidget_ThenGetAll_ExcludesRemovedInstance()
        {
            var tracker = new WidgetInstanceTracker();
            tracker.AddOrUpdate("widget-1", WidgetSize.Small);
            tracker.AddOrUpdate("widget-2", WidgetSize.Large);

            tracker.Remove("widget-1");

            var all = tracker.GetAll();
            Assert.Single(all);
            Assert.False(all.ContainsKey("widget-1"));
            Assert.True(all.ContainsKey("widget-2"));
        }

        [Fact]
        public async Task IsThreadSafe_ConcurrentAddRemove_DoesNotThrow()
        {
            var tracker = new WidgetInstanceTracker();
            var addTasks = Enumerable.Range(0, 10)
                .Select(i => Task.Run(() => tracker.AddOrUpdate($"widget-{i}", WidgetSize.Medium)));
            var removeTasks = Enumerable.Range(0, 10)
                .Select(i => Task.Run(() => tracker.Remove($"widget-{i}")));

            var exception = await Record.ExceptionAsync(
                () => Task.WhenAll(addTasks.Concat(removeTasks)));

            Assert.Null(exception);
        }

        [Fact]
        public void AddOrUpdate_ExistingId_UpdatesSize()
        {
            var tracker = new WidgetInstanceTracker();
            tracker.AddOrUpdate("widget-1", WidgetSize.Small);
            tracker.AddOrUpdate("widget-1", WidgetSize.Large);

            var all = tracker.GetAll();
            Assert.Single(all);
            Assert.Equal(WidgetSize.Large, all["widget-1"]);
        }

        [Fact]
        public void Contains_ReturnsTrueForTrackedWidget()
        {
            var tracker = new WidgetInstanceTracker();
            tracker.AddOrUpdate("widget-1", WidgetSize.Medium);

            Assert.True(tracker.Contains("widget-1"));
            Assert.False(tracker.Contains("widget-99"));
        }

        [Fact]
        public void Remove_NonExistentId_DoesNotThrow()
        {
            var tracker = new WidgetInstanceTracker();
            var exception = Record.Exception(() => tracker.Remove("does-not-exist"));
            Assert.Null(exception);
        }

        [Fact]
        public void GetAll_ReturnsSnapshot_NotLiveReference()
        {
            var tracker = new WidgetInstanceTracker();
            tracker.AddOrUpdate("widget-1", WidgetSize.Small);

            var snapshot = tracker.GetAll();
            tracker.AddOrUpdate("widget-2", WidgetSize.Large);

            // The snapshot captured before widget-2 was added should not change
            Assert.Single(snapshot);
        }
    }
}
