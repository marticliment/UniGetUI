using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.PackageManagerClasses;
using Windows.ApplicationModel;

namespace UniGetUI.Services
{
    /// <summary>
    /// Service for collecting system information, logs, and diagnostic data
    /// for GitHub issue reporting
    /// </summary>
    public class FeedbackService
    {
        private static readonly Lazy<FeedbackService> _instance = new Lazy<FeedbackService>(() => new FeedbackService());
        public static FeedbackService Instance => _instance.Value;

        private FeedbackService() { }

        /// <summary>
        /// Collects comprehensive system information
        /// </summary>
        public async Task<SystemInformation> CollectSystemInfoAsync()
        {
            var info = new SystemInformation();

            try
            {
                // Get app version
                var version = Package.Current.Id.Version;
                info.AppVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

                // Get OS information
                info.OSVersion = Environment.OSVersion.VersionString;
                info.OSArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                info.ProcessorArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "Unknown";

                // Get installed package managers
                info.PackageManagers = await GetInstalledPackageManagersAsync();

                // Get system locale
                info.SystemLocale = System.Globalization.CultureInfo.CurrentCulture.Name;
                info.UILocale = System.Globalization.CultureInfo.CurrentUICulture.Name;

                // Get .NET version
                info.DotNetVersion = Environment.Version.ToString();

                // Get memory info
                var totalMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024L * 1024L);
                info.TotalMemoryMB = totalMb > int.MaxValue ? int.MaxValue : (int)totalMb;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error collecting system info: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// Gets list of installed and available package managers
        /// </summary>
        private async Task<List<PackageManagerInfo>> GetInstalledPackageManagersAsync()
        {
            var managers = new List<PackageManagerInfo>();

            await Task.Run(() =>
            {
                foreach (var manager in PackageEngine.PackageEngine.PackageManagerList)
                {
                    managers.Add(new PackageManagerInfo
                    {
                        Name = manager.Name,
                        IsAvailable = manager.Status.Found,
                        Version = manager.Status.Version ?? "Unknown",
                        ExecutablePath = manager.Status.ExecutablePath ?? "Not found"
                    });
                }
            });

            return managers;
        }

        /// <summary>
        /// Collects recent application logs
        /// </summary>
        /// <param name="maxLines">Maximum number of log lines to collect</param>
        public async Task<string> CollectLogsAsync(int maxLines = 200)
        {
            var logBuilder = new StringBuilder();

            try
            {
                // Get log file path
                var logPath = Logger.GetLogFilePath();

                if (File.Exists(logPath))
                {
                    var lines = await File.ReadAllLinesAsync(logPath);
                    var recentLines = lines.TakeLast(maxLines);

                    logBuilder.AppendLine("### Recent Application Logs");
                    logBuilder.AppendLine("```");
                    foreach (var line in recentLines)
                    {
                        logBuilder.AppendLine(line);
                    }
                    logBuilder.AppendLine("```");
                }
                else
                {
                    logBuilder.AppendLine("### Logs");
                    logBuilder.AppendLine("Log file not found.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error collecting logs: {ex.Message}");
                logBuilder.AppendLine($"### Error Collecting Logs");
                logBuilder.AppendLine($"```\n{ex.Message}\n```");
            }

            return logBuilder.ToString();
        }

        /// <summary>
        /// Formats system information as markdown for GitHub issues
        /// </summary>
        public string FormatSystemInfoAsMarkdown(SystemInformation info)
        {
            var sb = new StringBuilder();

            sb.AppendLine("### System Information");
            sb.AppendLine();
            sb.AppendLine($"- **UniGetUI Version:** {info.AppVersion}");
            sb.AppendLine($"- **OS Version:** {info.OSVersion}");
            sb.AppendLine($"- **OS Architecture:** {info.OSArchitecture}");
            sb.AppendLine($"- **Processor Architecture:** {info.ProcessorArchitecture}");
            sb.AppendLine($"- **.NET Version:** {info.DotNetVersion}");
            sb.AppendLine($"- **System Locale:** {info.SystemLocale}");
            sb.AppendLine($"- **UI Locale:** {info.UILocale}");
            sb.AppendLine($"- **Total Memory:** {info.TotalMemoryMB} MB");
            sb.AppendLine();

            sb.AppendLine("### Package Managers");
            sb.AppendLine();

            if (info.PackageManagers.Any())
            {
                foreach (var pm in info.PackageManagers)
                {
                    var status = pm.IsAvailable ? "✅" : "❌";
                    sb.AppendLine($"- {status} **{pm.Name}**");
                    if (pm.IsAvailable)
                    {
                        sb.AppendLine($"  - Version: {pm.Version}");
                        sb.AppendLine($"  - Path: `{pm.ExecutablePath}`");
                    }
                }
            }
            else
            {
                sb.AppendLine("No package managers detected.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates a complete issue body with system info and logs
        /// </summary>
        public async Task<string> CreateIssueBodyAsync(
            string userDescription,
            bool includeLogs = true,
            bool includeSystemInfo = true)
        {
            var sb = new StringBuilder();

            // User description
            sb.AppendLine("### Description");
            sb.AppendLine();
            sb.AppendLine(userDescription);
            sb.AppendLine();

            // System information
            if (includeSystemInfo)
            {
                var systemInfo = await CollectSystemInfoAsync();
                sb.AppendLine(FormatSystemInfoAsMarkdown(systemInfo));
                sb.AppendLine();
            }

            // Logs
            if (includeLogs)
            {
                var logs = await CollectLogsAsync();
                sb.AppendLine(logs);
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Holds collected system information
    /// </summary>
    public class SystemInformation
    {
        public string AppVersion { get; set; } = "Unknown";
        public string OSVersion { get; set; } = "Unknown";
        public string OSArchitecture { get; set; } = "Unknown";
        public string ProcessorArchitecture { get; set; } = "Unknown";
        public string DotNetVersion { get; set; } = "Unknown";
        public string SystemLocale { get; set; } = "Unknown";
        public string UILocale { get; set; } = "Unknown";
        public int TotalMemoryMB { get; set; }
        public List<PackageManagerInfo> PackageManagers { get; set; } = new();
    }

    /// <summary>
    /// Information about a package manager
    /// </summary>
    public class PackageManagerInfo
    {
        public string Name { get; set; } = "Unknown";
        public bool IsAvailable { get; set; }
        public string Version { get; set; } = "Unknown";
        public string ExecutablePath { get; set; } = "Unknown";
    }
}
