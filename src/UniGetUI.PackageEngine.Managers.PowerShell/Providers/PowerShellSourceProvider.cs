using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    internal sealed class PowerShellSourceProvider : BaseSourceProvider<PackageManager>
    {
        public PowerShellSourceProvider(PowerShell manager) : base(manager) { }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            if (source.Url.ToString() == "https://www.powershellgallery.com/api/v2")
            {
                return ["Register-PSRepository", "-Default"];
            }

            return ["Register-PSRepository", "-Name", source.Name, "-SourceLocation", source.Url.ToString()];
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            return ["Unregister-PSRepository", "-Name", source.Name];
        }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        protected override IEnumerable<IManagerSource> GetSources_UnSafe()
        {
            List<ManagerSource> sources = [];

            Process p = new()
            {
                StartInfo = new()
                {
                    FileName = Manager.Status.ExecutablePath,
                    Arguments = Manager.Properties.ExecutableCallArgs + " Get-PSRepository",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListSources, p);

            p.Start();

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
                        if (parts.Length >= 3)
                        {
                            sources.Add(new ManagerSource(Manager, parts[0].Trim(), new Uri(parts[2].Trim())));
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
}
