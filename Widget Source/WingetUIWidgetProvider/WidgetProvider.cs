using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Chat;
using Windows.Management.Deployment;
using Windows.Storage.Pickers;

namespace WingetUIWidgetProvider
{

    internal class WidgetProvider : IWidgetProvider
    {
        public static Dictionary<string, CompactWidgetInfo> RunningWidgets = new Dictionary<string, CompactWidgetInfo>();

        WingetUIConnector wingetui;

        public WidgetProvider()
        {
            wingetui = new WingetUIConnector();
            wingetui.UpdateCheckFinished += Wingetui_UpdateCheckFinished;
            wingetui.Connected += Wingetui_Connected;

            var runningWidgets = WidgetManager.GetDefault().GetWidgetInfos();

            foreach (var widgetInfo in runningWidgets)
            {
                var widgetContext = widgetInfo.WidgetContext;
                var widgetId = widgetContext.Id;
                var widgetName = widgetContext.DefinitionId;
                if (!RunningWidgets.ContainsKey(widgetId))
                {
                    CompactWidgetInfo runningWidgetInfo = new CompactWidgetInfo() { widgetId = widgetId, widgetName = widgetName };
                    try
                    {
                        // If we had any save state (in this case we might have some state saved for Counting widget)
                        // convert string to required type if needed.
                        //int count = Convert.ToInt32(customState.ToString());
                        //runningWidgetInfo.customState = count;
                        runningWidgetInfo.isActive = true;
                        runningWidgetInfo.size = widgetInfo.WidgetContext.Size;
                        runningWidgetInfo.customState = 0;
    }
                    catch
                    {
                        Console.WriteLine("Failed to import old widget!");
                    }
                    RunningWidgets[widgetId] = runningWidgetInfo;
                }
            }
        }

        private void StartLoadingRoutine(CompactWidgetInfo widget)
        {
            WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(widget.widgetId);
            updateOptions.Data = "{ \"IsLoading\": true }";
            Console.WriteLine("Starting load routine...");
            updateOptions.Template = Templates.BaseTemplate;
            wingetui.Connect(widget);
            WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }

        private void Wingetui_Connected(object? sender, ConnectionEventArgs e)
        {
            WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(e.widget.widgetId);
            if (!e.Succeeded)
            {
                updateOptions.Data = Templates.GetData_NoWingetUI();
                Console.WriteLine("Could not connect to WingetUI");
            }
            else
            {
                updateOptions.Data = Templates.GetData_IsLoading();
                Console.WriteLine("Connected to WingetUI");
                wingetui.GetAvailableUpdates(e.widget);
            }

            updateOptions.Template = Templates.BaseTemplate;
            WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }

        private void Wingetui_UpdateCheckFinished(object? sender, UpdatesCheckFinishedEventArgs e)
        {
            WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(e.widget.widgetId);

            updateOptions.Template = Templates.BaseTemplate;
            if (!e.Succeeded)
            {
                updateOptions.Data = Templates.GetData_ErrorOccurred("UPDATE_CHECK_FAILED");
                Console.WriteLine("Could not check for updates");
            }
            else if (e.Count == 0)
            {
                updateOptions.Data = Templates.GetData_NoUpdatesFound();
                Console.WriteLine("No updates were found");
            }
            else
            {
                Console.WriteLine("Showing available updates...");
                updateOptions.Template = Templates.UpdatesTemplate;
                string packages = "";
                string[,] upgradablePackages = new string[e.Count, 3];
                for (int i = 0; i < e.Count; i++)
                {
                    upgradablePackages[i,0] = e.Updates[i].Name;
                    upgradablePackages[i, 1] = e.Updates[i].Version;
                    upgradablePackages[i,2] = e.Updates[i].NewVersion;
                    if (e.widget.size == WidgetSize.Medium && i == 3 && e.Count > 3)
                    {
                        i++;
                        packages += (e.Count - i).ToString() + " more packages can also be upgraded";
                        i = e.Count;
                    }
                    else if (e.widget.size == WidgetSize.Large && i == 7 && e.Count > 7)
                    {
                        i++;
                        packages += (e.Count - i).ToString() + " more packages can also be upgraded";
                        i = e.Count;
                    }
                }
                Console.WriteLine(updateOptions.Template);
                Console.WriteLine(updateOptions.Data);
                updateOptions.Data = Templates.GetData_UpdatesList(e.Count, upgradablePackages);
            }
            Console.WriteLine(updateOptions.Data);
            WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }

        public void CreateWidget(WidgetContext widgetContext)
        {
            var widgetId = widgetContext.Id; // To save RPC calls
            var widgetName = widgetContext.DefinitionId;
            CompactWidgetInfo runningWidgetInfo = new CompactWidgetInfo() { widgetId = widgetId, widgetName = widgetName };
            RunningWidgets[widgetId] = runningWidgetInfo;
            StartLoadingRoutine(runningWidgetInfo);
        }

        public void DeleteWidget(string widgetId, string customState)
        {
            RunningWidgets.Remove(widgetId);

            if (RunningWidgets.Count == 0)
            {
                emptyWidgetListEvent.Set();
            }
        }

        static ManualResetEvent emptyWidgetListEvent = new ManualResetEvent(false);

        public static ManualResetEvent GetEmptyWidgetListEvent()
        {
            return emptyWidgetListEvent;
        }

        public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
        {
            var widgetId = actionInvokedArgs.WidgetContext.Id;
            var data = actionInvokedArgs.Data;
            WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(widgetId);
            if (RunningWidgets.ContainsKey(widgetId))
            {
                var localWidgetInfo = RunningWidgets[widgetId];
                var verb = actionInvokedArgs.Verb;

                switch (verb)
                {
                    case (Verbs.Reload):
                        localWidgetInfo.customState = 0;
                        StartLoadingRoutine(localWidgetInfo);
                        break;

                    case (Verbs.ViewUpdatesOnWingetUI):
                        break;

                    case (Verbs.OpenWingetUI):
                        break;

                    case (Verbs.UpdateAll):
                        localWidgetInfo.customState = 1;
                        updateOptions.Data = Templates.GetData_UpdatesInCourse();
                        updateOptions.Template = Templates.BaseTemplate;
                        WidgetManager.GetDefault().UpdateWidget(updateOptions);
                        break;


                }
            }
            
        }

        public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
        {
            var widgetContext = contextChangedArgs.WidgetContext;
            var widgetId = widgetContext.Id;
            var widgetSize = widgetContext.Size;
            if (RunningWidgets.ContainsKey(widgetId))
            {
                var localWidgetInfo = RunningWidgets[widgetId];
                localWidgetInfo.size = widgetContext.Size;
                StartLoadingRoutine(localWidgetInfo);

            }
        }

        public void Activate(WidgetContext widgetContext)
        {
            var widgetId = widgetContext.Id;

            if (RunningWidgets.ContainsKey(widgetId))
            {
                var localWidgetInfo = RunningWidgets[widgetId];
                localWidgetInfo.isActive = true;
                localWidgetInfo.size = widgetContext.Size;
                StartLoadingRoutine(localWidgetInfo);
            }
        }
        public void Deactivate(string widgetId)
        {
            if (RunningWidgets.ContainsKey(widgetId))
            {
                var localWidgetInfo = RunningWidgets[widgetId];
                localWidgetInfo.isActive = false;
            }
        }
    }

    public class CompactWidgetInfo
    {
        public string widgetId { get; set; }
        public string widgetName { get; set; }
        public WidgetSize size { get; set; }
        public int customState = 0;
        public bool isActive = false;

    }

}
