using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    internal class PowerShellSourceProvider : BaseSourceProvider<PackageManager>
    {
        public PowerShellSourceProvider(PowerShell manager) : base(manager) { }

        public override string[] GetAddSourceParameters(ManagerSource source)
        {
            if (source.Url.ToString() == "https://www.powershellgallery.com/api/v2")
                return new string[] { "Register-PSRepository", "-Default" };
            return new string[] { "Register-PSRepository", "-Name", source.Name, "-SourceLocation", source.Url.ToString() };
        }

        public override string[] GetRemoveSourceParameters(ManagerSource source)
        {
            return new string[] { "Unregister-PSRepository", "-Name", source.Name };
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
                Arguments = Manager.Properties.ExecutableCallArgs + " Get-PSRepository",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            process.StartInfo = startInfo;
            process.Start();

            bool dashesPassed = false;
            string line;
            string output = "";
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                try
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (!dashesPassed)
                    {
                        if (line.Contains("---"))
                            dashesPassed = true;
                    }
                    else
                    {
                        string[] parts = Regex.Replace(line.Trim(), " {2,}", " ").Split(' ');
                        if (parts.Length >= 3)
                            sources.Add(new ManagerSource(Manager, parts[0].Trim(), new Uri(parts[2].Trim())));
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }
            output += await process.StandardError.ReadToEndAsync();
            Manager.LogOperation(process, output);
            await process.WaitForExitAsync();
            return sources.ToArray();
        }
    }
}
