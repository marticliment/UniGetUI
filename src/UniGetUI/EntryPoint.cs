using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using UniGetUI.Core.Data;
using UniGetUI.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UniGetUI
{
    public static class EntryPoint
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Having an async main method breaks WebView2
            try
            {
                CoreData.IsDaemon = args.Contains("--daemon");

                if (args.Contains("--uninstall-unigetui"))
                    // If the app is being uninstalled, run the cleaner and exit
                    UninstallPreps();
                else if (args.Contains("--migrate-wingetui-to-unigetui"))
                    WingetUIToUniGetUIMigrator();
                else
                    // Otherwise, run UniGetUI as normal
                    _ = AsyncMain(args);
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }
        
        /// <summary>
        /// UniGetUI app main entry point
        /// </summary>
        /// <param name="args">Call arguments</param>
        /// <returns></returns>
        static async Task AsyncMain(string[] args)
        {
            try
            {

                AppTools.Log("Welcome to UniGetUI Version " + CoreData.VersionName);
                AppTools.Log("               Version Code " + CoreData.VersionNumber.ToString());
                AppTools.Log("              ");

                // WinRT single-instance fancy stuff
                WinRT.ComWrappersSupport.InitializeComWrappers();
                bool isRedirect = await DecideRedirection();

                // If this is the main instance, start the app
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

        /// <summary>
        /// Default WinUI Redirector
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> DecideRedirection()
        {
            try
            {
                // Idk how does this work, I copied it from the MS Docs
                // example on single-instance apps using unpackaged AppSdk + WinUI3
                bool isRedirect = false;
                AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
                ExtendedActivationKind kind = args.Kind;

                AppInstance keyInstance = AppInstance.FindOrRegisterForKey("MartiCliment.UniGetUI.MainInterface");

                if (keyInstance.IsCurrent)
                {
                    keyInstance.Activated += async (s, e) =>
                    {
                        MainApp AppInstance = MainApp.Current as MainApp;
                        await AppInstance.ShowMainWindowFromRedirectAsync();
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
                AppTools.Log(e);
                return false;
            }
        }
        
        /// <summary>
        /// This method should be called when the app is being uninstalled
        /// It removes system links and other stuff that should be removed on uninstall
        /// </summary>
        private static void UninstallPreps()
        {
            try
            {
                ToastNotificationManagerCompat.Uninstall();
            }
            catch
            { 
            }
        }

        // This method shall be ran as administrator
        static private void WingetUIToUniGetUIMigrator()
        {
            try
            {
                string[] BasePaths =
                {
                    // User desktop icon
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    
                    // User start menu icon
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    
                    // Common desktop icon
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                    
                    // User start menu icon
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                };

                foreach (var path in BasePaths)
                    foreach (var old_wingetui_icon in new string[] { "WingetUI.lnk", "WingetUI .lnk", "UniGetUI (formerly WingetUI) .lnk" })
                        try
                        {
                            var old_file = Path.Join(path, old_wingetui_icon);
                            var new_file = Path.Join(path, "UniGetUI (formerly WingetUI).lnk");
                            AppTools.Log(old_file);
                            if (!File.Exists(old_file))
                                continue;
                            else if (File.Exists(old_file) && File.Exists(new_file))
                            {
                                AppTools.Log("Deleting shortcut " + old_file + " since new shortcut already exists");
                                File.Delete(old_file);
                            }
                            else if (File.Exists(old_file) && !File.Exists(new_file))
                            {
                                AppTools.Log("Moving shortcut to " + new_file);
                                File.Move(old_file, new_file);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppTools.Log(ex);
                        }
            } 
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }
    }
}
