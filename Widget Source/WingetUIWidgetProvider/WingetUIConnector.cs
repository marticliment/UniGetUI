using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public WingetUIConnector() {

        }

        async public void Connect(CompactWidgetInfo widget)
        {
            ConnectionEventArgs args = new ConnectionEventArgs();
            args.widget = widget;
            try
            {
                StreamReader reader = new StreamReader(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + "\\.wingetui\\CurrentSessionToken");
                SessionToken = reader.ReadToEnd().ToString().Replace("\n", "").Trim();
                Console.WriteLine(SessionToken);
                reader.Close();

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage task = await client.GetAsync("/widgets/attempt_connection?token="+SessionToken);
                if (task.IsSuccessStatusCode)
                    args.Succeeded = true;
                else
                    args.Succeeded = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                args.Succeeded = false;
            }
            Connected(this, args);
        }

        async public void GetAvailableUpdates(CompactWidgetInfo widget)
        {
            UpdatesCheckFinishedEventArgs args = new UpdatesCheckFinishedEventArgs(widget);
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage task = await client.GetAsync("/widgets/get_updates?token="+SessionToken);

                string outputString = await task.Content.ReadAsStringAsync();

                string purifiedString = outputString.Replace("\",\"status\":\"success\"}", "").Replace("{\"packages\":\"", "");


                string[] packageStrings = purifiedString.Split("||");
                int updateCount = packageStrings.Length;

                Package[] updates = new Package[updateCount];
                
                for(int i = 0; i < updateCount; i++)
                {
                    updates[i] = new Package(packageStrings[i]);
                }

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
    }

    public class Package
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string NewVersion{ get; set; }
        public string Source { get; set; }
        public string ManagerName { get; set; }
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
            } catch
            {
                isValid = false;
                Name = "";
                Id = "";
                Version = "";
                NewVersion = "";
                Source = "";
                ManagerName = "";
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
        public bool Succeeded = true;
        public CompactWidgetInfo widget { get; set; }
    }
}
