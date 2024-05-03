using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.ChocolateyManager
{
    internal class ChocolateySourceProvider : BaseSourceProvider<PackageManager>
    {
        public ChocolateySourceProvider(Chocolatey manager) : base(manager) { }

        public override string[] GetAddSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "add", "--name", source.Name, "--source", source.Url.ToString(), "-y" };
        }

        public override string[] GetRemoveSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "remove", "--name", source.Name, "-y" };
        }

        public override OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        protected override async Task<ManagerSource[]> GetSources_UnSafe()
        {
            List<ManagerSource> sources = new();

            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            process.StartInfo = startInfo;
            process.Start();


            string output = "";
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                try
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.Contains(" - ") && line.Contains(" | "))
                    {
                        string[] parts = line.Trim().Split('|')[0].Trim().Split(" - ");
                        if (parts[1].Trim() == "https://community.chocolatey.org/api/v2/")
                            sources.Add(new ManagerSource(Manager, "community", new Uri("https://community.chocolatey.org/api/v2/")));
                        else
                            sources.Add(new ManagerSource(Manager, parts[0].Trim(), new Uri(parts[1].Trim())));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            output += await process.StandardError.ReadToEndAsync();
            Manager.LogOperation(process, output);

            await process.WaitForExitAsync();
            return sources.ToArray();
        }
    }
}
