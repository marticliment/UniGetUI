using System.Diagnostics;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.Managers.PowerShellManager;

namespace UniGetUI.PackageEngine.Managers.Chocolatey
{
    public class ChocolateyDetailsHelper : BaseNuGetDetailsHelper
    {
        public ChocolateyDetailsHelper(BaseNuGet manager) : base(manager)
        { }

        protected override IEnumerable<string> GetInstallableVersions_UnSafe(IPackage package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
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

           IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions, p);

            p.Start();

            string? line;
            List<string> versions = [];
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                if (line.Contains("[Approved]"))
                {
                    versions.Add(line.Split(' ')[1].Trim());
                }
            }
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);

            return versions;
        }

        protected override string? GetInstallLocation_UnSafe(IPackage package)
        {
            string portable_path = Manager.Status.ExecutablePath.Replace("choco.exe", $"bin\\{package.Id}.exe");
            if (File.Exists(portable_path))
                return Path.GetDirectoryName(portable_path);

            foreach (var base_path in new string[]
                     {
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                         Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
                         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     })
            {
                var path_with_id = Path.Join(base_path, package.Id);
                if (Directory.Exists(path_with_id)) return path_with_id;
            }

            return Path.Join(Path.GetDirectoryName(Manager.Status.ExecutablePath), "lib", package.Id);

        }
    }
}
