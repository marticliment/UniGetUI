using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal class WinGetSourceProvider : BaseSourceProvider<PackageManager>
    {
        public WinGetSourceProvider(WinGet manager) : base(manager) { }

        public override string[] GetAddSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "add", "--name", source.Name, "--arg", source.Url.ToString(), "--accept-source-agreements", "--disable-interactivity" };
        }

        public override string[] GetRemoveSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "remove", "--name", source.Name, "--disable-interactivity" };
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
            return await WinGetHelper.Instance.GetSources_UnSafe(Manager as WinGet);
        }
    }
}
