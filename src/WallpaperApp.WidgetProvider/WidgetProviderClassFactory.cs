using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace WallpaperApp.WidgetProvider
{
    /// <summary>
    /// COM class factory for <see cref="WallpaperImageWidgetProvider"/>.
    /// Registered via CoRegisterClassObject so the Widget Board SCM can
    /// create instances of the provider in this process.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    internal sealed class WidgetProviderClassFactory : IClassFactory
    {
        private readonly WallpaperImageWidgetProvider _provider;

        // IUnknown GUID — used when the Widget Board queries for the base interface
        private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");

        public WidgetProviderClassFactory(WallpaperImageWidgetProvider provider)
        {
            _provider = provider;
        }

        public int CreateInstance(nint pUnkOuter, ref Guid riid, out nint ppvObject)
        {
            ppvObject = nint.Zero;

            // COM aggregation is not supported
            if (pUnkOuter != nint.Zero)
            {
                const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
                return CLASS_E_NOAGGREGATION;
            }

            // Return the single shared provider instance (singleton pattern).
            // IWidgetProvider is a WinRT (IInspectable) interface — must use
            // MarshalInspectable, not Marshal.GetComInterfaceForObject.
            if (riid == typeof(IWidgetProvider).GUID || riid == IID_IUnknown)
            {
                ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(_provider);
                return 0; // S_OK
            }

            const int E_NOINTERFACE = unchecked((int)0x80004002);
            return E_NOINTERFACE;
        }

        public int LockServer(bool fLock) => 0; // S_OK — lifetime managed by process
    }

    /// <summary>COM IClassFactory interface definition.</summary>
    [ComImport]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClassFactory
    {
        int CreateInstance(nint pUnkOuter, ref Guid riid, out nint ppvObject);
        int LockServer(bool fLock);
    }
}
