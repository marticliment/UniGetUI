using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class SimplePackage(IPackage Source)
    {
        public string Description { get; } = "Not loaded";
        public string[] Tags { get; } = [];
        public string Name { get; } = Source.Name;
        public string Id { get; } = Source.Id;
        public string VersionString { get; } = Source.VersionString;
        public string NewVersionString { get; } = Source.NewVersionString;
        public string IconId { get; } = Package.GetPackageIconId(Source.Id, Source.Manager.Name, Source.Source.Name);
        public string ManagerName { get; } = Source.Manager.Name;
        public string ManagerDisplayName { get; } = Source.Manager.DisplayName;
        public IconType ManagerIconId { get; } = Source.Manager.Properties.IconId;
        public string SourceName { get; } = Source.Source.Name;
        public Uri SourceUrl { get; } = Source.Source.Url;
        public IconType SourceIconId { get; } = Source.Source.IconId;
    }

    public class AdvancedOperationHistoryEntry(
        IPackage Source,
        OperationType Type,
        OperationStatus Status,
        string Logs
    ) : SimplePackage(Source)
    {
        public OperationType Type { get; } = Type;
        public OperationStatus Status { get; } = Status;
        public string Logs { get; } = Logs;
    };
}