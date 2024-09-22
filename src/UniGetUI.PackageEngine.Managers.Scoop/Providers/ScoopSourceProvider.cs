using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{
    internal sealed class ScoopSourceProvider : BaseSourceProvider<PackageManager>
    {
        public ScoopSourceProvider(Scoop manager) : base(manager) { }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            return ["bucket", "add", source.Name, source.Url.ToString()];
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            return ["bucket", "rm", source.Name];
        }

        protected override IEnumerable<IManagerSource> GetSources_UnSafe()
        {
            using var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Manager.Status.ExecutablePath,
                    Arguments = Manager.Properties.ExecutableCallArgs + " bucket list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardInputEncoding = System.Text.Encoding.UTF8,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                }
            };

            IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListSources, p);
            List<ManagerSource> sources = [];

            p.Start();

            bool DashesPassed = false;

            string? line;
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                try
                {
                    if (!DashesPassed)
                    {
                        if (line.Contains("---"))
                        {
                            DashesPassed = true;
                        }
                    }
                    else if (line.Trim() != "")
                    {
                        string[] elements = Regex.Replace(Regex.Replace(line, "[1234567890 :.-][AaPp][Mm][\\W]", "").Trim(), " {2,}", " ").Split(' ');
                        if (elements.Length >= 5)
                        {
                            if (!elements[1].Contains("https://") && !elements[1].Contains("http://"))
                            {
                                elements[1] = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                    "scoop", "buckets", elements[0].Trim());
                            }

                            sources.Add(new ManagerSource(Manager, elements[0].Trim(), new Uri(elements[1]), int.Parse(elements[4].Trim()), elements[2].Trim() + " " + elements[3].Trim()));
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
