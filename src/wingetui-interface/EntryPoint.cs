using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModernWindow
{
    public static class EntryPoint
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Having an async main method breaks WebView2
            _ = AsyncMain(args);
        }

        static async Task AsyncMain(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            bool isRedirect = await DecideRedirection();
            if (!isRedirect) // Sometimes, redirection fails, so we try again
                isRedirect = await DecideRedirection();
            if (!isRedirect) // Sometimes, redirection fails, so we try again (second time)
                isRedirect = await DecideRedirection();

            if (!isRedirect)
            {
                Microsoft.UI.Xaml.Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    new MainApp();
                });
            }
        }
        private static async Task<bool> DecideRedirection()
        {
            bool isRedirect = false;
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            ExtendedActivationKind kind = args.Kind;

            AppInstance keyInstance = AppInstance.FindOrRegisterForKey("MartiCliment.WingetUI.NeXT");

            if (keyInstance.IsCurrent)
            {
                keyInstance.Activated += async (s, e) =>
                {
                    var AppInstance = MainApp.Current as MainApp;
                    await AppInstance.ShowMainWindow_FromRedirect();
                };
            }
            else
            {
                isRedirect = true;
                await keyInstance.RedirectActivationToAsync(args);
            }
            return isRedirect;
        }

    }
}
