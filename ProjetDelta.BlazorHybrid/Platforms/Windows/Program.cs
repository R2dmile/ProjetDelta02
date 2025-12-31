using System;
using Microsoft.UI.Dispatching;
using WinUIApplication = Microsoft.UI.Xaml.Application;

namespace ProjetDelta.BlazorHybrid.WinUI;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Init COM wrappers avant toute creation de WebView2
        WinRT.ComWrappersSupport.InitializeComWrappers();
        WinUIApplication.Start(p =>
        {
            var syncContext = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(syncContext);
            new App();
        });
    }
}
