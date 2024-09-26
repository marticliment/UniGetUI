using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.WingetManager;

internal sealed class BundledWinGetHelper : IWinGetManagerHelper
{
    public IEnumerable<Package> GetAvailableUpdates_UnSafe(WinGet Manager)
    {
        List<Package> Packages = [];
        Process p = new()
        {
            StartInfo = new()
            {
                FileName = Manager.WinGetBundledPath,
                Arguments = Manager.Properties.ExecutableCallArgs +
                            " update --include-unknown  --accept-source-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };

        IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);

        p.Start();

        string OldLine = "";
        int IdIndex = -1;
        int VersionIndex = -1;
        int NewVersionIndex = -1;
        int SourceIndex = -1;
        bool DashesPassed = false;
        string? line;
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            logger.AddToStdOut(line);

            if (line.Contains("have pins"))
            {
                continue;
            }

            if (!DashesPassed && line.Contains("---"))
            {
                string HeaderPrefix = OldLine.Contains("SearchId") ? "Search" : "";
                string HeaderSuffix = OldLine.Contains("SearchId") ? "Header" : "";
                IdIndex = OldLine.IndexOf(HeaderPrefix + "Id", StringComparison.InvariantCulture);
                VersionIndex = OldLine.IndexOf(HeaderPrefix + "Version", StringComparison.InvariantCulture);
                NewVersionIndex = OldLine.IndexOf("Available" + HeaderSuffix, StringComparison.InvariantCulture);
                SourceIndex = OldLine.IndexOf(HeaderPrefix + "Source", StringComparison.InvariantCulture);
                DashesPassed = true;
            }
            else if (line.Trim() == "")
            {
                DashesPassed = false;
            }
            else if (DashesPassed && IdIndex > 0 && VersionIndex > 0 && NewVersionIndex > 0 && IdIndex < VersionIndex && VersionIndex < NewVersionIndex && NewVersionIndex < line.Length)
            {
                int offset = 0; // Account for non-unicode character length
                while (line[IdIndex - offset - 1] != ' ' || offset > (IdIndex - 5))
                {
                    offset++;
                }

                string name = line[..(IdIndex - offset)].Trim();
                string id = line[(IdIndex - offset)..].Trim().Split(' ')[0];
                string version = line[(VersionIndex - offset)..(NewVersionIndex - offset)].Trim();
                string newVersion;
                if (SourceIndex != -1)
                {
                    newVersion = line[(NewVersionIndex - offset)..(SourceIndex - offset)].Trim();
                }
                else
                {
                    newVersion = line[(NewVersionIndex - offset)..].Trim().Split(' ')[0];
                }

                IManagerSource source;
                if (SourceIndex == -1 || SourceIndex >= line.Length)
                {
                    source = Manager.DefaultSource;
                }
                else
                {
                    string sourceName = line[(SourceIndex - offset)..].Trim().Split(' ')[0];
                    source = Manager.GetSourceOrDefault(sourceName);
                }

                Packages.Add(new Package(name, id, version, newVersion, source, Manager));
            }
            OldLine = line;
        }

        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();
        logger.Close(p.ExitCode);

        return Packages;
    }

