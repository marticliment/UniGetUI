using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
public abstract class BaseOperationProvider<ManagerT> : IOperationProvider where ManagerT : IPackageManager
{
    protected ManagerT Manager;
    public BaseOperationProvider(ManagerT manager)
    {
        Manager = manager;
    }

    public abstract IEnumerable<string> GetOperationParameters(
        IPackage package,
        IInstallationOptions options,
        OperationType operation);

    public abstract OperationVeredict GetOperationResult(
        IPackage package,
        IInstallationOptions options,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode);

}
