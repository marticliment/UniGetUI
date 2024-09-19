using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal sealed class WinGetSourceProvider : BaseSourceProvider<PackageManager>
    {
        public WinGetSourceProvider(WinGet manager) : base(manager) { }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            List<string> args = ["source", "add", "--name", source.Name, "--arg", source.Url.ToString(), "--accept-source-agreements", "--disable-interactivity"];
            if (source.Name != "winget")
            {
                args.AddRange(["--type", "Microsoft.Rest"]);
            }

            return args.ToArray();
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            return ["source", "remove", "--name", source.Name, "--disable-interactivity"];
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
            if (Manager is WinGet manager)
            {
                return WinGetHelper.Instance.GetSources_UnSafe(manager);
            }

            throw new InvalidOperationException("WinGetSourceProvider.GetSources_UnSafe: Manager is supposed to be WinGet");
        }
    }
}
