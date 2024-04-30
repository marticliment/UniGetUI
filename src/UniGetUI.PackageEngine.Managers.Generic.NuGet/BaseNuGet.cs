using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using System.Net;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    public abstract class BaseNuGet : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };

        public BaseNuGet() : base()
        {
            PackageDetailsProvider = new BaseNuGetDetailsProvider(this);
        }

        public sealed override async Task InitializeAsync()
        {
            if(PackageDetailsProvider is not BaseNuGetDetailsProvider)
                throw new Exception("NuGet-based package managers must not reassign the PackageDetailsProvider property");

            if (!Capabilities.SupportsCustomVersions)
                throw new Exception("NuGet-based package managers must support custom versions");
            if (!Capabilities.SupportsCustomPackageIcons)
                throw new Exception("NuGet-based package managers must support custom versions");

            await base.InitializeAsync();
        }

        private struct SearchResult
        {
            public string version;
            public float version_float;
            public string id;
        }

        protected sealed override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            Logger.Error($"Using new NuGet search engine for manager {Name}");
            List<Package> Packages = new();

            ManagerSource[] sources;
            if (Capabilities.SupportsCustomSources)
                sources = await GetSources();
            else
                sources = new ManagerSource[] { Properties.DefaultSource };
            
            foreach(var source in sources)
            {
                Uri SearchUrl = new Uri($"{source.Url}/Search()?searchTerm=%27{query}%27&targetFramework=%27%27&includePrerelease=false");
                Logger.Debug($"Begin package search with url={SearchUrl} on manager {Name}"); ;
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.All
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(SearchUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warn($"Failed to fetch api at Url={SearchUrl} with status code {response.StatusCode}");
                        continue;
                    }

                    string SearchResults = await response.Content.ReadAsStringAsync();
                    MatchCollection matches = Regex.Matches(SearchResults, "<entry>([\\s\\S]*?)<\\/entry>");

                    Dictionary<string, SearchResult> AlreadyProcessedPackages = new();

                    foreach (Match match in matches)
                    {
                        if (!match.Success) continue;

                        string id = Regex.Match(match.Value, "Id='([^<>']+)'").Groups[1].Value;
                        string version = Regex.Match(match.Value, "Version='([^<>']+)'").Groups[1].Value;
                        float float_version = CoreTools.GetVersionStringAsFloat(version);
                        Match title = Regex.Match(match.Value, "<title[ \\\"\\=A-Za-z0-9]+>([^<>]+)<\\/title>");

                        if (AlreadyProcessedPackages.ContainsKey(id) && AlreadyProcessedPackages[id].version_float >= float_version)
                            continue;

                        AlreadyProcessedPackages[id] = new SearchResult() { id = id, version = version, version_float = float_version };
                    }
                    foreach(var package in AlreadyProcessedPackages.Values)
                        Packages.Add(new Package(CoreTools.FormatAsName(package.id), package.id, package.version, source, this));

                }
            }

            return Packages.ToArray();
        }
        
    }

}
