using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Media.Protection.PlayReady;
using static System.Net.Mime.MediaTypeNames;

namespace WingetUIWidgetProvider
{
    internal class WingetUIConnector
    {
        public event EventHandler<UpdatesCheckFinishedEventArgs> UpdateCheckFinished;
        public event EventHandler<ConnectionEventArgs> Connected;

        private string SessionToken;

        private bool was_connected = false;

        public WingetUIConnector() {
        }

        public void ResetConnection()
        {
            was_connected = false;
        }

        async public void Connect(CompactWidgetInfo widget)
        {
            ConnectionEventArgs args = new ConnectionEventArgs(widget);
            try
            {
                if (!was_connected)
                {
                    StreamReader reader = new StreamReader(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + "\\.wingetui\\CurrentSessionToken");
                    SessionToken = reader.ReadToEnd().ToString().Replace("\n", "").Trim();
                    reader.Close();
                    Console.WriteLine("Found token "+SessionToken);

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri("http://localhost:7058//");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage task = await client.GetAsync("/widgets/attempt_connection?token="+SessionToken);
                    if (task.IsSuccessStatusCode)
                        args.Succeeded = was_connected = true;
                    else
                        args.Succeeded = was_connected = false;
                }
                else
                {
                    args.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                args.Succeeded = was_connected = false;
            }
            Connected(this, args);
        }

        async public void GetAvailableUpdates(CompactWidgetInfo widget)
        {
            UpdatesCheckFinishedEventArgs args = new UpdatesCheckFinishedEventArgs(widget);
            try
            {
                string AllowedSource = "";

                switch (widget.widgetName)
                {
                    case (Widgets.Winget):
                        AllowedSource = "Winget";
                        break;

                    case (Widgets.Scoop):
                        AllowedSource = "Scoop";
                        break;

                    case (Widgets.Chocolatey):
                        AllowedSource = "Chocolatey";
                        break;

                    case (Widgets.Dotnet):
                        AllowedSource = ".NET Tool";
                        break;

                    case (Widgets.Npm):
                        AllowedSource = "Npm";
                        break;

                    case (Widgets.Pip):
                        AllowedSource = "Pip";
                        break;
                }

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage task = await client.GetAsync("/widgets/get_updates?token="+SessionToken);

                string outputString = await task.Content.ReadAsStringAsync();

                string purifiedString = outputString.Replace("\",\"status\":\"success\"}", "").Replace("{\"packages\":\"", "").Replace("\n", "").Trim();


                string[] packageStrings = purifiedString.Split("&&");
                int updateCount = packageStrings.Length;

                Package[] temp_updates = new Package[updateCount];

                int skippedPackages = 0;
                
                for(int i = 0; i < updateCount; i++)
                {
                    Package package = new Package(packageStrings[i]);
                    if (AllowedSource == "" || AllowedSource == package.ManagerName)
                        temp_updates[i - skippedPackages] = package;
                    else
                        skippedPackages++;
                }

                Package[] updates = new Package[updateCount - skippedPackages];
                for(int i = 0;i < updateCount - skippedPackages; i++)
                    updates[i] = temp_updates[i];

                args.Updates = updates;
                args.Count = updateCount;
                args.Succeeded = true;
            }
            catch (Exception ex)
            {
                args.Updates = new Package[0];
                args.Count = 0;
                args.Succeeded = false;
                Console.WriteLine(ex.ToString());
            }
            UpdateCheckFinished(this, args);

        }
        
        async public void OpenWingetUI()
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                await client.GetAsync("/widgets/open_wingetui?token=" + SessionToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        async public void ViewOnWingetUI()
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                await client.GetAsync("/widgets/view_on_wingetui?token=" + SessionToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        async public void UpdatePackage(Package package)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                await client.GetAsync("/widgets/update_package?token=" + SessionToken + "&id=" + package.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        async public void UpdateAllPackages()
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                await client.GetAsync("/widgets/update_all_packages?token=" + SessionToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public class Package
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string NewVersion{ get; set; }
        public string Source { get; set; }
        public string ManagerName { get; set; }
        public string Icon {  get; set; }
        public bool isValid = true;

        public Package(string packageString)
        {
            try
            {
                string[] packageParts = packageString.Split('|');
                Name = packageParts[0];
                Id = packageParts[1];
                Version = packageParts[2];
                NewVersion = packageParts[3];
                Source = packageParts[4];
                ManagerName = packageParts[5];
                if (packageParts[6] != "")
                    Icon = packageParts[6];
                else
                    Icon = "https://marticliment.com/resources/package_white.png";
            } catch
            {
                isValid = false;
                Name = "";
                Id = "";
                Version = "";
                NewVersion = "";
                Source = "";
                ManagerName = "";
                Icon = "https://marticliment.com/resources/package_white.png";
                Console.WriteLine("Can't construct package, given packageString=" + packageString);
            }
        }
    }

    public class UpdatesCheckFinishedEventArgs : EventArgs
    {
        public Package[] Updates { get; set; }
        public int Count { get; set; }
        public bool Succeeded { get; set; }
        public CompactWidgetInfo widget {  get; set; }

        public UpdatesCheckFinishedEventArgs(CompactWidgetInfo widget)
        {
            Updates = new Package[0];
            this.widget = widget;
        }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(CompactWidgetInfo widget)
        {
            this.widget = widget;
        }

        public bool Succeeded = true;
        public CompactWidgetInfo widget { get; set; }
    }
}
