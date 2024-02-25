using ModernWindow.Data;
using ModernWindow.Structures;
using Nancy;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernWindow.Interface
{
    public class BackgroundApiRunner
    {

        private bool __running = false;
        public async Task Start()
        {
            try
            {
                __running = true;
                NancyHost host;
                try
                {
                    host = new NancyHost(new HostConfiguration() { RewriteLocalhost = false, }, new Uri("http://localhost:7058/"));
                    host.Start();
                }
                catch (Exception e)
                {
                    // Could not create host, most likely because the api URL has not been reserved
                    // Do the reservation process.
                    Process p = new();
                    p.StartInfo.FileName = CoreData.WingetUIExecutableFile;
                    p.StartInfo.Arguments = "--reserve-api-url";
                    p.StartInfo.Verb = "runas";
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    await p.WaitForExitAsync();

                    host = new NancyHost(new Uri("http://localhost:7058/"));
                    host.Start();
                }

                AppTools.Log("Api running on http://localhost:7058");
                
                while(__running)
                {
                    await Task.Delay(100);
                }
                host.Stop();
                AppTools.Log("Api was shut down");
            }
            catch (Exception e)
            {
                AppTools.Log(e);
            }
        }

        public void Stop()
        {
            __running = false;
        }
    }
    
    public class BackgroundApiHandler: NancyModule
    {
        public BackgroundApiHandler()
        {
            // Enabke CORS
            After.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                    .WithHeader("Access-Control-Allow-Methods", "POST,GET,OPTIONS")
                    .WithHeader("Access-Control-Allow-Headers", "Authorization");
            });


            Get("/v2/show-package", async (parameters) =>
            {
                try
                {
                    if (Request.Query.@pid == "" || Request.Query.@psource == "")
                        return 400;

                    AppTools.Log(Request.Query.@pid);

                    while (AppTools.Instance.App.mainWindow is null) await Task.Delay(100);
                    while (AppTools.Instance.App.mainWindow.NavigationPage is null) await Task.Delay(100);
                    while (AppTools.Instance.App.mainWindow.NavigationPage.DiscoverPage is null) await Task.Delay(100);

                    AppTools.Instance.App.mainWindow.NavigationPage.DiscoverPage.ShowSharedPackage_ThreadSafe(Request.Query.@pid.ToString(), Request.Query.@psource.ToString());

                    return "{\"status\": \"success\"}";
                }
                catch (Exception e)
                {
                    AppTools.Log(e);
                    return 500;
                }
            });

        }
    }
}
