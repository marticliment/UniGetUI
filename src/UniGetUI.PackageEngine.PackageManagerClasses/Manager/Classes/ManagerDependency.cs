using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Classes.Manager.Classes
{
    public struct ManagerDependency
    {
        public readonly string Name;
        public readonly string InstallFileName;
        public readonly string InstallArguments;
        public readonly Func<Task<bool>> IsInstalled;

        public ManagerDependency(
            string name,
            string installFileName,
            string installArguments,
            Func<Task<bool>> isInstalled)
        {
            Name = name;
            InstallFileName = installFileName;
            InstallArguments = installArguments;
            IsInstalled = isInstalled;
        }
    }
}
