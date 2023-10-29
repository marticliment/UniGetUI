using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media.Protection.PlayReady;
using static System.Net.Mime.MediaTypeNames;

namespace WingetUIWidgetProvider
{
    internal class WingetUIConnector
    {
        public event EventHandler<UpdatesCheckFinishedEventArgs> UpdateCheckFinished;
        public event EventHandler<ConnectionEventArgs> Connected;

        public WingetUIConnector() {
        }

        async public void Connect(CompactWidgetInfo widget)
        {
            ConnectionEventArgs args = new ConnectionEventArgs();
            args.widget = widget;
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage task = await client.GetAsync("/is-running");
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
            UpdatesCheckFinishedEventArgs args = new UpdatesCheckFinishedEventArgs();
            args.widget = widget;
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage task = await client.GetAsync("/get-updates-stringlist");

                string outputString = await task.Content.ReadAsStringAsync();

                string purifiedString = outputString.Replace("\",\"status\":\"success\"}", "").Replace("{\"packages\":\"", "");

                args.Updates = purifiedString.Split("#~#");
                args.Count = args.Updates.Length;
                args.Succeeded = true;
            }
            catch (Exception ex)
            {
                args.Updates = new string[0];
                args.Count = 0;
                args.Succeeded = false;
                Console.WriteLine(ex.ToString());
            }
            UpdateCheckFinished(this, args);

        }        
    }

    public class UpdatesCheckFinishedEventArgs : EventArgs
    {
        public string[] Updates { get; set; }
        public int Count { get; set; }
        public bool Succeeded { get; set; }
        public CompactWidgetInfo widget {  get; set; }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public bool Succeeded = true;
        public CompactWidgetInfo widget { get; set; }
    }
}
