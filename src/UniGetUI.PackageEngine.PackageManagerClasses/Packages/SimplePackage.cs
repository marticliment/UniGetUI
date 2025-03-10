using System.Text.Json.Serialization;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class SimplePackage
    {
        public SimplePackage(IPackage source)
        {
            Description = "Not loaded";
            Tags = [];
            Name = source.Name;
            Id = source.Id;
            VersionString = source.VersionString;
            NewVersionString = source.NewVersionString;
            IconId = Package.GetPackageIconId(source.Id, source.Manager.Name, source.Source.Name);
            ManagerName = source.Manager.Name;
            ManagerDisplayName = source.Manager.DisplayName;
            ManagerIconId = source.Manager.Properties.IconId;
            SourceName = source.Source.Name;
            SourceUrl = source.Source.Url;
            SourceIconId = source.Source.IconId;
        }

        [JsonConstructor]
        public SimplePackage(
            string description, string[] tags, string name, string id,
            string versionString, string newVersionString, string iconId,
            string managerName, string managerDisplayName, IconType managerIconId,
            string sourceName, Uri sourceUrl, IconType sourceIconId)
        {
            Description = description;
            Tags = tags;
            Name = name;
            Id = id;
            VersionString = versionString;
            NewVersionString = newVersionString;
            IconId = iconId;
            ManagerName = managerName;
            ManagerDisplayName = managerDisplayName;
            ManagerIconId = managerIconId;
            SourceName = sourceName;
            SourceUrl = sourceUrl;
            SourceIconId = sourceIconId;
        }

        public string Description { get; }
        public string[] Tags { get; }
        public string Name { get; }
        public string Id { get; }
        public string VersionString { get; }
        public string NewVersionString { get; }
        public string IconId { get; }
        public string ManagerName { get; }
        public string ManagerDisplayName { get; }
        public IconType ManagerIconId { get; }
        public string SourceName { get; }
        public Uri SourceUrl { get; }
        public IconType SourceIconId { get; }
    }

    public class AdvancedOperationHistoryEntry : SimplePackage
    {
        public AdvancedOperationHistoryEntry(IPackage source, OperationType type, OperationStatus status, string logs) : base(source)
        {
            Type = type;
            Status = status;
            Logs = logs;
        }

        [JsonConstructor]
        public AdvancedOperationHistoryEntry(
            OperationType type, OperationStatus status, string logs,
            string description, string[] tags, string name, string id,
            string versionString, string newVersionString, string iconId,
            string managerName, string managerDisplayName, IconType managerIconId,
            string sourceName, Uri sourceUrl, IconType sourceIconId)
                : base(description, tags, name, id, versionString, newVersionString, iconId,
                managerName, managerDisplayName, managerIconId, sourceName, sourceUrl, sourceIconId)
        {
            Type = type;
            Status = status;
            Logs = logs;
        }

        public OperationType Type { get; }
        public OperationStatus Status { get; }
        public string Logs { get; }
    };
}