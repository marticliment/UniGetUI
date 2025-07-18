using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal sealed class WinGetSourceHelper : BaseSourceHelper
    {
        private readonly string[][] _sourceTypes =
        [
            ["--type", "Microsoft.PreIndexed.Package"],
            ["--type", "Microsoft.Rest"],
        ];
        private Dictionary<string, int> _attemptedSourceTypes = new();

        public WinGetSourceHelper(WinGet manager) : base(manager) { }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            List<string> args = ["source", "add", "--name", source.Name, "--arg", source.Url.ToString(), "--accept-source-agreements", "--disable-interactivity"];

            if (source.Name != "winget")
            {
                int sourceIndex = _attemptedSourceTypes.GetValueOrDefault(source.Name);
                args.AddRange(_sourceTypes[sourceIndex]);
            }

            return args.ToArray();
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            return ["source", "remove", "--name", source.Name, "--disable-interactivity"];
        }

        protected override OperationVeredict _getAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            if ((uint)ReturnCode is 0x8A150045 or 0x801901F4)
            {
                int sourceIndex = _attemptedSourceTypes.GetValueOrDefault(source.Name);
                if (sourceIndex + 1 >= _sourceTypes.Length)
                {
                    _attemptedSourceTypes[source.Name] = 0;
                    return OperationVeredict.Failure;
                }

                _attemptedSourceTypes[source.Name] = sourceIndex + 1;
                return OperationVeredict.AutoRetry;
            }

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