    public IEnumerable<Package> GetInstalledPackages_UnSafe(WinGet Manager)
    {
        List<Package> Packages = [];
        Process p = new()
        {
            StartInfo = new()
            {
                FileName = Manager.WinGetBundledPath,
                Arguments = Manager.Properties.ExecutableCallArgs + " list  --accept-source-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };

        IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);

        p.Start();

        string OldLine = "";
        int IdIndex = -1;
        int VersionIndex = -1;
        int SourceIndex = -1;
        int NewVersionIndex = -1;
        bool DashesPassed = false;
        string? line;
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            try
            {
                logger.AddToStdOut(line);
                if (!DashesPassed && line.Contains("---"))
                {
                    string HeaderPrefix = OldLine.Contains("SearchId") ? "Search" : "";
                    string HeaderSuffix = OldLine.Contains("SearchId") ? "Header" : "";
                    IdIndex = OldLine.IndexOf(HeaderPrefix + "Id", StringComparison.InvariantCulture);
                    VersionIndex = OldLine.IndexOf(HeaderPrefix + "Version", StringComparison.InvariantCulture);
                    NewVersionIndex = OldLine.IndexOf("Available" + HeaderSuffix, StringComparison.InvariantCulture);
                    SourceIndex = OldLine.IndexOf(HeaderPrefix + "Source", StringComparison.InvariantCulture);
                    DashesPassed = true;
                }
                else if (DashesPassed && IdIndex > 0 && VersionIndex > 0 && IdIndex < VersionIndex && VersionIndex < line.Length)
                {
                    int offset = 0; // Account for non-unicode character length
                    while (((IdIndex - offset) <= line.Length && line[IdIndex - offset - 1] != ' ') || offset > (IdIndex - 5))
                    {
                        offset++;
                    }

                    string name = line[..(IdIndex - offset)].Trim();
                    string id = line[(IdIndex - offset)..].Trim().Split(' ')[0];
                    if (NewVersionIndex == -1 && SourceIndex != -1)
                    {
                        NewVersionIndex = SourceIndex;
                    }
                    else if (NewVersionIndex == -1 && SourceIndex == -1)
                    {
                        NewVersionIndex = line.Length - 1;
                    }

                    string version = line[(VersionIndex - offset)..(NewVersionIndex - offset)].Trim();

                    IManagerSource source;
                    if (SourceIndex == -1 || (SourceIndex - offset) >= line.Length)
                    {
                        source = Manager.GetLocalSource(id); // Load Winget Local Sources
                    }
                    else
                    {
                        string sourceName = line[(SourceIndex - offset)..].Trim().Split(' ')[0].Trim();
                        source = Manager.GetSourceOrDefault(sourceName);
                    }
                    Packages.Add(new Package(name, id, version, source, Manager));
                }
                OldLine = line;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();
        logger.Close(p.ExitCode);

        return Packages;
    }

    public IEnumerable<Package> FindPackages_UnSafe(WinGet Manager, string query)
    {
        List<Package> Packages = [];
        Process p = new()
        {
            StartInfo = new()
            {
                FileName = Manager.WinGetBundledPath,
                Arguments = Manager.Properties.ExecutableCallArgs + " search \"" + query +
                            "\"  --accept-source-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };

        IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);

        p.Start();

        string OldLine = "";
        int IdIndex = -1;
        int VersionIndex = -1;
        int SourceIndex = -1;
        bool DashesPassed = false;
        string? line;
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            logger.AddToStdOut(line);
            if (!DashesPassed && line.Contains("---"))
            {
                string HeaderPrefix = OldLine.Contains("SearchId") ? "Search" : "";
                IdIndex = OldLine.IndexOf(HeaderPrefix + "Id", StringComparison.InvariantCulture);
                VersionIndex = OldLine.IndexOf(HeaderPrefix + "Version", StringComparison.InvariantCulture);
                SourceIndex = OldLine.IndexOf(HeaderPrefix + "Source", StringComparison.InvariantCulture);
                DashesPassed = true;
            }
            else if (DashesPassed && IdIndex > 0 && VersionIndex > 0 && IdIndex < VersionIndex && VersionIndex < line.Length)
            {
                int offset = 0; // Account for non-unicode character length
                while (line[IdIndex - offset - 1] != ' ' || offset > (IdIndex - 5))
                {
                    offset++;
                }

                string name = line[..(IdIndex - offset)].Trim();
                string id = line[(IdIndex - offset)..].Trim().Split(' ')[0];
                string version = line[(VersionIndex - offset)..].Trim().Split(' ')[0];
                IManagerSource source;
                if (SourceIndex == -1 || SourceIndex >= line.Length)
                {
                    source = Manager.DefaultSource;
                }
                else
                {
                    string sourceName = line[(SourceIndex - offset)..].Trim().Split(' ')[0];
                    source = Manager.GetSourceOrDefault(sourceName);
                }
                Packages.Add(new Package(name, id, version, source, Manager));
            }
            OldLine = line;
        }

        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();
        logger.Close(p.ExitCode);

        return Packages;
    }



    public void GetPackageDetails_UnSafe(WinGet Manager, IPackageDetails details)
    {
        if (details.Package.Source.Name == "winget")
        {
            details.ManifestUrl = new Uri("https://github.com/microsoft/winget-pkgs/tree/master/manifests/"
                                          + details.Package.Id[0].ToString().ToLower() + "/"
                                          + details.Package.Id.Split('.')[0] + "/"
                                          + string.Join("/",
                                              details.Package.Id.Contains('.')
                                                  ? details.Package.Id.Split('.')[1..]
                                                  : details.Package.Id.Split('.'))
            );
        }
        else if (details.Package.Source.Name == "msstore")
        {
            details.ManifestUrl = new Uri("https://apps.microsoft.com/detail/" + details.Package.Id);
        }

        // Get the output for the best matching locale
        Process process = new();
        List<string> output = [];
        bool LocaleFound = true;
        ProcessStartInfo startInfo = new()
        {
            FileName = Manager.WinGetBundledPath,
            Arguments = Manager.Properties.ExecutableCallArgs + " show " + WinGetOperationProvider.GetIdNamePiece(details.Package) +
                        " --disable-interactivity --accept-source-agreements --locale " +
                        System.Globalization.CultureInfo.CurrentCulture,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        process.StartInfo = startInfo;
        process.Start();

        string? _line;
        while ((_line = process.StandardOutput.ReadLine()) is not null)
        {
            if (_line.Trim() != "")
            {
                output.Add(_line);
                if (_line.Contains("The value provided for the `locale` argument is invalid") ||
                    _line.Contains("No applicable installer found; see Logger.Logs for more details."))
                {
                    LocaleFound = false;
                    break;
                }
            }
        }

        // Load fallback english locale
        if (!LocaleFound)
        {
            output.Clear();
            Logger.Info("Winget could not found culture data for package Id=" + details.Package.Id + " and Culture=" +
                        System.Globalization.CultureInfo.CurrentCulture.ToString() + ". Trying to get data for en-US");
            process = new Process();
            LocaleFound = true;
            startInfo = new()
            {
                FileName = Manager.WinGetBundledPath,
                Arguments = Manager.Properties.ExecutableCallArgs + " show " + WinGetOperationProvider.GetIdNamePiece(details.Package) +
                            " --disable-interactivity --accept-source-agreements --locale en-US",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = startInfo;
            process.Start();

            while ((_line = process.StandardOutput.ReadLine()) is not null)
            {
                if (_line.Trim() != "")
                {
                    output.Add(_line);
                    if (_line.Contains("The value provided for the `locale` argument is invalid") ||
                        _line.Contains("No applicable installer found; see Logger.Logs for more details."))
                    {
                        LocaleFound = false;
                        break;
                    }
                }
            }
        }

        // Load default locale
        if (!LocaleFound)
        {
            output.Clear();
            Logger.Info("Winget could not found culture data for package Id=" + details.Package.Id +
                        " and Culture=en-US. Loading default");
            process = new Process();
            startInfo = new()
            {
                FileName = Manager.WinGetBundledPath,
                Arguments = Manager.Properties.ExecutableCallArgs + " show " + WinGetOperationProvider.GetIdNamePiece(details.Package) +
                            " --disable-interactivity --accept-source-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = startInfo;
            process.Start();

            while ((_line = process.StandardOutput.ReadLine()) is not null)
            {
                if (_line.Trim() != "")
                {
                    output.Add(_line);
                }
            }
        }

        // Parse the output
        bool IsLoadingDescription = false;
        bool IsLoadingReleaseNotes = false;
        bool IsLoadingTags = false;
        foreach (string __line in output)
        {
            try
            {
                string line = __line.TrimEnd();
                if (line == "")
                {
                    continue;
                }

                // Check if a multiline field is being loaded
                if (line.StartsWith(" ") && IsLoadingDescription)
                {
                    details.Description += "\n" + line.Trim();
                }
                else if (line.StartsWith(" ") && IsLoadingReleaseNotes)
                {
                    details.ReleaseNotes += "\n" + line.Trim();
                }
                else if (line.StartsWith(" ") && IsLoadingTags)
                {
                    details.Tags = details.Tags.Append(line.Trim()).ToArray();
                }

                // Stop loading multiline fields
                else if (IsLoadingDescription)
                {
                    IsLoadingDescription = false;
                }
                else if (IsLoadingReleaseNotes)
                {
                    IsLoadingReleaseNotes = false;
                }
                else if (IsLoadingTags)
                {
                    IsLoadingTags = false;
                }

                // Check for single-line fields
                if (line.Contains("Publisher:"))
                {
                    details.Publisher = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Author:"))
                {
                    details.Author = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Homepage:"))
                {
                    details.HomepageUrl = new Uri(line.Replace("Homepage:", "").Trim());
                }
                else if (line.Contains("License:"))
                {
                    details.License = line.Split(":")[1].Trim();
                }
                else if (line.Contains("License Url:"))
                {
                    details.LicenseUrl = new Uri(line.Replace("License Url:", "").Trim());
                }
                else if (line.Contains("Installer SHA256:"))
                {
                    details.InstallerHash = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Installer Url:"))
                {
                    details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                    details.InstallerSize = CoreTools.GetFileSize(details.InstallerUrl);
                }
                else if (line.Contains("Release Date:"))
                {
                    details.UpdateDate = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Release Notes Url:"))
                {
                    details.ReleaseNotesUrl = new Uri(line.Replace("Release Notes Url:", "").Trim());
                }
                else if (line.Contains("Installer Type:"))
                {
                    details.InstallerType = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Description:"))
                {
                    details.Description = line.Split(":")[1].Trim();
                    IsLoadingDescription = true;
                }
                else if (line.Contains("ReleaseNotes"))
                {
                    details.ReleaseNotes = line.Split(":")[1].Trim();
                    IsLoadingReleaseNotes = true;
                }
                else if (line.Contains("Tags"))
                {
                    details.Tags = [];
                    IsLoadingTags = true;
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Error occurred while parsing line value=\"" + _line + "\"");
                Logger.Warn(e.Message);
            }
        }

        return;
    }

    public IEnumerable<string> GetInstallableVersions_Unsafe(WinGet Manager, IPackage package)
    {
        Process p = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Manager.WinGetBundledPath,
                Arguments = Manager.Properties.ExecutableCallArgs + " show " + WinGetOperationProvider.GetIdNamePiece(package) +
                            $" --versions --accept-source-agreements",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };

        IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions, p);

        p.Start();

        string? line;
        List<string> versions = [];
        bool DashesPassed = false;
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            logger.AddToStdOut(line);
            if (!DashesPassed)
            {
                if (line.Contains("---"))
                {
                    DashesPassed = true;
                }
            }
            else
            {
                versions.Add(line.Trim());
            }
        }

        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();
        logger.Close(p.ExitCode);
        return versions;
    }

    public IEnumerable<IManagerSource> GetSources_UnSafe(WinGet Manager)
    {
        List<IManagerSource> sources = [];

        Process p = new()
        {
            StartInfo = new()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };

        p.Start();

        IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);

        bool dashesPassed = false;
        string? line;
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            logger.AddToStdOut(line);
            try
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (!dashesPassed)
                {
                    if (line.Contains("---"))
                    {
                        dashesPassed = true;
                    }
                }
                else
                {
                    string[] parts = Regex.Replace(line.Trim(), " {2,}", " ").Split(' ');
                    if (parts.Length > 1)
                    {
                        sources.Add(new ManagerSource(Manager, parts[0].Trim(), new Uri(parts[1].Trim())));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e);
            }
        }

        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();
        logger.Close(p.ExitCode);
        return sources;

    }
}
