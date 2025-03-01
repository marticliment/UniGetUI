using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal sealed class WinGetSourceHelper : BaseSourceHelper
    {
        public WinGetSourceHelper(WinGet manager) : base(manager) { }

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

        protected override OperationVeredict _getAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure;
        }

        protected override OperationVeredict _getRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure;
        }

        protected override IReadOnlyList<IManagerSource> GetSources_UnSafe()
        {
            return WinGetHelper.Instance.GetSources_UnSafe();
        }
    }
}
