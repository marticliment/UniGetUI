using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using ModernWindow.Data;
using ModernWindow.Structures;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModernWindow
{
    public static class EntryPoint
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Having an async main method breaks WebView2
                CoreData.IsDaemon = args.Contains("--daemon");

                if (args.Contains("--uninstall-wingetui"))
                    UninstallPreps();
                else
                    _ = AsyncMain(args);
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        static async Task AsyncMain(string[] args)
        {
            try
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
                        DispatcherQueueSynchronizationContext context = new(
                            DispatcherQueue.GetForCurrentThread());
                        SynchronizationContext.SetSynchronizationContext(context);
                        new MainApp();
                    });
                }
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }
        private static async Task<bool> DecideRedirection()
        {
            try
            {

                bool isRedirect = false;
                AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
                ExtendedActivationKind kind = args.Kind;

                AppInstance keyInstance = AppInstance.FindOrRegisterForKey("MartiCliment.WingetUI.NeXT");

                if (keyInstance.IsCurrent)
                {
                    keyInstance.Activated += async (s, e) =>
                    {
                        MainApp AppInstance = MainApp.Current as MainApp;
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
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
                return false;
            }
        }
        private static void UninstallPreps()
        {
            //TODO: Make the uninstaller call WingetUI with the argument --uninstall-wingetui
            ToastNotificationManagerCompat.Uninstall();
        }

    }
}
