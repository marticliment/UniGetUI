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
using Windows.Security.Cryptography.Core;
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
                    CompactWidgetInfo runningWidgetInfo = new CompactWidgetInfo(widgetId, widgetName);
                    try
                    {
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
                WidgetManager.GetDefault().UpdateWidget(updateOptions);
            }
            else if (e.Count == 0)
            {
                updateOptions.Data = Templates.GetData_NoUpdatesFound();
                Console.WriteLine("No updates were found");
                WidgetManager.GetDefault().UpdateWidget(updateOptions);
            }
            else
            {
                e.widget.AvailableUpdates = e.Updates;
                DrawUpdates(e.widget);
            }
        }

        private void DrawUpdates(CompactWidgetInfo widget)
        { 
            WidgetUpdateRequestOptions updateOptions = new WidgetUpdateRequestOptions(widget.widgetId);
            
            Console.WriteLine("Showing available updates...");
            updateOptions.Template = Templates.UpdatesTemplate;
            string packages = "";
            Package[] upgradablePackages = new Package[widget.AvailableUpdates.Length];
            int nullPackages = 0;
            for (int i = 0; i < widget.AvailableUpdates.Length; i++)
            {
                if (widget.AvailableUpdates[i].Name == "")
                {
                    nullPackages += 1;
                }
                else
                {
                    upgradablePackages[i] = widget.AvailableUpdates[i];
                    if (widget.size == WidgetSize.Medium && i == (3 + nullPackages) && widget.AvailableUpdates.Length > (3 + nullPackages))
                    {
                        i++;
                        packages += (widget.AvailableUpdates.Length - i).ToString() + " more packages can also be upgraded";
                        i = widget.AvailableUpdates.Length;
                    }
                    else if (widget.size == WidgetSize.Large && i == (7 + nullPackages) && widget.AvailableUpdates.Length > (7 + nullPackages) && widget.AvailableUpdates.Length > 7)
                    {
                        i++;
                        packages += (widget.AvailableUpdates.Length - i).ToString() + " more packages can also be upgraded";
                        i = widget.AvailableUpdates.Length;
                    }
                }
            }

            Console.WriteLine(widget.AvailableUpdates.Length);
            Console.WriteLine(nullPackages);

            if ((widget.AvailableUpdates.Length - nullPackages) == 0)
            {
                updateOptions.Template = Templates.BaseTemplate;
                updateOptions.Data = Templates.GetData_NoUpdatesFound();
            } else {
                Console.WriteLine(updateOptions.Template);
                Console.WriteLine(updateOptions.Data);
                updateOptions.Data = Templates.GetData_UpdatesList(widget.AvailableUpdates.Length, upgradablePackages);
            }

            Console.WriteLine(updateOptions.Data);
            WidgetManager.GetDefault().UpdateWidget(updateOptions);
        }

        public void CreateWidget(WidgetContext widgetContext)
        {
            var widgetId = widgetContext.Id;
            var widgetName = widgetContext.DefinitionId;
            CompactWidgetInfo runningWidgetInfo = new CompactWidgetInfo(widgetId, widgetName);
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
                        wingetui.ResetConnection();
                        StartLoadingRoutine(localWidgetInfo);
                        break;

                    case (Verbs.ViewUpdatesOnWingetUI):
                        wingetui.ViewOnWingetUI();
                        break;

                    case (Verbs.OpenWingetUI):
                        wingetui.OpenWingetUI();
                        break;

                    case (Verbs.UpdateAll):
                        localWidgetInfo.customState = 1;
                        wingetui.UpdateAllPackages();
                        updateOptions.Data = Templates.GetData_UpdatesInCourse();
                        updateOptions.Template = Templates.BaseTemplate;
                        WidgetManager.GetDefault().UpdateWidget(updateOptions);
                        break;

                    default:
                        if (verb.Contains(Verbs.UpdatePackage))
                        {
                            int index = int.Parse(verb.Replace(Verbs.UpdatePackage, ""));
                            Console.WriteLine(index);
                            wingetui.UpdatePackage(localWidgetInfo.AvailableUpdates[index]);
                            localWidgetInfo.AvailableUpdates = localWidgetInfo.AvailableUpdates.Where((val, idx) => idx != index).ToArray(); // Remove that widget from the current list
                            DrawUpdates(localWidgetInfo);
                        } else
                        {
                            Console.WriteLine("INVALID VERB " + verb);
                            StartLoadingRoutine(localWidgetInfo);
                        }
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
        public CompactWidgetInfo(string widgetId, string widgetName) {
            AvailableUpdates = new Package[0];
            this.widgetId = widgetId;
            this.widgetName = widgetName;
        }

        public string widgetId { get; set; }
        public string widgetName { get; set; }
        public WidgetSize size { get; set; }
        public int customState = 0;
        public bool isActive = false;
        public Package[] AvailableUpdates { get; set; }

    }

}
