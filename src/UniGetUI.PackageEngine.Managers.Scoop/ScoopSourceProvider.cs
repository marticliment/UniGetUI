using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{
    internal class ScoopSourceProvider : BaseSourceProvider<PackageManager>
    {
        public ScoopSourceProvider(Scoop manager) : base(manager) { }

        public override OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override string[] GetAddSourceParameters(ManagerSource source)
        {
            return new string[] { "bucket", "add", source.Name, source.Url.ToString() };
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override string[] GetRemoveSourceParameters(ManagerSource source)
        {
            return new string[] { "bucket", "rm", source.Name };
        }

        protected override async Task<ManagerSource[]> GetSources_UnSafe()
        {
            using (Process process = new())
            {
                process.StartInfo.FileName = Manager.Status.ExecutablePath;
                process.StartInfo.Arguments = Manager.Properties.ExecutableCallArgs + " bucket list";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                List<ManagerSource> sources = new();

                process.Start();

                string _output = "";
                bool DashesPassed = false;

                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                {
                    _output += line + "\n";
                    try
                    {
                        if (!DashesPassed)
                        {
                            if (line.Contains("---"))
                                DashesPassed = true;
                        }
                        else if (line.Trim() != "")
                        {
                            string[] elements = Regex.Replace(line.Replace("AM", "").Replace("am", "").Replace("PM", "").Replace("pm", "").Trim(), " {2,}", " ").Split(' ');
                            if (elements.Length >= 5)
                            {
                                if (!elements[1].Contains("https://"))
                                    elements[1] = "https://scoop.sh/"; // If the URI is invalid, we'll use the main website
                                sources.Add(new ManagerSource(Manager, elements[0].Trim(), new Uri(elements[1].Trim()), int.Parse(elements[4].Trim()), elements[2].Trim() + " " + elements[3].Trim()));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Warn(e);
                    }
                }
                _output += await process.StandardError.ReadToEndAsync();
                Manager.LogOperation(process, _output);

                await process.WaitForExitAsync();


                return sources.ToArray();
            }
        }
    }
}
