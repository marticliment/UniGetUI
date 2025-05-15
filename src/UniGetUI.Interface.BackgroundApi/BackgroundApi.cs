using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.Interface
{
    internal static class ApiTokenHolder
    {
        public static string Token = "";
    }

    public class BackgroundApiRunner
    {
        public event EventHandler<EventArgs>? OnOpenWindow;
        public event EventHandler<EventArgs>? OnOpenUpdatesPage;
        public event EventHandler<KeyValuePair<string, string>>? OnShowSharedPackage;
        public event EventHandler<EventArgs>? OnUpgradeAll;
        public event EventHandler<string>? OnUpgradeAllForManager;
        public event EventHandler<string>? OnUpgradePackage;

        private IHost? _host;
        private bool _running;

        public BackgroundApiRunner()
        {
        }

        public static bool AuthenticateToken(string? token)
        {
            return token == ApiTokenHolder.Token;
        }

        public async Task Start()
        {

            if (Settings.Get("DisableWidgetsApi"))
            {
                Logger.Warn("Widgets API is disabled");
                return;
            }

            ApiTokenHolder.Token = CoreTools.RandomString(64);
            Settings.SetValue("CurrentSessionToken", ApiTokenHolder.Token);
            Logger.Info("Randomly-generated background API auth token: " + ApiTokenHolder.Token);

            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services => services.AddCors());
            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel();
#if !DEBUG
                webBuilder.SuppressStatusMessages(true);
#endif
                webBuilder.Configure(app =>
                    {
                        app.UseCors(policy => policy
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                        );

                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            // Share endpoints
                            endpoints.MapGet("/v2/show-package", V2_ShowPackage);
                            endpoints.MapGet("/is-running", API_IsRunning);
                            // Widgets v1 API
                            endpoints.MapGet("/widgets/v1/get_wingetui_version", WIDGETS_V1_GetUniGetUIVersion);
                            endpoints.MapGet("/widgets/v1/get_updates", WIDGETS_V1_GetUpdates);
                            endpoints.MapGet("/widgets/v1/open_wingetui", WIDGETS_V1_OpenUniGetUI);
                            endpoints.MapGet("/widgets/v1/view_on_wingetui", WIDGETS_V1_ViewOnUniGetUI);
                            endpoints.MapGet("/widgets/v1/update_package", WIDGETS_V1_UpdatePackage);
                            endpoints.MapGet("/widgets/v1/update_all_packages", WIDGETS_V1_UpdateAllPackages);
                            endpoints.MapGet("/widgets/v1/update_all_packages_for_source",
                                WIDGETS_V1_UpdateAllPackagesForSource);
                            // Widgets v2 API
                            endpoints.MapGet("/widgets/v2/get_icon_for_package", WIDGETS_V2_GetIconForPackage);
                        });
                    });
                webBuilder.UseUrls("http://localhost:7058");
            });
            _host = builder.Build();
            _running = true;
            await _host.StartAsync();
            Logger.Info("Api running on http://localhost:7058");
        }

    private async Task V2_ShowPackage(HttpContext context)
        {
            var query = context.Request.Query;
            if (string.IsNullOrEmpty(query["pid"]) || string.IsNullOrEmpty(query["psource"]))
            {
                context.Response.StatusCode = 400;
                return;
            }

            OnShowSharedPackage?.Invoke(null, new KeyValuePair<string, string>(query["pid"], query["psource"]));

            await context.Response.WriteAsync("{\"status\": \"success\"}");
        }

        private async Task API_IsRunning(HttpContext context)
        {
            await context.Response.WriteAsync("{\"status\": \"success\"}");
        }

        private async Task WIDGETS_V1_GetUniGetUIVersion(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            await context.Response.WriteAsync(CoreData.BuildNumber.ToString());
        }

        private async Task WIDGETS_V1_GetUpdates(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            if (!PEInterface.UpgradablePackagesLoader.IsLoaded && !PEInterface.UpgradablePackagesLoader.IsLoading)
            {
                _ = PEInterface.UpgradablePackagesLoader.ReloadPackages();
            }

            while (PEInterface.UpgradablePackagesLoader.IsLoading)
            {
                await Task.Delay(100);
            }

            StringBuilder packages = new();
            foreach (IPackage package in PEInterface.UpgradablePackagesLoader.Packages)
            {
                if (package.Tag is PackageTag.OnQueue or PackageTag.BeingProcessed) continue;

                string icon = $"http://localhost:7058/widgets/v2/get_icon_for_package?packageId={Uri.EscapeDataString(package.Id)}&packageSource={Uri.EscapeDataString(package.Source.Name)}&token={ApiTokenHolder.Token}";
                packages.Append($"{package.Name.Replace('|', '-')}" + $"|{package.Id}" + $"|{package.VersionString}" + $"|{package.NewVersionString}" + $"|{package.Source.AsString_DisplayName}" + $"|{package.Manager.Name}" + $"|{icon}&&");
            }

            string result = packages.ToString();
            if (result.Length > 2) result = result[..(result.Length - 2)];

            await context.Response.WriteAsync(result);
        }

        private async Task WIDGETS_V1_OpenUniGetUI(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            OnOpenWindow?.Invoke(null, EventArgs.Empty);
            context.Response.StatusCode = 200;
        }

        private async Task WIDGETS_V1_ViewOnUniGetUI(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            OnOpenUpdatesPage?.Invoke(null, EventArgs.Empty);
            context.Response.StatusCode = 200;
        }

        private async Task WIDGETS_V1_UpdatePackage(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var id = context.Request.Query["id"];
            if (string.IsNullOrEmpty(id))
            {
                context.Response.StatusCode = 400;
                return;
            }

            OnUpgradePackage?.Invoke(null, id);
            context.Response.StatusCode = 200;
        }

        private async Task WIDGETS_V1_UpdateAllPackages(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            Logger.Info("[WIDGETS] Updating all packages");
            OnUpgradeAll?.Invoke(null, EventArgs.Empty);
            context.Response.StatusCode = 200;
        }

        private async Task WIDGETS_V1_UpdateAllPackagesForSource(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var source = context.Request.Query["source"];
            if (string.IsNullOrEmpty(source))
            {
                context.Response.StatusCode = 400;
                return;
            }

            Logger.Info($"[WIDGETS] Updating all packages for manager {source}");
            OnUpgradeAllForManager?.Invoke(null, source);
            context.Response.StatusCode = 200;
        }

        private async Task WIDGETS_V2_GetIconForPackage(HttpContext context)
        {
            if (!AuthenticateToken(context.Request.Query["token"]))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var packageId = context.Request.Query["packageId"];
            var packageSource = context.Request.Query["packageSource"];
            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(packageSource))
            {
                context.Response.StatusCode = 400;
                return;
            }

            string iconPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Images", "package_color.png");

            IPackage? package = PEInterface.UpgradablePackagesLoader.GetPackageForId(packageId, packageSource);
            if (package != null)
            {
                var iconUrl = await Task.Run(package.GetIconUrl);
                if (iconUrl.ToString() != "ms-appx:///Assets/Images/package_color.png")
                {
                    string mimePath = Path.Join(CoreData.UniGetUICacheDirectory_Icons, package.Manager.Name, package.Id, "icon.mime");
                    iconPath = Path.Join(CoreData.UniGetUICacheDirectory_Icons, package.Manager.Name, package.Id, $"icon.{IconCacheEngine.MimeToExtension[await File.ReadAllTextAsync(mimePath)]}");
                }
            }
            else
            {
                Logger.Warn($"[API] Package id={packageId} with source={packageSource} not found!");
            }

            var bytes = await File.ReadAllBytesAsync(iconPath);
            var ext = Path.GetExtension(iconPath).TrimStart('.').ToLower();
            context.Response.ContentType = IconCacheEngine.ExtensionToMime.GetValueOrDefault(ext, "image/png");
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        public async void Stop()
        {
            await _host.StopAsync();
            Logger.Info("Api was shut down");
        }
    }
}
