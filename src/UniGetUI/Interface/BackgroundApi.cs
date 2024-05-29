using Nancy;
using Nancy.Hosting.Self;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface
{
    internal static class ApiTokenHolder
    {
        public static string Token = "";
    }

    public class BackgroundApiRunner
    {

        private bool __running = false;

        public static bool AuthenticateToken(string token)
        {
            return token == ApiTokenHolder.Token;
        }

        /// <summary>
        /// Run the background api and wait for it for being stopped with the Stop() method
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            try
            {

                if (Settings.Get("DisableWidgetsApi"))
                {
                    Logger.Warn("Widgets API is disabled");
                    return;
                }


                ApiTokenHolder.Token = CoreTools.RandomString(64);
                Settings.SetValue("CurrentSessionToken", ApiTokenHolder.Token);
                Logger.Info("Api auth token: " + ApiTokenHolder.Token);

                __running = true;
                NancyHost host;
                try
                {
                    host = new NancyHost(new HostConfiguration { RewriteLocalhost = false, }, new Uri("http://localhost:7058/"));
                    host.Start();
                }
                catch
                {
                    host = new NancyHost(new Uri("http://localhost:7058/"));
                    host.Start();
                }

                Logger.Info("Api running on http://localhost:7058");

                while (__running)
                {
                    await Task.Delay(100);
                }
                host.Stop();
                Logger.Info("Api was shut down");
            }
            catch (Exception e)
            {
                Logger.Error("An error occurred while initializing the API");
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Stop the background api
        /// </summary>
        public void Stop()
        {
            __running = false;
        }
    }

    /// <summary>
    /// The background api builder
    /// </summary>
    public class BackgroundApi : NancyModule
    {

        static Dictionary<string, string> PackageIconsPathReference = new();

        public BackgroundApi()
        {

            // Enable CORS
            After.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                    .WithHeader("Access-Control-Allow-Methods", "POST,GET,OPTIONS")
                    .WithHeader("Access-Control-Allow-Headers", "Authorization");
            });
            BuildShareApi();
            BuildV1WidgetsApi();
        }



        /// <summary>
        /// Build the endpoints required for the Share Interface
        /// </summary>
        public void BuildShareApi()
        {
            // Show package from https://marticliment.com/unigetui/share
            Get("/v2/show-package", async (parameters) =>
            {
                try
                {
                    if (Request.Query.@pid == "" || Request.Query.@psource == "")
                        return 400;

                    while (MainApp.Instance.MainWindow is null) await Task.Delay(100);
                    while (MainApp.Instance.MainWindow.NavigationPage is null) await Task.Delay(100);
                    while (MainApp.Instance.MainWindow.NavigationPage.DiscoverPage is null) await Task.Delay(100);

                    MainApp.Instance.MainWindow.NavigationPage.DiscoverPage.ShowSharedPackage_ThreadSafe(Request.Query.@pid.ToString(), Request.Query.@psource.ToString());

                    return "{\"status\": \"success\"}";
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    return 500;
                }
            });


            // Basic entrypoint to know if UniGetUI is running
            Get("/is-running", (parameters) =>
            {
                return "{\"status\": \"success\"}";
            });
        }

        /// <summary>
        /// Build the endpoints required for the /widgets/v1 endpoint. All of these 
        /// endpoints are authenticated with MainApp.Instance.AuthenticateToken
        /// </summary>
        public void BuildV1WidgetsApi()
        {
            // Basic version check
            Get("/widgets/v1/get_wingetui_version", (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                return CoreData.VersionNumber.ToString();
            });

            // Return found updates
            Get("/widgets/v1/get_updates", async (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                string packages = "";
                foreach (Package package in MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.Packages)
                {
                    if (package.Tag == PackageTag.OnQueue || package.Tag == PackageTag.BeingProcessed)
                        continue; // Do not show already processed packages on queue 

                    string icon;
                    string icon_path = (await package.GetIconUrl()).ToString();
                    if (icon_path == "ms-appx:///Assets/Images/package_color.png")
                    {
                        icon = "https://marticliment.com/resources/widgets/package_color.png";
                    }
                    else
                    {
                        PackageIconsPathReference[package.Id] = Path.Join(CoreData.UniGetUICacheDirectory_Icons, package.Manager.Name, $"{package.Id}.{icon_path.Split('.')[^1]}");
                        icon = $"http://localhost:7058/widgets/v2/get_icon_for_package?packageId={package.Id}&token={ApiTokenHolder.Token}";
                    }
                    packages += $"{package.Name.Replace('|', '-')}|{package.Id}|{package.Version}|{package.NewVersion}|{package.Source}|{package.Manager.Name}|{icon}&&";
                }

                if (packages.Length > 2)
                    packages = packages[..(packages.Length - 2)];

                return packages;
            });

            // Open UniGetUI (as it was)
            Get("/widgets/v1/open_wingetui", (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() => { MainApp.Instance.MainWindow.Activate(); });
                return 200;
            });

            // Open UniGetUI with the Updates page shown
            Get("/widgets/v1/view_on_wingetui", (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainApp.Instance.MainWindow.NavigationPage.UpdatesNavButton.ForceClick();
                    MainApp.Instance.MainWindow.Activate();
                });
                return 200;
            });

            // Update a specific package given its Id
            Get("/widgets/v1/update_package", (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                if (Request.Query.@id == "")
                    return 400;

                MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.UpdatePackageForId(Request.Query.@id);
                });
                return 200;
            });

            // Update all packages
            Get("/widgets/v1/update_all_packages", (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    Logger.Info("[WIDGETS] Updating all packages");
                    MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.UpdateAll();
                });
                return 200;
            });

            // Update all packages for a specific manager
            Get("/widgets/v1/update_all_packages_for_source", (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                if (Request.Query.@source == "")
                    return 400;

                MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() =>
                { 
                    Logger.Info($"[WIDGETS] Updating all packages for manager {Request.Query.@source}");
                    MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.UpdateAllPackagesForManager(Request.Query.@source);
                });
                return 200;
            });



            // Update all packages for a specific manager
            Get("/widgets/v2/get_icon_for_package", async (parameters) =>
            {
                if (!BackgroundApiRunner.AuthenticateToken(Request.Query.@token))
                    return 401;

                if (Request.Query.@packageId == "")
                    return 400;

                string path = "";
                if (PackageIconsPathReference.ContainsKey(Request.Query.@packageId) && File.Exists(PackageIconsPathReference[Request.Query.@packageId]))
                    path = PackageIconsPathReference[Request.Query.@packageId];
                else
                    path = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Images", "package_color.png");

                byte[] fileContents = await File.ReadAllBytesAsync(path);
                return new Response()
                {
                    ContentType = $"image/{path.Split('.')[^1]}",
                    Contents = (stream) =>
                    {
                        stream.Write(fileContents, 0, fileContents.Length);
                    }
                };

            });
        }
    }
}

