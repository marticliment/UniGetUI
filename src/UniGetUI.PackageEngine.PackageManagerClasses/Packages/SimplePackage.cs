using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class SimplePackage
    {
        public string Description { get; }
        public string[] Tags { get; }
        public string Name { get; }
        public string Id { get; }
        public string VersionString { get; }
        public string NewVersionString { get; }
        public string IconId { get; }
        public string ManagerName { get; }
        public string ManagerDisplayName { get; }
        public string SourceName { get; }
        public Uri SourceUrl { get; }
        public IconType SourceIconId { get; }

        public SimplePackage(IPackage Source)
        {
            // Load the details first
            Source.Details.Load();

            // Now assign the values
            Description = Source.Details.Description ?? "No description";
            Tags = Source.Details.Tags;
            Name = Source.Name;
            Id = Source.Id;
            VersionString = Source.VersionString;
            NewVersionString = Source.NewVersionString;
            IconId = Package.GetPackageIconId(Source.Id, Source.Manager.Name, Source.Source.Name);
            ManagerName = Source.Manager.Name;
            ManagerDisplayName = Source.Manager.DisplayName;
            SourceName = Source.Source.Name;
            SourceUrl = Source.Source.Url;
            SourceIconId = Source.Source.IconId;
        }
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