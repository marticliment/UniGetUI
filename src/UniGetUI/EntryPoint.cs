﻿using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;

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

                if (args.Contains("--uninstall-unigetui") || args.Contains("--uninstall-wingetui"))
                {
                    // If the app is being uninstalled, run the cleaner and exit
                    UninstallPreps();
                }
                else if (args.Contains("--migrate-wingetui-to-unigetui"))
                {
                    WingetUIToUniGetUIMigrator();
                }
                else
                {
                    // Otherwise, run UniGetUI as normal
                    _ = AsyncMain(args);
                }
            }
            catch (Exception e)
            {
                CoreTools.ReportFatalException(e);
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
                string textart = $"""
                     __  __      _ ______     __  __  ______
                    / / / /___  (_) ____/__  / /_/ / / /  _/
                   / / / / __ \/ / / __/ _ \/ __/ / / // /  
                  / /_/ / / / / / /_/ /  __/ /_/ /_/ // /   
                  \____/_/ /_/_/\____/\___/\__/\____/___/   
                      Welcome to UniGetUI Version {CoreData.VersionName}
                  """;

                Logger.ImportantInfo(textart);
                Logger.ImportantInfo("  ");
                Logger.ImportantInfo($"Version Code:  {CoreData.VersionNumber}");
                Logger.ImportantInfo($"Encoding Code Page set to {CoreData.CODE_PAGE}");

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
                CoreTools.ReportFatalException(e);
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
                        MainApp? AppInstance = MainApp.Current as MainApp;
                        if (AppInstance != null)
                        {
                            await AppInstance.ShowMainWindowFromRedirectAsync();
                        }
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
                Logger.Warn(e);
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

                foreach (string path in BasePaths)
                {
                    foreach (string old_wingetui_icon in new []{ "WingetUI.lnk", "WingetUI .lnk", "UniGetUI (formerly WingetUI) .lnk" })
                    {
                        try
                        {
                            string old_file = Path.Join(path, old_wingetui_icon);
                            string new_file = Path.Join(path, "UniGetUI (formerly WingetUI).lnk");
                            if (!File.Exists(old_file))
                            {
                                continue;
                            }
                            else if (File.Exists(old_file) && File.Exists(new_file))
                            {
                                Logger.Info("Deleting shortcut " + old_file + " since new shortcut already exists");
                                File.Delete(old_file);
                            }
                            else if (File.Exists(old_file) && !File.Exists(new_file))
                            {
                                Logger.Info("Moving shortcut to " + new_file);
                                File.Move(old_file, new_file);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"An error occurred while migrating the shortcut {Path.Join(path, old_wingetui_icon)}");
                            Logger.Warn(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
