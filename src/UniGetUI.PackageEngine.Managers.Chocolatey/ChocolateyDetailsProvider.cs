using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.Chocolatey
{
    public class ChocolateyDetailsProvider : BaseNuGetDetailsProvider
    {
        public ChocolateyDetailsProvider(BaseNuGet manager) : base(manager)
        { }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    // choco search php --exact --all-versions
                    FileName = Manager.Status.ExecutablePath,
                    Arguments = Manager.Properties.ExecutableCallArgs + $" search {package.Id} --exact --all-versions",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            var logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions, p);

            p.Start();

            string? line;
            List<string> versions = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (line.Contains("[Approved]"))
                    versions.Add(line.Split(' ')[1].Trim());
            }
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return versions.ToArray();
        }
    }
}
