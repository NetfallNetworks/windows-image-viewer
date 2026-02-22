namespace WallpaperApp.Widget
{
    /// <summary>
    /// Thread-safe tracker for active widget instances and their current sizes.
    /// Keyed by widget ID string as provided by the Widget Board host.
    /// </summary>
    public class WidgetInstanceTracker
    {
        private readonly Dictionary<string, WidgetSize> _instances = new();
        private readonly object _lock = new();

        /// <summary>Adds or updates a widget instance with its current display size.</summary>
        public void AddOrUpdate(string widgetId, WidgetSize size)
        {
            lock (_lock)
            {
                _instances[widgetId] = size;
            }
        }

        /// <summary>Removes a widget instance when the user unpins it.</summary>
        public void Remove(string widgetId)
        {
            lock (_lock)
            {
                _instances.Remove(widgetId);
            }
        }

        /// <summary>Returns a snapshot of all active widget instances.</summary>
        public IReadOnlyDictionary<string, WidgetSize> GetAll()
        {
            lock (_lock)
            {
                return new Dictionary<string, WidgetSize>(_instances);
            }
        }

        /// <summary>Returns true if the widget ID is currently tracked.</summary>
        public bool Contains(string widgetId)
        {
            lock (_lock)
            {
                return _instances.ContainsKey(widgetId);
            }
        }
    }
}
