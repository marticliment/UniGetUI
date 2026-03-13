using System.Diagnostics;
using System.Text.Json;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.Managers.HomebrewManager;

namespace UniGetUI.PackageEngine.Managers.HomebrewManager.Helpers
{
    internal sealed class HomebrewPkgDetailsHelper(Homebrew manager) : BasePkgDetailsHelper(manager)
    {
        protected override void GetDetails_UnSafe(IPackageDetails details)
        {
            INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageDetails);

            try
            {
                // Use brew info --json to get rich package metadata
                using Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Manager.Status.ExecutablePath,
                        Arguments = $"info --json=v2 {details.Package.Id}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                    }
                };
                p.StartInfo.Environment["HOMEBREW_NO_AUTO_UPDATE"] = "1";
                p.Start();

                string jsonOutput = p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd(); // consume stderr
                p.WaitForExit();

                using var doc = JsonDocument.Parse(jsonOutput);
                var root = doc.RootElement;

                // Try formulae first, then casks
                if (root.TryGetProperty("formulae", out var formulae) && formulae.GetArrayLength() > 0)
                {
                    var formula = formulae[0];
                    details.Description = formula.GetProperty("desc").GetString() ?? "";

                    if (formula.TryGetProperty("homepage", out var homepage))
                        details.HomepageUrl = new Uri(homepage.GetString() ?? "https://brew.sh");

                    if (formula.TryGetProperty("license", out var license))
                        details.License = license.GetString() ?? "";

                    details.Publisher = "Homebrew";
                    details.InstallerType = "Formula";
                    details.ManifestUrl = new Uri($"https://formulae.brew.sh/formula/{details.Package.Id}");
                }
                else if (root.TryGetProperty("casks", out var casks) && casks.GetArrayLength() > 0)
                {
                    var cask = casks[0];
                    details.Description = cask.GetProperty("desc").GetString() ?? "";

                    if (cask.TryGetProperty("homepage", out var homepage))
                        details.HomepageUrl = new Uri(homepage.GetString() ?? "https://brew.sh");

                    details.Publisher = "Homebrew Cask";
                    details.InstallerType = "Cask";
                    details.ManifestUrl = new Uri($"https://formulae.brew.sh/cask/{details.Package.Id}");
                }

                logger.Close(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                logger.Close(1);
            }
        }

        protected override CacheableIcon? GetIcon_UnSafe(IPackage package)
        {
            // Homebrew doesn't provide package icons
            return null;
        }

        protected override IReadOnlyList<Uri> GetScreenshots_UnSafe(IPackage package)
        {
            return [];
        }

        protected override string? GetInstallLocation_UnSafe(IPackage package)
        {
            try
            {
                using Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Manager.Status.ExecutablePath,
                        Arguments = $"--prefix {package.Id}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                p.StartInfo.Environment["HOMEBREW_NO_AUTO_UPDATE"] = "1";
                p.Start();
                string result = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return p.ExitCode == 0 && !string.IsNullOrEmpty(result) ? result : null;
            }
            catch
            {
                return null;
            }
        }

        protected override IReadOnlyList<string> GetInstallableVersions_UnSafe(IPackage package)
        {
            // Homebrew doesn't easily support installing specific versions
            return [];
        }
    }
}
